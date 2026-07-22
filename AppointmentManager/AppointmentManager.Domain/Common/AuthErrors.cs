// =====================================
// קובץ: AuthErrors.cs
// שכבה: Domain → Common
// תפקיד: מרכז את כל השגיאות הקשורות להרשאות ואימות זהות (Authentication/Authorization).
//         "static class" = לא ניתן ליצור מופע ממחלקה זו, גישה ישירה למתודות/שדות.
//         כל השגיאות מוגדרות כ-Properties עם "get" בלבד (לא ניתן לשינוי מחוץ).
//         אחסון כאן (במקום כ-strings ישירות בקוד) מאפשר שינוי הודעות ממקום אחד.
// =====================================

using AppointmentManager.Domain.Common;

namespace AppointmentManager.Domain.Common
{
    /// <summary>
    /// אוסף שגיאות מוגדרות מראש עבור תהליכי אימות והרשאה.
    /// כל Property מחזיר אובייקט Error מוכן לשימוש.
    /// "=>" = יוצר Error חדש בכל גישה לשדה (לא מאוחסן).
    /// </summary>
    public static class AuthErrors
    {
        /// <summary>
        /// שגיאה: נסיון הרשמה עם אימייל שכבר קיים במערכת.
        /// ErrorType.Conflict = HTTP 409 Conflict.
        /// </summary>
        public static Error UserAlreadyExists => Error.Conflict("Auth.UserAlreadyExists", "משתמש עם אימייל זה כבר קיים במערכת.");

        /// <summary>
        /// שגיאה: אימייל או סיסמה שגויים בניסיון התחברות.
        /// לא מפרטים מה בדיוק שגוי (מטעמי אבטחה - לא לחשוף אם האימייל קיים).
        /// ErrorType.Validation = HTTP 400 Bad Request.
        /// </summary>
        public static Error InvalidCredentials => Error.Validation("Auth.InvalidCredentials", "אימייל או סיסמה שגויים.");

        /// <summary>
        /// שגיאה: המשתמש אינו מורשה לבצע את הפעולה המבוקשת.
        /// לדוגמה: לקוח שמנסה לבטל תור של לקוח אחר.
        /// ErrorType.Unauthorized = HTTP 401 Unauthorized.
        /// </summary>
        public static Error Unauthorized => Error.Unauthorized("Auth.Unauthorized", "אין לך הרשאה לבצע פעולה זו.");

        /// <summary>
        /// שגיאה: חיפוש משתמש לפי מזהה לא הניב תוצאה.
        /// ErrorType.NotFound = HTTP 404 Not Found.
        /// </summary>
        public static Error UserNotFound => Error.NotFound("Auth.UserNotFound", "המשתמש המבוקש לא נמצא במערכת.");
    }
}
