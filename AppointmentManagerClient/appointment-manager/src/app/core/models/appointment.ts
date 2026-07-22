/** בקשה לקביעת תור חדש – נשלחת ב-POST /appointments/book */
export interface AppointmentRequest {
  /** מועד התחלת התור בפורמט ISO (למשל "2025-06-01T10:00:00") */
  startTime: string;
  /** משך התור בדקות */
  durationMinutes: number;
  /** שם הלקוח (ריק כשהלקוח מזמין בעצמו, ממולא כשמנהל מזמין) */
  clientName: string;
  /** טלפון הלקוח (ריק כשהלקוח מזמין בעצמו) */
  clientPhone: string;
}

/**
 * סטטוס אפשרי של תור:
 * - `Scheduled` = מתוכנן (טרם התקיים)
 * - `Cancelled`  = בוטל
 * - `Completed`  = הושלם
 */
export type AppointmentStatus = 'Scheduled' | 'Cancelled' | 'Completed';

/** נתוני תור כפי שמוחזרים מהשרת */
export interface AppointmentResponse {
  /** מזהה ייחודי של התור (GUID) */
  id: string;
  /** זמן התחלה בפורמט ISO */
  startTime: string;
  /** זמן סיום בפורמט ISO (מחושב מ-startTime + durationMinutes) */
  endTime: string;
  /** משך התור בדקות */
  durationMinutes: number;
  /** סטטוס נוכחי של התור */
  status: AppointmentStatus;
  /** שם הלקוח */
  clientName: string;
  /** טלפון הלקוח */
  clientPhone: string;
}

/**
 * בלוק זמן פנוי יחיד – חלק מרשימת הזמנים הזמינים ביום מסוים.
 * בלוק הוא פרק זמן רציף שבו ניתן לקבוע תור.
 */
export interface TimeBlockDto {
  /** שעת תחילת הבלוק (פורמט ISO או "HH:mm:ss") */
  start: string;
  /** שעת סיום הבלוק */
  end: string;
  /** אורך הבלוק בדקות (end - start) */
  durationMinutes: number;
  /** משך מקסימלי מותר לתור בבלוק זה (0 = ללא הגבלה) */
  maxAllowedDuration: number;
  /** רשימת משכים תקפים לבחירה (למשל [15, 30, 45, 60]) */
  validDurations: number[];
}

/** תשובת השרת לשאילתת זמנים פנויים לתאריך מסוים */
export interface AvailableSlotsResponse {
  /** התאריך שנשאל עליו (פורמט "YYYY-MM-DD") */
  date: string;
  /** רשימת הבלוקים הפנויים באותו יום */
  freeBlocks: TimeBlockDto[];
}

/**
 * טווח שעות מוגדר על ידי שעת התחלה ושעת סיום.
 * נשמר כ-TimeSpan של .NET בפורמט "HH:mm:ss".
 */
export interface TimeRangeDto {
  /** שעת התחלה – פורמט TimeSpan: "HH:mm:ss" */
  start: string;
  /** שעת סיום – פורמט TimeSpan: "HH:mm:ss" */
  end: string;
}

/**
 * אפשרויות מיקום תור בתוך בלוק פנוי.
 * מגדיר היכן ניתן "להצמיד" את התור בתוך הבלוק:
 * - `stickToStart` – הצמד לתחילת הבלוק
 * - `stickToEnd` – הצמד לסוף הבלוק
 * - `validOtherRanges` – טווחים נוספים שבהם ניתן להתחיל
 */
export interface BlockPlacementOptions {
  /** שעת תחילת הבלוק הפנוי */
  blockStart: string;
  /** שעת סיום הבלוק הפנוי */
  blockEnd: string;
  /** שעת התחלה אם מצמידים לתחילת הבלוק */
  stickToStart: string;
  /** שעת התחלה אם מצמידים לסוף הבלוק */
  stickToEnd: string;
  /** טווחים נוספים שבהם שעת ההתחלה תקפה (ללא יצירת מרווח קצר) */
  validOtherRanges: TimeRangeDto[];
}

/** תשובת השרת לשאילתת אפשרויות מיקום – מכיל רשימת בלוקים עם האפשרויות שלהם */
export interface PlacementOptionsResponse {
  /** בדרך כלל מכיל אלמנט אחד – הבלוק שנבחר */
  availableBlocks: BlockPlacementOptions[];
}
