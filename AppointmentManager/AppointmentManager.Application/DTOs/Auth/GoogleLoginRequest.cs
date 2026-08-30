// =====================================
// קובץ: GoogleLoginRequest.cs
// שכבה: Application → DTOs → Auth
// תפקיד: מגדיר את מבנה הבקשה להתחברות עם Google.
//         ה-Frontend מקבל ID Token חתום מ-Google Identity Services
//         ושולח אותו לשרת לאימות.
// =====================================

using System.ComponentModel.DataAnnotations;

namespace AppointmentManager.Application.DTOs.Auth;

/// <summary>
/// נתוני התחברות עם Google שמגיעים מהלקוח (Frontend) ל-API.
/// </summary>
/// <param name="IdToken">ה-ID Token החתום שהתקבל מ-Google Identity Services בצד הלקוח</param>
public record GoogleLoginRequest([Required] string IdToken)
{
    /// <summary>
    /// בנאי ריק הנדרש ל-JSON Deserialization.
    /// </summary>
    public GoogleLoginRequest() : this(string.Empty) { }
}
