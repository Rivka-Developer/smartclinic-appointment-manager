// =====================================
// קובץ: RegisterRequest.cs
// שכבה: Application → DTOs → Auth
// תפקיד: מגדיר את מבנה הבקשה להרשמה למערכת כלקוח חדש.
//         כולל אימותי קלט שמבוצעים אוטומטית לפני הגעה לשירות.
// =====================================

using System.ComponentModel.DataAnnotations;

namespace AppointmentManager.Application.DTOs.Auth;

/// <summary>
/// נתוני הרשמה שמגיעים מהלקוח (Frontend) ל-API.
/// כל שדה מכיל אימותים שמסוננים אוטומטית לפני הגעה לשירות.
/// </summary>
/// <param name="FullName">שם מלא (2-100 תווים, שדה חובה)</param>
/// <param name="Email">כתובת אימייל (שדה חובה, פורמט תקין, עד 150 תווים)</param>
/// <param name="PhoneNumber">מספר טלפון ישראלי (שדה חובה, פורמט מוגדר)</param>
/// <param name="Password">סיסמה (שדה חובה, לפחות 6 תווים)</param>
public record RegisterRequest(
    [Required][MinLength(2)][MaxLength(100)] string FullName,       // [MinLength(2)] = מינימום 2 תווים | [MaxLength(100)] = עד 100 תווים
    [Required][EmailAddress][MaxLength(150)] string Email,           // [EmailAddress] = מוודא פורמט אימייל תקין
    [Required][RegularExpression(                                    // [RegularExpression] = בדיקה מול תבנית (Regex)
        @"^0[2-9]\d{7,8}$",                                         // התבנית: מתחיל ב-0, ספרה 2-9, ואז 7-8 ספרות
        ErrorMessage = "מספר טלפון לא תקין. יש להזין מספר ישראלי תקין (לדוגמה: 0501234567)")]
        string PhoneNumber,
    [Required][MinLength(6)] string Password)                        // [MinLength(6)] = לפחות 6 תווים בסיסמה
{
    /// <summary>
    /// בנאי ריק הנדרש ל-JSON Deserialization.
    /// string.Empty = מחרוזת ריקה "" (לא null).
    /// </summary>
    public RegisterRequest() : this(string.Empty, string.Empty, string.Empty, string.Empty) { }
}
