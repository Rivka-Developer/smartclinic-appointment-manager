// =====================================
// קובץ: AppointmentService.cs
// שכבה: Application → Services
// תפקיד: מימוש שירות ניהול תורים - הלוגיקה העסקית המרכזית.
//         אחראי על: קביעת תורים, ביטולם, ושליפת מידע.
//         מטפל בעסקאות (Transactions) לבטיחות במקרה של בקשות מקביליות.
//         שולח אימיילים אישור/ביטול ללקוחות.
// =====================================

using AppointmentManager.Application.DTOs;
using AppointmentManager.Application.Helpers;
using AppointmentManager.Application.Interfaces;
using AppointmentManager.Domain;
using AppointmentManager.Domain.Common;
using AppointmentManager.Domain.Entities;
using AppointmentManager.Domain.Interfaces;
using AutoMapper;
using BC = BCrypt.Net.BCrypt; // לצורך יצירת סיסמה אקראית ל-ManagedClient
using Microsoft.Extensions.Logging;
using System.Data;

namespace AppointmentManager.Application.Services;

/// <summary>
/// מימוש שירות ניהול תורים.
/// Primary Constructor: כל הפרמטרים מוזרקים על ידי ה-DI Container.
/// </summary>
public class AppointmentService(
    IAvailabilityService availabilityService, // לבדיקת זמינות
    IMapper mapper,                           // להמרת DTOs לישויות
    IUnitOfWork uow,                          // לגישה לבסיס הנתונים
    IEmailService emailService,               // לשליחת אימיילים
    ILogger<AppointmentService> logger)       // לרישום לוגים
    : IAppointmentService
{
    /// <summary>
    /// קובע תור במערכת לאחר אימות וסדרת בדיקות.
    /// מבצע פעולה זו בתוך עסקה (Transaction) כדי למנוע תורים כפולים.
    /// </summary>
    public async Task<Result> BookAppointmentAsync(Appointment app)
    {
        var validationResult = app.Validate();
        if (!validationResult.IsSuccess) return validationResult;

        // בדיקה: אין תורים בשישי ושבת
        if (app.StartTime.DayOfWeek == DayOfWeek.Friday || app.StartTime.DayOfWeek == DayOfWeek.Saturday)
            return Result.Failure(AppointmentErrors.WeekendNotAllowed);

        // תאריך T חסום להזמנה אם עברנו את שעת הסגירה שלו: (T - יום) ב-LateBookingCutoffHour
        var settings = await uow.Settings.GetSettingsAsync();
        if (settings != null)
        {
            var cutoff = app.StartTime.Date.AddDays(-1).AddHours(settings.LateBookingCutoffHour);
            if (DateTime.Now >= cutoff)
                return Result.Failure(AppointmentErrors.LateNightCutoff);
        }

        await uow.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            var coreResult = await ExecuteBookingCoreAsync(app);
            if (!coreResult.IsSuccess) { await uow.RollbackAsync(); return coreResult; }

            await uow.CommitAsync();
            logger.LogInformation("תור נקבע בהצלחה - לקוח {ClientId} בשעה {StartTime} למשך {Duration} דקות",
                app.ClientId, app.StartTime, app.DurationMinutes);
        }
        catch (ConcurrencyConflictException)
        {
            await uow.RollbackAsync();
            logger.LogWarning("התנגשות במקביל בהזמנת תור - לקוח {ClientId} בשעה {StartTime}", app.ClientId, app.StartTime);
            return Result.Failure(Error.Conflict("Concurrency.Conflict", "התור נתפס על ידי משתמש אחר."));
        }
        catch (Exception ex)
        {
            await uow.RollbackAsync();
            logger.LogError(ex, "שגיאה בהזמנת תור - לקוח {ClientId} בשעה {StartTime}", app.ClientId, app.StartTime);
            throw;
        }

        // שליחת אימייל אישור מחוץ לטרנזקציה – כישלון לא מבטל תור שנשמר
        await TrySendConfirmationEmailAsync(app, "אישור קביעת תור");
        return Result.Success();
    }

    /// <summary>
    /// קביעת תור על ידי מנהל/ת עבור לקוח לפי שם + טלפון.
    /// יצירת הלקוח והזמנת התור עטופות בטרנזקציה אחת – אם הזמנה נכשלת, הלקוח לא נשמר.
    /// </summary>
    public async Task<Result> AdminBookForClientAsync(AppointmentRequest request)
    {
        // בדיקה: אין תורים בשישי ושבת
        if (request.StartTime.DayOfWeek == DayOfWeek.Friday || request.StartTime.DayOfWeek == DayOfWeek.Saturday)
            return Result.Failure(AppointmentErrors.WeekendNotAllowed);

        await uow.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            var client = await uow.Users.FindByPhoneAsync(request.ClientPhone);

            if (client == null)
            {
                client = new User
                {
                    FullName = request.ClientName,
                    PhoneNumber = request.ClientPhone,
                    Email = $"managed_{Guid.NewGuid():N}@internal.local",
                    PasswordHash = BC.HashPassword(Guid.NewGuid().ToString()),
                    Role = UserRole.ManagedClient
                };
                await uow.Users.AddAsync(client);
                await uow.SaveChangesAsync();
            }

            var app = new Appointment
            {
                ClientId = client.Id,
                StartTime = request.StartTime,
                DurationMinutes = request.DurationMinutes
            };

            var validationResult = app.Validate();
            if (!validationResult.IsSuccess) { await uow.RollbackAsync(); return validationResult; }

            var coreResult = await ExecuteBookingCoreAsync(app);
            if (!coreResult.IsSuccess) { await uow.RollbackAsync(); return coreResult; }

            await uow.CommitAsync();
            logger.LogInformation("תור נקבע על ידי מנהל - לקוח {Phone} בשעה {StartTime}", request.ClientPhone, request.StartTime);
            return Result.Success();
        }
        catch (ConcurrencyConflictException)
        {
            await uow.RollbackAsync();
            return Result.Failure(Error.Conflict("Concurrency.Conflict", "התור נתפס על ידי משתמש אחר."));
        }
        catch (Exception ex)
        {
            await uow.RollbackAsync();
            logger.LogError(ex, "שגיאה בהזמנת תור על ידי מנהל - {Phone}", request.ClientPhone);
            throw;
        }
    }

    // ── פונקציות עזר פרטיות ──────────────────────────────────────────────────

    /// <summary>
    /// לוגיקת הזמנה ללא ניהול טרנזקציה – נקראת מתוך טרנזקציה פתוחה.
    /// </summary>
    private async Task<Result> ExecuteBookingCoreAsync(Appointment app)
    {
        var settings = await uow.Settings.GetSettingsAsync();
        if (settings == null)
            return Result.Failure(AppointmentErrors.NotFound);

        bool isEvening = app.StartTime.TimeOfDay >= settings.EveningStartTime;
        int maxAllowed = isEvening ? settings.EveningMaxDuration : settings.MorningMaxDuration;

        if (app.DurationMinutes > maxAllowed)
        {
            logger.LogWarning("הזמנת תור נדחתה - משך {Duration} עולה על מקסימום {Max} ללקוח {ClientId}",
                app.DurationMinutes, maxAllowed, app.ClientId);
            return Result.Failure(AppointmentErrors.TooLong(maxAllowed));
        }

        var availabilityCheck = await availabilityService.IsSlotAvailableAsync(app.StartTime, app.DurationMinutes);
        if (!availabilityCheck.IsSuccess)
        {
            logger.LogWarning("הזמנת תור נדחתה - חריץ לא זמין בשעה {StartTime} ללקוח {ClientId}",
                app.StartTime, app.ClientId);
            return availabilityCheck;
        }

        await uow.Appointments.AddAsync(app);
        await uow.SaveChangesAsync();
        return Result.Success();
    }

    /// <summary>
    /// שליחת אימייל אישור/ביטול – כישלון נרשם בלבד, לא מעלה חריגה.
    /// </summary>
    private async Task TrySendConfirmationEmailAsync(Appointment app, string subjectPrefix)
    {
        try
        {
            var client = await uow.Users.GetByIdAsync(app.ClientId);
            if (client == null || client.Role == UserRole.ManagedClient || string.IsNullOrEmpty(client.Email))
                return;

            var settings = await uow.Settings.GetSettingsAsync();
            string businessName = settings?.BusinessName ?? "SmartClinic";

            bool isCancellation = subjectPrefix.Contains("ביטול");
            string subject = $"{subjectPrefix} - SmartClinic";
            string body = isCancellation
                ? EmailTemplates.BookingCancellation(client.FullName, app.StartTime, businessName)
                : EmailTemplates.BookingConfirmation(client.FullName, app.StartTime, app.DurationMinutes, businessName);

            await emailService.SendEmailAsync(client.Email, subject, body);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "שגיאה בשליחת אימייל {Prefix} - לקוח {ClientId}", subjectPrefix, app.ClientId);
        }
    }

    /// <summary>
    /// מחזיר בלוקי זמן תפוסים מאוחדים לתצוגת לקוח.
    /// </summary>
    public async Task<Result<List<TimeBlock>>> GetClientViewAsync(DateTime date)
    {
        var apps = await uow.Appointments.GetActiveAppointmentsByDateAsync(date);
        var settings = await uow.Settings.GetSettingsAsync();

        // חישוב בלוקים תפוסים מאוחדים (תורים חופפים מחוברים לגוש אחד)
        var busyBlocks = availabilityService.CalculateMergedBusyBlocks(apps, settings);
        return Result.Success(busyBlocks);
    }

    /// <summary>
    /// מחזיר את כל התורים בטווח תאריכים לצפייה ביומן המנהל/ת.
    /// </summary>
    public async Task<Result<List<AppointmentResponse>>> GetAdminCalendarAsync(DateTime start, DateTime end)
    {
        // שליפה כולל פרטי לקוחות (Include Client)
        var apps = await uow.Appointments.GetWithClientsByDateRangeAsync(start, end);

        // המרה ל-DTOs (AutoMapper)
        var response = mapper.Map<List<AppointmentResponse>>(apps);
        return Result.Success(response);
    }

    /// <summary>
    /// מבטל תור קיים.
    /// לקוח: מוגבל לתור שלו + חלון ביטול.
    /// מנהל: ללא הגבלות.
    /// </summary>
    public async Task<Result> CancelAppointmentAsync(Guid appointmentId, Guid userId, UserRole userRole)
    {
        // חיפוש התור
        var app = await uow.Appointments.GetByIdAsync(appointmentId);
        if (app == null) return Result.Failure(AppointmentErrors.NotFound); // לא נמצא

        // בדיקת הרשאה: לקוח יכול לבטל רק את התור שלו
        if (userRole == UserRole.Client && app.ClientId != userId)
            return Result.Failure(AuthErrors.Unauthorized);

        // בדיקת מדיניות ביטול ללקוחות
        if (userRole == UserRole.Client)
        {
            var settings = await uow.Settings.GetSettingsAsync();
            if (settings == null)
                return Result.Failure(AppointmentErrors.NotFound);

            var hoursUntilAppointment = (app.StartTime - DateTime.UtcNow).TotalHours;
            if (hoursUntilAppointment < settings.CancellationDeadlineHours)
                return Result.Failure(AppointmentErrors.CannotCancel);
        }

        // ביצוע הביטול (Soft Delete - שינוי סטטוס)
        app.Status = AppointmentStatus.Cancelled;
        await uow.Appointments.UpdateAsync(app);
        await uow.SaveChangesAsync();

        logger.LogInformation("תור בוטל - מזהה תור {AppointmentId} על ידי {UserId} (תפקיד: {Role})",
            appointmentId, userId, userRole);

        await TrySendConfirmationEmailAsync(app, "ביטול תור");

        return Result.Success();
    }

    /// <summary>
    /// מחזיר את היסטוריית התורים של משתמש ספציפי.
    /// </summary>
    public async Task<Result<List<AppointmentResponse>>> GetUserHistoryAsync(Guid userId)
    {
        // שליפת כל התורים של המשתמש (עבר, עתיד, מבוטלים)
        var appointments = await uow.Appointments.GetByClientIdAsync(userId);

        // המרה ל-DTOs
        var response = mapper.Map<List<AppointmentResponse>>(appointments);
        return Result.Success(response);
    }
}
