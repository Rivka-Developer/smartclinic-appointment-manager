// =====================================
// קובץ: SystemSettings.cs
// שכבה: Domain → Entities (ישויות ליבה)
// תפקיד: מגדיר את מבנה הגדרות המערכת הגלובליות.
//         הגדרות אלה שולטות על כל התנהגות המערכת:
//         כמה זמן בין תורים, מה המשכים המותרים, מתי "ערב" מתחיל וכו'.
//         קיימת רשומה יחידה אחת בטבלה זו (Singleton pattern).
// =====================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppointmentManager.Domain.Entities
{
    /// <summary>
    /// הגדרות מערכת גלובליות הנשמרות בבסיס הנתונים ויכולות להשתנות על ידי המנהל/ת.
    /// כל ההגדרות כאן משפיעות על לוגיקת קביעת התורים בכל המערכת.
    /// </summary>
    public class SystemSettings
    {
        /// <summary>
        /// מזהה ייחודי של רשומת ההגדרות.
        /// Guid.NewGuid() יוצר מזהה חדש. בפועל רק רשומה אחת קיימת בטבלה.
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// זמן "נשימה" (Buffer) בין תור לתור, בדקות.
        /// לדוגמה: אם BufferTime=5, ואחרי תור שמסתיים ב-10:30,
        /// התור הבא יכול להתחיל ב-10:35 לכל המוקדם.
        /// מטרה: לתת למטפל/ת זמן מנוחה קצר בין מטופלים.
        /// </summary>
        public int BufferTime { get; set; }

        /// <summary>
        /// גודל "חור" מינימלי ביומן שמערכת מוכנה להשאיר, בדקות.
        /// לדוגמה: אם MinGapSize=15 ואורך התור הוא 40 דקות,
        /// לא יאפשרו לקבוע תור כזה שישאיר פחות מ-15 דקות פנויות לפניו או אחריו
        /// (כי חור קטן כל כך לא שימושי).
        /// </summary>
        public int MinGapSize { get; set; }

        /// <summary>
        /// מספר השעות לפני התור שבהן עדיין ניתן לבטל.
        /// לדוגמה: CancellationDeadlineHours=24 פירושו שאפשר לבטל עד 24 שעות לפני.
        /// ברירת מחדל: 24 שעות.
        /// </summary>
        public int CancellationDeadlineHours { get; set; } = 24;

        /// <summary>
        /// כתובת האימייל של המנהל/ת לקבלת דוחות יומיים.
        /// אם ריק - לא נשלחים דוחות.
        /// </summary>
        public string AdminContactEmail { get; set; } = string.Empty;

        /// <summary>
        /// משך מקסימלי לתור שמוגדר בשעות הבוקר, בדקות.
        /// בוקר = כל הזמן שלפני EveningStartTime.
        /// ברירת מחדל: 120 דקות (שעתיים).
        /// </summary>
        public int MorningMaxDuration { get; set; } = 120;

        /// <summary>
        /// משך מקסימלי לתור שמוגדר בשעות הערב, בדקות.
        /// ערב = כל הזמן מ-EveningStartTime ואילך.
        /// ברירת מחדל: 40 דקות.
        /// </summary>
        public int EveningMaxDuration { get; set; } = 40;

        /// <summary>
        /// השעה שממנה מתחיל "ערב" לצורך חישוב המשך המקסימלי.
        /// TimeSpan(16, 0, 0) = שעה 16:00 (ארבע אחר הצהריים).
        /// לדוגמה: תור ב-15:50 נחשב "בוקר", תור ב-16:00 נחשב "ערב".
        /// </summary>
        public TimeSpan EveningStartTime { get; set; } = new TimeSpan(17, 0, 0);

        /// <summary>
        /// שם העסק להצגה בממשק ובאימיילים שנשלחים ללקוחות.
        /// לדוגמה: "SmartClinic" או "קליניקת ד\"ר כהן".
        /// </summary>
        public string BusinessName { get; set; } = string.Empty;

        /// <summary>
        /// שעת הסגירה להזמנות ליום המחרת, בפורמט 24 שעות.
        /// לאחר שעה זו, לקוחות לא יכולים לקבוע תורים ליום המחרת.
        /// ברירת מחדל: 23 (23:00 = 11 בלילה).
        /// </summary>
        public int LateBookingCutoffHour { get; set; } = 23;
    }
}
