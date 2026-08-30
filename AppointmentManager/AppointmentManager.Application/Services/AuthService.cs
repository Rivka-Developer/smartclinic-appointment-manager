// =====================================
// קובץ: AuthService.cs
// שכבה: Application → Services
// תפקיד: מימוש שירות האימות - הרשמה והתחברות.
//         אחראי על: בדיקת כפילות אימייל, הצפנת סיסמאות, יצירת JWT Token.
//         JWT (JSON Web Token) = מחרוזת מוצפנת שמכילה מידע על המשתמש
//         ומועברת בכל בקשה לאימות הזהות.
// =====================================

using System.IdentityModel.Tokens.Jwt; // לעבודה עם JWT Tokens
using System.Security.Claims;           // ל-Claims (פיסות מידע בתוך ה-Token)
using System.Text;                      // ל-Encoding.UTF8
using AppointmentManager.Application.DTOs.Auth;
using AppointmentManager.Application.Interfaces;
using AppointmentManager.Domain;
using AppointmentManager.Domain.Entities;
using AppointmentManager.Domain.Interfaces;
using AppointmentManager.Domain.Common;
using Google.Apis.Auth;                   // לאימות ID Token מול Google
using Microsoft.Extensions.Configuration; // לקריאת הגדרות מ-appsettings.json
using Microsoft.IdentityModel.Tokens;     // ל-SymmetricSecurityKey ו-SigningCredentials
using BC = BCrypt.Net.BCrypt;             // כינוי קצר לספריית BCrypt לצורך הצפנת סיסמאות

namespace AppointmentManager.Application.Services;

/// <summary>
/// מימוש שירות האימות.
/// Primary Constructor: הפרמטרים מוזרקים אוטומטית על ידי ה-DI Container.
/// IUnitOfWork = גישה לכל ה-Repositories.
/// IConfiguration = גישה לקובץ ההגדרות (appsettings.json).
/// </summary>
public class AuthService(IUnitOfWork uow, IConfiguration config) : IAuthService
{
    /// <summary>
    /// רושם משתמש חדש למערכת.
    /// </summary>
    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request)
    {
        // בדיקה: האם כבר קיים משתמש עם אימייל זה?
        var existingUser = await uow.Users.GetByEmailAsync(request.Email);
        if (existingUser != null)
            return Result.Failure<AuthResponse>(AuthErrors.UserAlreadyExists); // שגיאה: אימייל קיים

        // יצירת משתמש חדש עם הנתונים מהבקשה
        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            PasswordHash = BC.HashPassword(request.Password), // הצפנת הסיסמה - לא שומרים את הסיסמה המקורית!
            Role = UserRole.Client // כל רישום עצמאי מקבל תפקיד Client (לא Admin)
        };

        // הוספת המשתמש לתור ה-Changes
        await uow.Users.AddAsync(user);

        // שמירה בפועל לבסיס הנתונים
        await uow.SaveChangesAsync();

        // יצירת JWT Token עבור המשתמש החדש
        var token = GenerateJwtToken(user);

        // בניית התגובה שתוחזר ל-Frontend
        var response = new AuthResponse(token, user.FullName, user.Role.ToString());

        return Result.Success(response); // הצלחה עם ה-Token
    }

    /// <summary>
    /// מאמת משתמש ומתחבר למערכת.
    /// </summary>
    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request)
    {
        // חיפוש המשתמש לפי אימייל
        var user = await uow.Users.GetByEmailAsync(request.Email);

        // בדיקת קיום המשתמש ואימות הסיסמה
        // BC.Verify = בודק אם הסיסמה שהוזנה תואמת ל-Hash השמור
        // (לא ניתן "לפענח" Hash - רק לאמת)
        // PasswordHash == null אצל משתמשים שנרשמו רק דרך Google - אין להם סיסמה לאמת מולה.
        if (user == null || user.PasswordHash == null || !BC.Verify(request.Password, user.PasswordHash))
            return Result.Failure<AuthResponse>(AuthErrors.InvalidCredentials); // שגיאה כללית (לא מפרטים מה שגוי)

        // יצירת Token
        var token = GenerateJwtToken(user);
        var response = new AuthResponse(token, user.FullName, user.Role.ToString());

        return Result.Success(response);
    }

    /// <summary>
    /// מתחבר (או נרשם אוטומטית) עם חשבון Google.
    /// </summary>
    public async Task<Result<AuthResponse>> GoogleLoginAsync(GoogleLoginRequest request)
    {
        GoogleJsonWebSignature.Payload payload;
        try
        {
            // אימות ה-ID Token מול שרתי Google: בודק חתימה, תוקף, ושה-Audience תואם ל-Client ID שלנו.
            payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { config["Authentication:Google:ClientId"]! }
            });
        }
        catch (InvalidJwtException)
        {
            return Result.Failure<AuthResponse>(AuthErrors.InvalidGoogleToken);
        }

        // חיפוש משתמש קיים לפי אימייל - אם נרשם בעבר עם סיסמה, מקשרים את חשבון Google אליו.
        var user = await uow.Users.GetByEmailAsync(payload.Email);

        if (user == null)
        {
            // משתמש חדש - נוצר כ-Client רגיל, ללא סיסמה.
            user = new User
            {
                FullName = payload.Name ?? payload.Email,
                Email = payload.Email,
                PhoneNumber = string.Empty,
                PasswordHash = null,
                GoogleId = payload.Subject,
                Role = UserRole.Client
            };
            await uow.Users.AddAsync(user);
            await uow.SaveChangesAsync();
        }
        else if (user.GoogleId == null)
        {
            // משתמש קיים שנרשם עם סיסמה - קישור חשבון Google אליו.
            user.GoogleId = payload.Subject;
            await uow.SaveChangesAsync();
        }

        var token = GenerateJwtToken(user);
        var response = new AuthResponse(token, user.FullName, user.Role.ToString());
        return Result.Success(response);
    }

    /// <summary>
    /// יוצר JWT Token עם פרטי המשתמש כ-Claims.
    /// JWT = שלושה חלקים מוצפנים: Header.Payload.Signature
    /// Claims = פיסות מידע בתוך ה-Payload (מה המשתמש יודע עליו עצמו).
    /// </summary>
    private string GenerateJwtToken(User user)
    {
        // יצירת מפתח ה-Signing מהקונפיגורציה (חייב להיות לפחות 32 תווים)
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));

        // הגדרת אלגוריתם החתימה: HmacSha256
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        // הגדרת ה-Claims - פרטים שיהיו בתוך ה-Token
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), // מזהה המשתמש - לשליפה בכל בקשה
            new Claim(ClaimTypes.Email, user.Email),                  // האימייל
            new Claim(ClaimTypes.Role, user.Role.ToString()),         // התפקיד - לבדיקת הרשאות
            new Claim("FullName", user.FullName)                      // שם מלא - להצגה בממשק
        };

        // בניית ה-Token
        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],         // מי הנפיק את ה-Token (שם השרת)
            audience: config["Jwt:Audience"],     // למי ה-Token מיועד (שם האפליקציה)
            claims: claims,                       // הפרטים שבתוך ה-Token
            expires: DateTime.UtcNow.AddHours(2),  // ה-Token תקף ל-2 שעות
            signingCredentials: credentials);     // החתימה הדיגיטלית

        // המרת ה-Token לפורמט מחרוזת (Base64URL)
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
