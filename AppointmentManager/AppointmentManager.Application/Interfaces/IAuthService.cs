// =====================================
// קובץ: IAuthService.cs
// שכבה: Application → Interfaces
// תפקיד: מגדיר "חוזה" לשירות האימות (Authentication).
//         Interface = רשימת מתודות שהמימוש (AuthService.cs) חייב לספק.
//         ה-API Controllers תלויים בממשק זה ולא במימוש הספציפי.
// =====================================

using System.Threading.Tasks;
using AppointmentManager.Application.DTOs.Auth;
using AppointmentManager.Domain.Common;

namespace AppointmentManager.Application.Interfaces
{
    /// <summary>
    /// חוזה לשירות אימות: הרשמה והתחברות.
    /// המימוש האמיתי ב-Services/AuthService.cs.
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// מבצע הרשמת משתמש חדש.
        /// בודק שהאימייל לא קיים, מצפין סיסמה, ויוצר JWT Token.
        /// </summary>
        /// <param name="request">נתוני ההרשמה (שם, אימייל, טלפון, סיסמה)</param>
        /// <returns>
        /// Result.Success עם AuthResponse (Token + שם + תפקיד) אם ההרשמה הצליחה.
        /// Result.Failure עם AuthErrors.UserAlreadyExists אם האימייל כבר קיים.
        /// </returns>
        Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request);

        /// <summary>
        /// מבצע התחברות למערכת.
        /// בודק שהאימייל קיים ושהסיסמה תואמת, ויוצר JWT Token.
        /// </summary>
        /// <param name="request">נתוני ההתחברות (אימייל + סיסמה)</param>
        /// <returns>
        /// Result.Success עם AuthResponse (Token + שם + תפקיד) אם ההתחברות הצליחה.
        /// Result.Failure עם AuthErrors.InvalidCredentials אם האימייל/סיסמה שגויים.
        /// </returns>
        Task<Result<AuthResponse>> LoginAsync(LoginRequest request);

        /// <summary>
        /// מבצע התחברות (או הרשמה אוטומטית) עם חשבון Google.
        /// מאמת את ה-ID Token מול Google, ולפי האימייל: מקשר משתמש קיים
        /// או יוצר משתמש Client חדש ללא סיסמה.
        /// </summary>
        /// <param name="request">ה-ID Token שהתקבל מ-Google Identity Services בצד הלקוח</param>
        /// <returns>
        /// Result.Success עם AuthResponse (Token + שם + תפקיד) אם ההתחברות הצליחה.
        /// Result.Failure עם AuthErrors.InvalidGoogleToken אם ה-Token לא תקין.
        /// </returns>
        Task<Result<AuthResponse>> GoogleLoginAsync(GoogleLoginRequest request);
    }
}
