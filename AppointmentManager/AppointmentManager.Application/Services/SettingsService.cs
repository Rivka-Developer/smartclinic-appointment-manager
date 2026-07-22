// =====================================
// קובץ: SettingsService.cs
// שכבה: Application → Services
// תפקיד: מימוש שירות הגדרות מערכת.
//         אחראי על קריאה ועדכון של הגדרות הקליניקה (באפר, MinGap, שעות ערב וכו').
// =====================================

using AppointmentManager.Application.DTOs;
using AppointmentManager.Application.Interfaces;
using AppointmentManager.Domain.Entities;
using AppointmentManager.Domain.Interfaces;
using AppointmentManager.Domain.Common;
using AutoMapper;
using Microsoft.Extensions.Caching.Memory;

namespace AppointmentManager.Application.Services;

/// <summary>
/// מימוש שירות הגדרות מערכת.
/// </summary>
public class SettingsService(IUnitOfWork uow, IMapper mapper, IMemoryCache cache) : ISettingsService
{
    /// <summary>
    /// מחזיר את ההגדרות הנוכחיות של המערכת.
    /// </summary>
    public async Task<Result<SystemSettingsDto>> GetSettingsAsync()
    {
        // שליפת ההגדרות מבסיס הנתונים דרך ה-Repository
        // (SettingsRepository יוצר ברירת מחדל אם אין הגדרות)
        var settings = await uow.Settings.GetSettingsAsync();

        // אימות תקינות - בפועל לא אמור לקרות אחרי ה-Seeding ב-Program.cs
        if (settings == null)
        {
            return Result.Failure<SystemSettingsDto>(Error.NotFound("Settings.NotFound", "הגדרות מערכת לא נמצאו."));
        }

        // המרה מישות הדומיין (SystemSettings) ל-DTO (SystemSettingsDto)
        var dto = mapper.Map<SystemSettingsDto>(settings);

        return Result.Success(dto);
    }

    /// <summary>
    /// מעדכן את הגדרות המערכת עם הנתונים החדשים שנשלחו.
    /// </summary>
    public async Task<Result> UpdateSettingsAsync(SystemSettingsDto settingsDto)
    {
        // אימות קלט בסיסי
        if (settingsDto == null)
            return Result.Failure(Error.NullValue);

        // שליפת ההגדרות הקיימות (מאחר שיש רק רשומה אחת)
        var existing = await uow.Settings.GetSettingsAsync();

        // עדכון שדות ה-existing מה-settingsDto (AutoMapper מעדכן שדות קיימים)
        // Id לא יעודכן כי הוגדר Ignore ב-MappingProfile.cs
        mapper.Map(settingsDto, existing);

        await uow.SaveChangesAsync();

        // ביטול cache כדי ש-AvailabilityService יטען את ההגדרות החדשות
        cache.Remove("SystemSettings");

        return Result.Success();
    }
}
