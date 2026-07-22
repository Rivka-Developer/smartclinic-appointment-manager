// =====================================
// קובץ: UserResponse.cs
// שכבה: Application → DTOs
// תפקיד: מגדיר את מבנה פרטי משתמש כפי שמוחזרים ב-API.
//         שונה מ-User (ישות הדומיין) - לא כולל PasswordHash ונתונים רגישים.
//         כולל TotalAppointments - ספירה שמחושבת בעת המיפוי.
// =====================================

/// <summary>
/// פרטי משתמש/לקוח כפי שמוחזרים למנהל/ת ב-API.
/// לא כולל מידע רגיש (כמו PasswordHash).
/// </summary>
/// <param name="Id">מזהה ייחודי של המשתמש</param>
/// <param name="FullName">שם מלא</param>
/// <param name="PhoneNumber">מספר טלפון</param>
/// <param name="Email">כתובת אימייל</param>
/// <param name="Role">תפקיד כמחרוזת ("Admin" / "Client" / "ManagedClient")</param>
/// <param name="TotalAppointments">מספר התורים הכולל של הלקוח/ה</param>
public record UserResponse(Guid Id, string FullName, string PhoneNumber, string Email, string Role, int TotalAppointments)
{
    /// <summary>
    /// בנאי ריק הנדרש ל-AutoMapper.
    /// "" = מחרוזת ריקה (ולא null) לשדות string.
    /// 0 = אפס לשדות מספריים.
    /// Guid.Empty = מזהה אפסים.
    /// </summary>
    public UserResponse() : this(Guid.Empty, "", "", "", "", 0) {}
}
