// =====================================
// קובץ: IUserService.cs
// שכבה: Application → Interfaces
// תפקיד: מגדיר "חוזה" לשירות ניהול משתמשים.
//         מאפשר למנהל/ת לצפות ברשימת לקוחות ובפרטי כל לקוח.
//         המימוש האמיתי ב-Services/UserService.cs.
// =====================================

using AppointmentManager.Application.DTOs;
using AppointmentManager.Domain.Common;

/// <summary>
/// חוזה לשירות ניהול משתמשים - פעולות צפייה בלבד.
/// כל פעולות הכתיבה (יצירת משתמש, שינוי סיסמה) מתבצעות דרך IAuthService.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// מחזיר רשימה ממוספרת (Paginated) של כל הלקוחות.
    /// ממוינת לפי שם.
    /// </summary>
    /// <param name="pageNumber">מספר עמוד (מתחיל מ-1)</param>
    /// <param name="pageSize">כמה לקוחות בכל עמוד</param>
    /// <returns>PagedResult עם פרטי הלקוחות ומידע על הדפדוף</returns>
    Task<Result<PagedResult<UserResponse>>> GetAllClientsAsync(int pageNumber, int pageSize);

    /// <summary>
    /// מחזיר פרטים מלאים של לקוח אחד כולל כל היסטוריית התורים שלו.
    /// </summary>
    /// <param name="userId">מזהה הלקוח</param>
    /// <returns>UserHistoryResponse עם פרטי הלקוח ורשימת התורים</returns>
    Task<Result<UserHistoryResponse>> GetClientHistoryAsync(Guid userId);

    /// <summary>
    /// מוחק לקוח ואת כל התורים שלו לצמיתות.
    /// </summary>
    /// <param name="userId">מזהה הלקוח למחיקה</param>
    Task<Result> DeleteUserAsync(Guid userId);
}
