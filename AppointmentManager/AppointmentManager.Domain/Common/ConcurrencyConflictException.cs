// =====================================
// קובץ: ConcurrencyConflictException.cs
// שכבה: Domain → Common
// תפקיד: מגדיר חריגה מותאמת אישית לזיהוי התנגשויות מקביליות.
//         "מקביליות" (Concurrency) = שני משתמשים מנסים לבצע פעולה על אותו נתון בו-זמנית.
//         לדוגמה: שני לקוחות מנסים לקבוע תור לאותו חלון זמן פנוי בדיוק באותו רגע.
//         בסיס הנתונים מזהה זאת דרך RowVersion ו-UnitOfWork זורק חריגה זו.
//         הסרביס תופס (catch) חריגה זו ומחזיר שגיאת Conflict מסודרת ללקוח.
// =====================================

namespace AppointmentManager.Domain.Common;

/// <summary>
/// חריגה מותאמת המזהה מצב שבו שני משתמשים ניסו לשנות את אותו נתון בו-זמנית.
/// יורשת מ-Exception - המחלקה הבסיסית לכל החריגות ב-C#.
/// </summary>
public class ConcurrencyConflictException : Exception
{
    /// <summary>
    /// קונסטרקטור ריק - יוצר חריגה עם הודעת ברירת מחדל.
    /// ": base(...)" קורא לקונסטרקטור של מחלקת האב (Exception).
    /// </summary>
    public ConcurrencyConflictException()
        : base("A concurrency conflict occurred.")
    {
    }

    /// <summary>
    /// קונסטרקטור עם הודעה מותאמת אישית.
    /// </summary>
    /// <param name="message">תיאור ההתנגשות</param>
    public ConcurrencyConflictException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// קונסטרקטור עם הודעה מותאמת וחריגה פנימית (Inner Exception).
    /// "Inner Exception" = החריגה המקורית שגרמה לחריגה זו.
    /// שימושי לשימור שרשרת השגיאות המלאה לצורכי debugging.
    /// </summary>
    /// <param name="message">תיאור ההתנגשות</param>
    /// <param name="innerException">החריגה המקורית מ-Entity Framework</param>
    public ConcurrencyConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
