// =====================================
// קובץ: WorkShiftService.cs
// שכבה: Application → Services
// תפקיד: מימוש שירות ניהול משמרות עבודה.
//         אחראי על: הוספה, עדכון, מחיקה ושליפת משמרות.
//         בודק תקינות זמנים וחפיפות לפני כל שינוי.
// =====================================

using AppointmentManager.Application.DTOs;
using AppointmentManager.Application.Interfaces;
using AppointmentManager.Domain.Entities;
using AppointmentManager.Domain.Interfaces;
using AppointmentManager.Domain.Common;
using AutoMapper;

namespace AppointmentManager.Application.Services;

/// <summary>
/// מימוש שירות ניהול משמרות.
/// IUnitOfWork = גישה לכל ה-Repositories.
/// IMapper = AutoMapper לממרה בין DTO לישות ולהיפך.
/// </summary>
public class WorkShiftService(IUnitOfWork uow, IMapper mapper) : IWorkShiftService
{
    /// <summary>
    /// מחזיר את כל המשמרות ביום ספציפי, ממוינות לפי שעת התחלה.
    /// </summary>
    public async Task<Result<IEnumerable<WorkShiftResponse>>> GetWorkShiftsByDateAsync(DateTime date)
    {
        // שליפת המשמרות מבסיס הנתונים דרך ה-Repository
        var shifts = await uow.Shifts.GetSortedShiftsByDateAsync(date);

        // המרה מישות הדומיין (WorkShift) ל-DTO (WorkShiftResponse) באמצעות AutoMapper
        var response = mapper.Map<IEnumerable<WorkShiftResponse>>(shifts);

        return Result.Success(response);
    }

    /// <summary>
    /// מוסיף משמרת חדשה לבסיס הנתונים.
    /// בודק תקינות זמנים וחפיפות עם משמרות קיימות.
    /// </summary>
    public async Task<Result> AddWorkShiftAsync(WorkShiftRequest request)
    {
        // בדיקת null - Error.NullValue הוגדר ב-Error.cs
        if (request == null) return Result.Failure(Error.NullValue);

        // בדיקה: אין משמרות בשישי ושבת
        if (request.Date.DayOfWeek == DayOfWeek.Friday || request.Date.DayOfWeek == DayOfWeek.Saturday)
            return Result.Failure(WorkShiftErrors.WeekendNotAllowed);

        // בדיקת תקינות: שעת סיום חייבת להיות אחרי שעת התחלה
        // TimeSpan השוואה: 09:00:00 <= 13:00:00
        if (request.EndTime <= request.StartTime)
            return Result.Failure(WorkShiftErrors.InvalidTime);

        // שליפת משמרות קיימות ביום זה לבדיקת חפיפה
        var existingShifts = await uow.Shifts.GetSortedShiftsByDateAsync(request.Date);

        // בדיקת חפיפה: האם המשמרת החדשה חופפת למשמרת קיימת?
        // תנאי חפיפה: ההתחלה החדשה לפני הסוף של הקיימת AND הסוף החדש אחרי ההתחלה של הקיימת
        if (existingShifts.Any(s => request.StartTime < s.EndTime && request.EndTime > s.StartTime))
            return Result.Failure(WorkShiftErrors.Overlap);

        // המרה מ-DTO לישות הדומיין
        var shift = mapper.Map<WorkShift>(request);

        // הוספה לתור ה-Changes
        await uow.Shifts.AddAsync(shift);

        // שמירה לבסיס הנתונים
        await uow.SaveChangesAsync();

        return Result.Success();
    }

    /// <summary>
    /// מעדכן משמרת קיימת לפי מזהה.
    /// </summary>
    public async Task<Result> UpdateWorkShiftAsync(Guid id, WorkShiftRequest request)
    {
        if (request == null) return Result.Failure(Error.NullValue);

        // בדיקה: אין משמרות בשישי ושבת
        if (request.Date.DayOfWeek == DayOfWeek.Friday || request.Date.DayOfWeek == DayOfWeek.Saturday)
            return Result.Failure(WorkShiftErrors.WeekendNotAllowed);

        // חיפוש המשמרת לעדכון
        var shift = await uow.Shifts.GetByIdAsync(id);
        if (shift == null) return Result.Failure(WorkShiftErrors.NotFound); // לא נמצאה

        // בדיקת תקינות זמנים
        if (request.EndTime <= request.StartTime)
            return Result.Failure(WorkShiftErrors.InvalidTime);

        // בדיקת חפיפה - מתעלמת מהמשמרת הנוכחית עצמה (s.Id != id)
        // כלומר: מאפשרת "עדכון" שמשאיר את אותן שעות בדיוק
        var existingShifts = await uow.Shifts.GetSortedShiftsByDateAsync(request.Date);
        if (existingShifts.Any(s => s.Id != id && request.StartTime < s.EndTime && request.EndTime > s.StartTime))
            return Result.Failure(WorkShiftErrors.Overlap);

        // עדכון שדות הישות מה-DTO (AutoMapper מעדכן שדות קיימים)
        mapper.Map(request, shift);

        // סימון לעדכון ב-EF
        await uow.Shifts.UpdateAsync(shift);

        // שמירה לבסיס הנתונים
        await uow.SaveChangesAsync();

        return Result.Success();
    }

    /// <summary>
    /// מוחק משמרת לחלוטין מבסיס הנתונים (Hard Delete).
    /// </summary>
    public async Task<Result> DeleteWorkShiftAsync(Guid id)
    {
        // מחיקה דרך ה-Repository (ראה WorkShiftRepository.DeleteAsync)
        await uow.Shifts.DeleteAsync(id);

        // שמירה לבסיס הנתונים
        await uow.SaveChangesAsync();

        return Result.Success();
    }
}
