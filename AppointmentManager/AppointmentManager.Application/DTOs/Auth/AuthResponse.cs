// =====================================
// קובץ: AuthResponse.cs
// שכבה: Application → DTOs → Auth
// תפקיד: מגדיר את מבנה התגובה שמוחזרת ללקוח לאחר התחברות/הרשמה מוצלחת.
//         מכיל את ה-JWT Token שישמש לזיהוי בכל בקשה עתידית.
// =====================================

namespace AppointmentManager.Application.DTOs.Auth;

/// <summary>
/// תגובת אימות מוצלחת שנשלחת ללקוח (Frontend) לאחר התחברות או הרשמה.
/// הלקוח שומר את ה-Token ומצרף אותו לכל בקשה עתידית בכותרת Authorization.
/// </summary>
/// <param name="Token">JWT Token - מחרוזת מוצפנת המזהה את המשתמש ותפקידו</param>
/// <param name="FullName">שם מלא של המשתמש - להצגה בממשק המשתמש</param>
/// <param name="Role">תפקיד המשתמש ("Admin" / "Client") - לשליטה על מה מוצג בממשק</param>
public record AuthResponse(string Token, string FullName, string Role)
{
    /// <summary>
    /// בנאי ריק הנדרש ל-JSON Deserialization ו-AutoMapper.
    /// string.Empty = "" (מחרוזת ריקה, לא null).
    /// </summary>
    public AuthResponse() : this(string.Empty, string.Empty, string.Empty) { }
}
