// =====================================
// קובץ: UserService.cs
// שכבה: Application → Services
// תפקיד: מימוש שירות ניהול משתמשים - פעולות קריאה בלבד.
//         אחראי על שליפת רשימת לקוחות ופרטי לקוח ספציפי.
// =====================================

using AppointmentManager.Application.DTOs;
using AppointmentManager.Domain.Common;
using AppointmentManager.Domain.Interfaces;
using AutoMapper;
using AppointmentManager.Application.Interfaces;

namespace AppointmentManager.Application.Services;

/// <summary>
/// מימוש שירות ניהול משתמשים.
/// </summary>
public class UserService(IUnitOfWork uow, IMapper mapper) : IUserService
{
    /// <summary>
    /// מחזיר עמוד של לקוחות עם מידע על הדפדוף.
    /// </summary>
    /// <param name="pageNumber">מספר עמוד (מתחיל מ-1)</param>
    /// <param name="pageSize">כמה לקוחות בכל עמוד</param>
    public async Task<Result<PagedResult<UserResponse>>> GetAllClientsAsync(int pageNumber, int pageSize)
    {
        // שליפת לקוחות עם כל התורים שלהם ומספר כולל
        // (clients, total) = Tuple Deconstruction - פריקת tuple לשני משתנים
        var (clients, total) = await uow.Users.GetAllClientsWithAppointmentsAsync(pageNumber, pageSize);

        // המרה מישויות הדומיין (User) ל-DTOs (UserResponse)
        // IEnumerable<UserResponse> - ניתן למעבר רציף על כל הפריטים
        var items = mapper.Map<IEnumerable<UserResponse>>(clients);

        // עטיפה באובייקט PagedResult עם מידע על הדפדוף
        return Result.Success(new PagedResult<UserResponse>(items, total, pageNumber, pageSize));
    }

    /// <summary>
    /// מחזיר פרטי לקוח ספציפי כולל כל היסטוריית התורים.
    /// </summary>
    /// <param name="userId">מזהה הלקוח המבוקש</param>
    public async Task<Result<UserHistoryResponse>> GetClientHistoryAsync(Guid userId)
    {
        // שליפת הלקוח עם כל התורים שלו (JOIN)
        var client = await uow.Users.GetClientWithHistoryAsync(userId);

        // אם הלקוח לא נמצא - החזרת שגיאה
        if (client == null)
            return Result.Failure<UserHistoryResponse>(Error.NotFound("User.NotFound", "הלקוח לא נמצא"));

        // המרה מישות הדומיין (User) ל-DTO (UserHistoryResponse)
        var response = mapper.Map<UserHistoryResponse>(client);

        return Result.Success(response);
    }

    /// <summary>
    /// מוחק לקוח ואת כל התורים שלו לצמיתות.
    /// </summary>
    public async Task<Result> DeleteUserAsync(Guid userId)
    {
        var deleted = await uow.Users.DeleteAsync(userId);

        if (!deleted)
            return Result.Failure(Error.NotFound("User.NotFound", "הלקוח לא נמצא"));

        await uow.SaveChangesAsync();
        return Result.Success();
    }
}
