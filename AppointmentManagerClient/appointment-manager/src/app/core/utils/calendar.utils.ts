// src/app/core/utils/calendar.utils.ts
// פונקציות עזר משותפות לעיבוד זמנים בלוח השנה.
// מייצאות פונקציות טהורות (ללא תלות ב-Angular) שניתן לייבא מכל קומפוננטה.

/** אירוע הנפלט מ-WeekCalendarComponent כאשר שבוע נבנה */
export interface WeekBuiltEvent {
  weekOffset: number;
  days: WeekDayBase[];
  weekStart: Date;
  weekEnd: Date;
}

/** נתוני יום בסיסיים משותפים בין כל תצוגות לוח השנה */
export interface WeekDayBase {
  date: Date;
  label: string;
  hebrewDate: string;
  holidays: string[];
  isToday: boolean;
  isPast: boolean;
  isWeekend: boolean;
}

interface HebrewCalendarLike {
  getWeekHolidays(start: Date, end: Date): Map<string, string[]>;
  formatDate(d: Date): string;
}

const DAY_NAMES = ['ראשון', 'שני', 'שלישי', 'רביעי', 'חמישי', 'שישי', 'שבת'];

/** בונה מערך 7 ימים לשבוע לפי weekOffset, כולל תאריכים עבריים וחגים */
export function buildWeekDays(
  weekOffset: number,
  hebrewCalendar: HebrewCalendarLike
): { days: WeekDayBase[]; weekStart: Date; weekEnd: Date; today: Date } {
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  const weekStart = new Date(today);
  weekStart.setDate(today.getDate() - today.getDay() + weekOffset * 7);
  const weekEnd = new Date(weekStart);
  weekEnd.setDate(weekStart.getDate() + 6);
  weekEnd.setHours(23, 59, 59, 999);
  const weekHolidays = hebrewCalendar.getWeekHolidays(weekStart, weekEnd);
  const days = Array.from({ length: 7 }, (_, i) => {
    const date = new Date(weekStart);
    date.setDate(weekStart.getDate() + i);
    return {
      date,
      label: DAY_NAMES[i],
      hebrewDate: hebrewCalendar.formatDate(date),
      holidays: weekHolidays.get(date.toDateString()) ?? [],
      isToday: date.toDateString() === today.toDateString(),
      isPast: date < today,
      isWeekend: date.getDay() === 5 || date.getDay() === 6
    };
  });
  return { days, weekStart, weekEnd, today };
}

/** ממיר Date ל-ISO string לפי שעון מקומי (ולא UTC) – "YYYY-MM-DDTHH:mm:ss" */
export function toLocalISOStr(d: Date): string {
  const p = (n: number) => String(n).padStart(2, '0');
  return `${d.getFullYear()}-${p(d.getMonth()+1)}-${p(d.getDate())}T${p(d.getHours())}:${p(d.getMinutes())}:${p(d.getSeconds())}`;
}

/** ממיר Date לפורמט "YYYY-MM-DD" לפי שעון מקומי */
export function toLocalDateStr(d: Date): string {
  const p = (n: number) => String(n).padStart(2, '0');
  return `${d.getFullYear()}-${p(d.getMonth()+1)}-${p(d.getDate())}`;
}

/**
 * ממיר ISO datetime string או TimeSpan לפורמט "HH:mm" לתצוגה.
 * תומך בשני פורמטים: "2025-05-21T10:30:00" → "10:30" וגם "10:30:00" → "10:30".
 */
export function formatTime(ts: string): string {
  if (ts.includes('T')) {
    return new Date(ts).toLocaleTimeString('he-IL', { hour: '2-digit', minute: '2-digit', hour12: false });
  }
  return ts.substring(0, 5);
}

/**
 * מחלץ "HH:mm" מ-ISO datetime string לצורך שליחה ל-API.
 * משתמש בשעה מקומית (לא UTC).
 */
export function extractTimeString(iso: string): string {
  if (iso.includes('T')) {
    const d = new Date(iso);
    return `${String(d.getHours()).padStart(2,'0')}:${String(d.getMinutes()).padStart(2,'0')}`;
  }
  return iso.substring(0, 5);
}

/** ממיר TimeSpan "HH:mm:ss" לדקות מחצות */
export function timeSpanToMinutes(ts: string): number {
  const [h, m] = ts.split(':').map(Number);
  return h * 60 + m;
}

/** חותך TimeSpan "HH:mm:ss" לפורמט "HH:mm" לתצוגה */
export function formatTimeSpan(ts: string): string {
  const parts = ts.split(':');
  return `${parts[0]}:${parts[1]}`;
}

/**
 * ממיר ISO datetime string או TimeSpan לדקות מחצות.
 * גרסה כללית יותר של timeSpanToMinutes – תומכת גם בפורמט ISO מלא.
 */
export function toMinutes(ts: string): number {
  const timePart = ts.includes('T') ? ts.split('T')[1] : ts;
  const [h, m] = timePart.split(':').map(Number);
  return (isNaN(h) ? 0 : h) * 60 + (isNaN(m) ? 0 : m);
}
