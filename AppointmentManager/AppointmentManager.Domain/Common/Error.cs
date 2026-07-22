// =====================================
// קובץ: Error.cs
// שכבה: Domain → Common (משותף לכל השכבות)
// תפקיד: מגדיר את מבנה השגיאה הסטנדרטי של המערכת.
//         במקום לזרוק חריגות (Exceptions) לכל בעיה,
//         המערכת משתמשת בסוג Error מובנה שמכיל קוד, תיאור, וסוג השגיאה.
//         גישה זו מאפשרת טיפול בשגיאות בצורה בטוחה וצפויה.
// =====================================

namespace AppointmentManager.Domain.Common
{
    /// <summary>
    /// מייצג שגיאה עסקית בצורה מובנית ואחידה.
    /// "record" ב-C# הוא מחלקה ש:
    ///   1. בלתי ניתנת לשינוי (Immutable) - לאחר יצירה הנתונים לא משתנים.
    ///   2. השוואה לפי ערכים (Value Equality) - שתי שגיאות עם אותם נתונים נחשבות שוות.
    /// </summary>
    /// <param name="Code">קוד שגיאה קצר לזיהוי (לדוגמה: "Auth.UserAlreadyExists")</param>
    /// <param name="Description">תיאור השגיאה בעברית למשתמש (לדוגמה: "משתמש עם אימייל זה כבר קיים")</param>
    /// <param name="Type">סוג השגיאה - קובע את קוד HTTP שיוחזר ללקוח</param>
    public record Error(string Code, string Description, ErrorType Type)
    {
        /// <summary>
        /// שגיאה ריקה - מייצגת "אין שגיאה".
        /// string.Empty = מחרוזת ריקה "".
        /// משמשת כברירת מחדל ב-Result.Success().
        /// </summary>
        public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);

        /// <summary>
        /// שגיאה מוגדרת-מראש לכל מקרה שבו הוזן ערך null כשלא אמור להיות.
        /// </summary>
        public static readonly Error NullValue = new("Error.NullValue", "הערך שהוזן הוא null", ErrorType.Validation);

        // --- פקטורי-מתודות (Factory Methods) ליצירת שגיאות לפי סוג ---
        // כל מתודה יוצרת שגיאה מהסוג המתאים עם הקוד והתיאור שמועברים אליה.

        /// <summary>יוצרת שגיאת אימות קלט (400 Bad Request)</summary>
        public static Error Validation(string code, string description) => new(code, description, ErrorType.Validation);

        /// <summary>יוצרת שגיאת "לא נמצא" (404 Not Found)</summary>
        public static Error NotFound(string code, string description) => new(code, description, ErrorType.NotFound);

        /// <summary>יוצרת שגיאת "קיים כבר" / "התנגשות" (409 Conflict)</summary>
        public static Error Conflict(string code, string description) => new(code, description, ErrorType.Conflict);

        /// <summary>יוצרת שגיאת כשל כללי (500 Internal Server Error)</summary>
        public static Error Failure(string code, string description) => new(code, description, ErrorType.Failure);

        /// <summary>יוצרת שגיאת הרשאה (401 Unauthorized)</summary>
        public static Error Unauthorized(string code, string description) => new(code, description, ErrorType.Unauthorized);
    }

    /// <summary>
    /// סוגי השגיאות האפשריות במערכת.
    /// הסוג קובע את קוד ה-HTTP Status Code שיוחזר ב-API.
    /// </summary>
    public enum ErrorType
    {
        /// <summary>כשל כללי לא מוגדר - 500 Internal Server Error</summary>
        Failure = 0,

        /// <summary>שגיאת אימות קלט - 400 Bad Request (לדוגמה: שדה חסר, פורמט שגוי)</summary>
        Validation = 1,

        /// <summary>משאב לא נמצא - 404 Not Found (לדוגמה: תור שאינו קיים)</summary>
        NotFound = 2,

        /// <summary>התנגשות עם מצב קיים - 409 Conflict (לדוגמה: אימייל כבר רשום)</summary>
        Conflict = 3,

        /// <summary>אין הרשאה לפעולה - 401 Unauthorized (לדוגמה: ניסיון לבטל תור של אחר)</summary>
        Unauthorized = 4
    }
}
