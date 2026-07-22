// =====================================
// קובץ: WorkShiftErrors.cs
// שכבה: Domain → Common
// תפקיד: מרכז את כל השגיאות הקשורות למשמרות עבודה.
//         כאשר מנהל/ת מנסה להוסיף/לעדכן משמרת לא תקינה,
//         אחת מהשגיאות האלה תוחזר.
// =====================================

using AppointmentManager.Domain.Common;

namespace AppointmentManager.Domain.Common
{
    /// <summary>
    /// אוסף שגיאות מוגדרות מראש עבור פעולות משמרות עבודה.
    /// </summary>
    public static class WorkShiftErrors
    {
        /// <summary>
        /// שגיאה: ניסיון לגשת למשמרת עם מזהה שאינו קיים.
        /// לדוגמה: עדכון/מחיקת משמרת שכבר נמחקה.
        /// HTTP 404 Not Found.
        /// </summary>
        public static Error NotFound => Error.NotFound("WorkShift.NotFound", "המשמרת המבוקשת לא נמצאה.");

        /// <summary>
        /// שגיאה: הוספת משמרת שחופפת לזמן של משמרת קיימת.
        /// לדוגמה: יש משמרת 09:00-13:00, ומנסים להוסיף 12:00-16:00 (חפיפה בשעה 12:00-13:00).
        /// HTTP 409 Conflict.
        /// </summary>
        public static Error Overlap => Error.Conflict("WorkShift.Overlap", "קיימת כבר משמרת בחלק מהזמן שנבחר.");

        /// <summary>
        /// שגיאה: שעת הסיום של המשמרת קודמת לשעת ההתחלה.
        /// לדוגמה: StartTime=14:00, EndTime=10:00 - לא הגיוני.
        /// HTTP 400 Bad Request.
        /// </summary>
        public static Error InvalidTime => Error.Validation("WorkShift.InvalidTime", "שעת סיום חייבת להיות אחרי שעת התחלה.");

        /// <summary>
        /// שגיאה: ניסיון להוסיף משמרת ביום שישי או שבת.
        /// HTTP 400 Bad Request.
        /// </summary>
        public static Error WeekendNotAllowed => Error.Validation("WorkShift.WeekendNotAllowed", "לא ניתן להוסיף משמרות בימי שישי ושבת.");
    }
}
