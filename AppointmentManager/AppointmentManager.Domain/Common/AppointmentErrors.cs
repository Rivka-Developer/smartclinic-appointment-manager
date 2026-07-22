// =====================================
// קובץ: AppointmentErrors.cs
// שכבה: Domain → Common
// תפקיד: מרכז את כל השגיאות הקשורות לתורים.
//         כל שגיאה היא Property (או מתודה) המחזיר/ת אובייקט Error מוגדר.
//         ריכוז כאן מאפשר לשנות הודעה ממקום אחד ולא לחפש בכל הקוד.
// =====================================

using AppointmentManager.Domain.Common;

namespace AppointmentManager.Domain.Common
{
    /// <summary>
    /// אוסף שגיאות מוגדרות מראש עבור פעולות תורים.
    /// </summary>
    public static class AppointmentErrors
    {
        /// <summary>
        /// שגיאה: ניסיון לקבוע תור לזמן שכבר עבר.
        /// לדוגמה: קביעת תור ל-09:00 כאשר השעה כבר 10:00.
        /// HTTP 400 Bad Request.
        /// </summary>
        public static Error PastDate => Error.Validation("Appointment.PastDate", "לא ניתן לקבוע תור לזמן שעבר.");

        /// <summary>
        /// שגיאה: זמן התחלת התור אינו בכפולות של 5 דקות.
        /// לדוגמה: 10:03 שגוי, 10:05 תקין.
        /// HTTP 400 Bad Request.
        /// </summary>
        public static Error InvalidInterval => Error.Validation("Appointment.InvalidInterval", "הזמן חייב להיות בכפולות של 5 דקות.");

        /// <summary>
        /// שגיאה: משך התור ארוך מהמקסימום המותר לאותה שעה ביום.
        /// מקבלת פרמטר "max" שמציין מה המקסימום הספציפי.
        /// זוהי מתודה (לא Property) כי היא מקבלת ארגומנט.
        /// HTTP 400 Bad Request.
        /// </summary>
        /// <param name="max">המשך המקסימלי המותר בדקות</param>
        public static Error TooLong(int max) => Error.Validation("Appointment.TooLong", $"בשעה זו ניתן לקבוע מקסימום {max} דקות.");

        /// <summary>
        /// שגיאה: הזמן שנבחר אינו פנוי - אין חלון זמן מתאים.
        /// לדוגמה: הזמן תפוס או גולש מחוץ למשמרת.
        /// HTTP 409 Conflict.
        /// </summary>
        public static Error NoSlotFound => Error.Conflict("Appointment.NoSlot", "הזמן שנבחר אינו זמין או גולש מחוץ למשמרת.");

        /// <summary>
        /// שגיאה: הצבת התור תשאיר "חור" קטן מדי ביומן.
        /// חור קטן מ-MinGapSize דקות אינו שימושי ולא ניתן למלא בתור אחר.
        /// HTTP 400 Bad Request.
        /// </summary>
        public static Error SmallGap => Error.Validation("Appointment.SmallGap", "הבחירה משאירה חור קטן מדי ביומן (פחות מ-15 דקות).");

        /// <summary>
        /// שגיאה: ניסיון לבטל תור בתוך חלון הזמן האסור לביטול.
        /// ברירת מחדל: לא ניתן לבטל פחות מ-24 שעות לפני.
        /// HTTP 400 Bad Request.
        /// </summary>
        public static Error CannotCancel => Error.Validation("Appointment.CannotCancel", "לא ניתן לבטל תור פחות מ-24 שעות לפני המועד.");

        /// <summary>
        /// שגיאה: ניסיון לקבוע תור ליום המחרת לאחר שעת הסגירה (23:00).
        /// HTTP 400 Bad Request.
        /// </summary>
        public static Error LateNightCutoff => Error.Validation("Appointment.LateNightCutoff", "הזמנת תורים ליום המחרת נסגרת בשעה 23:00. אנא צרי קשר ישירות עם המרפאה.");

        /// <summary>
        /// שגיאה: חיפוש תור לפי מזהה לא הניב תוצאה.
        /// HTTP 404 Not Found.
        /// </summary>
        public static Error NotFound => Error.NotFound("Appointment.NotFound", "התור המבוקש לא נמצא.");

        /// <summary>
        /// שגיאה: ניסיון לקבוע תור ביום שישי או שבת.
        /// HTTP 400 Bad Request.
        /// </summary>
        public static Error WeekendNotAllowed => Error.Validation("Appointment.WeekendNotAllowed", "לא ניתן לקבוע תורים בימי שישי ושבת.");
    }
}
