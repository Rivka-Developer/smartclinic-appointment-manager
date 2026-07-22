// src/app/core/services/hebrew-calendar.service.ts
// ─────────────────────────────────────────────────────────────────────────────
// שירות לוח שנה עברי.
//
// מספק שתי יכולות עיקריות:
//   1. formatDate() – המרת תאריך לועזי לפורמט עברי יפה:
//      "ט"ו שבט" במקום "15 Shevat"
//   2. getDayHolidays() / getWeekHolidays() – זיהוי חגים ומועדים
//      לפי תאריך עברי ותצוגתם בממשק
//
// הממשה מבוססת על ה-Intl.DateTimeFormat API המובנה בדפדפן,
// שתומך בלוח שנה עברי (ca-hebrew).
// ─────────────────────────────────────────────────────────────────────────────

import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class HebrewCalendarService {

  /**
   * מעצב לתצוגה בעברית עם לוח שנה עברי.
   * מחזיר שם יום החודש ושם החודש בעברית.
   * דוגמה: "15 שבט"
   */
  private readonly displayFmt = new Intl.DateTimeFormat('he-IL-u-ca-hebrew', {
    day: 'numeric',    // יום בחודש (מספר)
    month: 'long',     // שם חודש מלא
  });

  /**
   * מעצב באנגלית עם לוח שנה עברי.
   * משמש לזיהוי חגים – שמות החודשים מגיעים ב-ASCII אנגלי
   * שקל יותר להשוות עם switch/if.
   * דוגמה: "15 Shevat"
   */
  private readonly engFmt = new Intl.DateTimeFormat('en-u-ca-hebrew', {
    day: 'numeric',
    month: 'long',
  });

  /**
   * ממיר מספר יום חודש (1–30) לאות/אותיות עבריות עם גרש/גרשיים.
   * דוגמאות: 1→"א'", 10→"י'", 15→"ט\"ו", 22→"כ\"ב"
   *
   * חריגים מיוחדים: 15 ו-16 – 
   *
   * @param n – מספר יום החודש (1–30)
   * @returns ייצוג עברי כולל גרש/גרשיים
   */
  private toHebrewLetters(n: number): string {
    const ones = ['', 'א', 'ב', 'ג', 'ד', 'ה', 'ו', 'ז', 'ח', 'ט']; // 1–9
    const tens = ['', 'י', 'כ', 'ל'];                                   // 10, 20, 30

    // חריגים: 15 ו-16 אסורים בכתיב רגיל 
    if (n === 15) return 'ט"ו';
    if (n === 16) return 'ט"ז';

    // עשרות בלבד (10, 20, 30): אות + גרש
    if (n % 10 === 0) return tens[n / 10] + "'";

    // אחדות בלבד (1–9): אות + גרש
    if (n < 10) return ones[n] + "'";

    // שני ספרות: אות עשרות + גרשיים + אות אחדות
    return tens[Math.floor(n / 10)] + '"' + ones[n % 10];
  }

  /**
   * מעצב תאריך לועזי לפורמט עברי יפה.
   * דוגמה: 2025-01-15 → "ט\"ז שבט"
   *
   * משתמש ב-formatToParts() כדי לפרק את הפלט לחלקים
   * ולהמיר את המספר הדיגיטלי לאותיות עבריות.
   *
   * @param date – תאריך לועזי
   * @returns מחרוזת בעברית כגון "ט\"ז שבט"
   */
  formatDateStr(isoStr: string): string {
    return this.formatDate(new Date(isoStr));
  }

  formatDate(date: Date): string {
    try {
      // formatToParts מחזיר מערך של {type, value}
      // לדוגמה: [{type:'day',value:'15'}, {type:'literal',value:' '}, {type:'month',value:'שבט'}]
      const parts = this.displayFmt.formatToParts(date);
      const dayRaw    = parts.find(p => p.type === 'day')?.value   ?? '1';
      const monthName = parts.find(p => p.type === 'month')?.value ?? '';
      // ממירים את המספר לאותיות עבריות ומצרפים את שם החודש
      return `${this.toHebrewLetters(parseInt(dayRaw, 10))} ${monthName}`;
    } catch {
      // fallback: אם משהו נכשל, נשתמש בפורמט הרגיל
      return this.displayFmt.format(date);
    }
  }

  /**
   * בודק אילו חגים/מועדים חלים בכל יום של שבוע נתון.
   *
   * @param weekStart – ראשון בשבוע
   * @param weekEnd   – שבת בשבוע
   * @returns Map מ-date.toDateString() לרשימת שמות החגים באותו יום
   */
  getWeekHolidays(weekStart: Date, weekEnd: Date): Map<string, string[]> {
    const map = new Map<string, string[]>();
    const d = new Date(weekStart); // עותק כדי לא לשנות את הפרמטר

    // עוברים יום אחרי יום מתחילת השבוע לסופו
    while (d <= weekEnd) {
      const holidays = this.getDayHolidays(d);
      if (holidays.length > 0) {
        // מוסיפים ל-map רק ימים שיש בהם חגים
        map.set(d.toDateString(), holidays);
      }
      d.setDate(d.getDate() + 1); // יום הבא
    }
    return map;
  }

  /**
   * מחזיר רשימת חגים/מועדים הנופלים בתאריך נתון.
   *
   * @param date – תאריך לועזי
   * @returns מערך שמות חגים בעברית (ריק אם אין חגים)
   */
  getDayHolidays(date: Date): string[] {
    try {
      // שולפים את החודש והיום בלוח השנה העברי (בפורמט אנגלי)
      const parts     = this.engFmt.formatToParts(date);
      const monthRaw  = parts.find(p => p.type === 'month')?.value ?? '';
      const dayRaw    = parts.find(p => p.type === 'day')?.value   ?? '0';
      const day       = parseInt(dayRaw, 10);             // יום כמספר שלם
      const month     = monthRaw.toLowerCase().trim();    // חודש באותיות קטנות לנוחות השוואה

      if (!day || !month) return []; // נתונים חסרים – אין חגים
      return this.lookup(month, day); // חיפוש החגים בטבלה
    } catch {
      return []; // שגיאה – מחזירים רשימה ריקה
    }
  }

  /**
   * טבלת חגים ומועדים לפי חודש עברי ויום.
   * מחזיר רשימת שמות חגים בעברית לפי החודש והיום.
   *
   * @param month – שם החודש העברי באנגלית קטנות (למשל "tishri", "nisan")
   * @param day   – יום החודש (1–30)
   * @returns מערך שמות חגים בעברית
   */
  private lookup(month: string, day: number): string[] {
    const h: string[] = []; // רשימת החגים שנמצאו

    // ── תשרי (החודש הראשון של השנה האזרחית) ──
    if (month.startsWith('tishri') || month.startsWith('tishrei')) {
      if (day === 1 || day === 2)              h.push('ראש השנה');
      else if (day === 3)                      h.push('צום גדליה');
      else if (day === 10)                     h.push('יום כיפור');
      else if (day === 15)                     h.push('סוכות');
      else if (day >= 16 && day <= 20)         h.push('חול המועד סוכות');
      else if (day === 21)                     h.push('הושענא רבה');
      else if (day === 22)                     h.push('שמיני עצרת / שמחת תורה');
    }

    // ── כסלו – חנוכה מתחילה ב-כ"ה ──
    else if (month.startsWith('kislev')) {
      if (day >= 25) h.push('חנוכה'); // כ"ה כסלו עד סוף החודש
    }

    // ── טבת – חנוכה ממשיכה + צום עשרה בטבת ──
    else if (month.startsWith('tevet')) {
      if (day <= 3)        h.push('חנוכה');       // ב'-ד' טבת (המשך חנוכה)
      else if (day === 10) h.push('עשרה בטבת');   // צום
    }

    // ── שבט ──
    else if (month.startsWith('shevat') || month.startsWith('shvat')) {
      if (day === 15) h.push('ט"ו בשבט'); // ראש השנה לאילנות
    }

    // ── אדר ב' (שנה מעוברת) – פורים חל בו ──
    // בדיקת 'ii' או '2' בשם מזהה שנה מעוברת
    else if (month.includes('ii') || month.includes('2')) {
      if (day === 13)      h.push('תענית אסתר');
      else if (day === 14) h.push('פורים');
      else if (day === 15) h.push('שושן פורים');
    }

    // ── אדר (שנה רגילה) – לא אדר א' ──
    else if (month.startsWith('adar')) {
      if (!month.includes('i')) { // "adar" בלבד (לא "adari" = אדר א')
        if (day === 13)      h.push('תענית אסתר');
        else if (day === 14) h.push('פורים');
        else if (day === 15) h.push('שושן פורים');
      }
    }

    // ── ניסן – פסח ויום השואה ──
    else if (month.startsWith('nisan')) {
      if (day === 15)                  h.push('פסח');
      else if (day >= 16 && day <= 20) h.push('חול המועד פסח');
      else if (day === 21)             h.push('שביעי של פסח');
      else if (day === 27)             h.push('יום השואה');
    }

    // ── אייר – יום זיכרון, יום העצמאות, ל"ג בעומר, יום ירושלים ──
    else if (month.startsWith('iyar')) {
      if (day === 4)       h.push('יום הזיכרון');
      else if (day === 5)  h.push('יום העצמאות');
      else if (day === 18) h.push('ל"ג בעומר');
      else if (day === 28) h.push('יום ירושלים');
    }

    // ── סיון – שבועות ──
    else if (month.startsWith('sivan')) {
      if (day === 6) h.push('שבועות'); // חג מתן תורה
    }

    // ── תמוז – צום יז בתמוז ──
    else if (month.startsWith('tamuz') || month.startsWith('tammuz')) {
      if (day === 17) h.push('י"ז בתמוז'); // צום
    }

    // ── אב – תשעה באב וטו באב ──
    else if (month === 'av' || month.startsWith('av ')) {
      if (day === 9)       h.push('תשעה באב'); // צום (חורבן בית המקדש)
      else if (day === 15) h.push('ט"ו באב');  // חג האהבה הישראלי
    }

    return h; // רשימת החגים שנמצאו (ריקה אם אין)
  }
}
