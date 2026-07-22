// =====================================
// קובץ: LoginRequest.cs
// שכבה: Application → DTOs → Auth
// תפקיד: מגדיר את מבנה הבקשה להתחברות למערכת.
//         DTO (Data Transfer Object) = אובייקט להעברת נתונים בין שכבות.
//         "record" = מבנה נתונים בלתי-ניתן לשינוי (Immutable) המתאים לבקשות API.
//         [Required] = שדה חובה (לא יתקבל בקשה ריקה).
//         [EmailAddress] = בדיקת פורמט אימייל תקין.
//         [MinLength(6)] = לפחות 6 תווים בסיסמה.
// =====================================

using System.ComponentModel.DataAnnotations;

namespace AppointmentManager.Application.DTOs.Auth;

/// <summary>
/// נתוני התחברות שמגיעים מהלקוח (Frontend) ל-API.
/// המערכת מאמתת פורמטים לפני שמגיעים לשירות.
/// </summary>
/// <param name="Email">כתובת האימייל (חייבת להיות בפורמט תקין כמו user@example.com)</param>
/// <param name="Password">הסיסמה (לפחות 6 תווים)</param>
public record LoginRequest(
    [Required][EmailAddress] string Email,       // [Required] = שדה חובה | [EmailAddress] = מוודא פורמט אימייל
    [Required][MinLength(6)] string Password)    // [Required] = שדה חובה | [MinLength(6)] = מינימום 6 תווים
{
    /// <summary>
    /// בנאי ריק הנדרש ל-JSON Deserialization.
    /// כאשר JSON מגיע מהלקוח, ה-Framework צריך יכולת לצור אובייקט ריק ואז למלא שדות.
    /// ": this(...)" = קורא לבנאי הראשי עם ערכים ריקים.
    /// </summary>
    public LoginRequest() : this(string.Empty, string.Empty) { }
}
