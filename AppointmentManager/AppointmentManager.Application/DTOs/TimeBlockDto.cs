// =====================================
// קובץ: TimeBlockDto.cs
// שכבה: Application → DTOs
// תפקיד: גרסה מועשרת של TimeBlock לשליחה ל-Frontend.
//         מוסיפה מידע נוסף: האם הבלוק פנוי, מהם המשכים החוקיים, ומה המקסימום.
//         ה-Frontend משתמש בנתונים אלה להציג ללקוח אילו אפשרויות זמינות.
// =====================================

namespace AppointmentManager.Application.DTOs
{
    /// <summary>
    /// בלוק זמן עשיר בנתונים לשליחה ל-Frontend.
    /// שונה מ-TimeBlock שהוא מבנה פנימי בלבד.
    /// </summary>
    public class TimeBlockDto
    {
        /// <summary>בנאי ריק לאפשור AutoMapper ו-JSON Deserialization</summary>
        public TimeBlockDto() { }

        /// <summary>
        /// בנאי המקבל שעת התחלה, סיום, וסימון פנוי/תפוס.
        /// </summary>
        /// <param name="start">זמן תחילת הבלוק</param>
        /// <param name="end">זמן סיום הבלוק</param>
        /// <param name="isAvailable">האם הבלוק פנוי לקביעת תור?</param>
        public TimeBlockDto(DateTime start, DateTime end, bool isAvailable)
        {
            Start = start;
            End = end;
            IsAvailable = isAvailable; // true = פנוי, false = תפוס
        }

        /// <summary>
        /// בנאי חלופי הסוקר אם הבלוק שייך ללקוח ספציפי.
        /// clientId = null פירושו שהבלוק פנוי (אין לקוח שמחזיק אותו).
        /// int? = int שיכול להיות null ("nullable int").
        /// </summary>
        public TimeBlockDto(DateTime start, DateTime end, int? clientId = null)
        {
            Start = start;
            End = end;
            IsAvailable = clientId is null; // null = פנוי, יש ערך = תפוס
        }

        /// <summary>זמן תחילת הבלוק</summary>
        public DateTime Start { get; set; }

        /// <summary>זמן סיום הבלוק</summary>
        public DateTime End { get; set; }

        /// <summary>האם הבלוק פנוי לקביעת תור? true = כן, false = תפוס</summary>
        public bool IsAvailable { get; set; }

        /// <summary>
        /// משך הבלוק בדקות - מחושב אוטומטית.
        /// (End - Start) = TimeSpan | .TotalMinutes = ממיר ל-double.
        /// לדוגמה: אם Start=09:00 ו-End=11:00, אז DurationMinutes=120.
        /// </summary>
        public double DurationMinutes => (End - Start).TotalMinutes;

        /// <summary>
        /// המשך המקסימלי המותר לתור בבלוק זה, בדקות.
        /// מושפע מהשעה ביום (בוקר/ערב) ומהגדרות המערכת.
        /// </summary>
        public int MaxAllowedDuration { get; set; }

        /// <summary>
        /// רשימת משכי התורים החוקיים שניתן לבחור בבלוק זה, בדקות.
        /// לדוגמה: [15, 20, 25, 30] אם הבלוק גדול מספיק.
        /// ה-Frontend מציג את האפשרויות הללו לבחירה.
        /// </summary>
        public List<int> ValidDurations { get; set; } = new();
    }
}
