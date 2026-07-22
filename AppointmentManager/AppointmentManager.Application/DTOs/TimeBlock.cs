// =====================================
// קובץ: TimeBlock.cs
// שכבה: Application → DTOs
// תפקיד: מייצג "בלוק זמן" - פרק זמן עם שעת התחלה וסיום.
//         משמש כמבנה נתונים פנימי לאלגוריתם חישוב הזמינות.
//         לדוגמה: חלון פנוי 09:00-11:00 מיוצג כ-TimeBlock(Start=09:00, End=11:00).
// =====================================

namespace AppointmentManager.Application.DTOs
{
    /// <summary>
    /// בלוק זמן עם שעת התחלה וסיום.
    /// "record" = מבנה בלתי-ניתן לשינוי עם השוואה לפי ערכים.
    /// "with" expressions מאפשרים יצירת עותק עם שינוי שדה בודד (ראה AvailabilityService).
    /// </summary>
    /// <param name="Start">זמן תחילת הבלוק (DateTime = תאריך + שעה)</param>
    /// <param name="End">זמן סיום הבלוק</param>
    public record TimeBlock(DateTime Start, DateTime End)
    {
        /// <summary>
        /// בנאי ריק הנדרש ל-AutoMapper.
        /// DateTime.MinValue = ערך DateTime הקטן ביותר (01/01/0001).
        /// </summary>
        public TimeBlock() : this(DateTime.MinValue, DateTime.MinValue) { }
    }
}
