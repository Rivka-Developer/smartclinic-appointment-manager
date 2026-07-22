// =====================================
// קובץ: BackgroundJobService.cs
// שכבה: Application → Services
// תפקיד: מימוש שירות משימות רקע אוטומטיות.
//         משימות אלה מופעלות על ידי Hangfire לפי לוח הזמנים שהוגדר ב-Program.cs.
//         IServiceScopeFactory נדרש כי Hangfire מריץ Jobs מחוץ ל-HTTP Request,
//         ולכן לא ניתן להשתמש ב-Scoped Services ישירות - צריך ליצור scope חדש.
// =====================================

using AppointmentManager.Application.Helpers;
using AppointmentManager.Application.Interfaces;
using AppointmentManager.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AppointmentManager.Domain;
using System.Text;

namespace AppointmentManager.Application.Services;

/// <summary>
/// מימוש שירות משימות הרקע.
/// IServiceScopeFactory = מפעל ל-DI Scopes חדשים (כי Hangfire רץ מחוץ לבקשת HTTP).
/// </summary>
public class BackgroundJobService(IServiceScopeFactory scopeFactory, ILogger<BackgroundJobService> logger) : IBackgroundJobService
{
    /// <summary>
    /// שולח תזכורות אימייל ללקוחות עם תורים מחר.
    /// מופעלת פעם בשעה על ידי Hangfire.
    /// </summary>
    public async Task SendAppointmentRemindersAsync()
    {
        // יצירת Scope חדש - נדרש כי Hangfire חי מחוץ לבקשת HTTP
        using var scope = scopeFactory.CreateScope();

        // שליפת שירותים מה-Scope החדש
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        // חלון תזכורת: תורים שמתחילים בין 20 ל-28 שעות מעכשיו (~24 שעות לפני התור)
        var now = DateTime.UtcNow;
        var windowStart = now.AddHours(20);
        var windowEnd = now.AddHours(28);

        var appointments = await uow.Appointments.GetActiveAppointmentsByTimeRangeAsync(windowStart, windowEnd);

        // מעבר על כל תור ושליחת תזכורת
        foreach (var app in appointments)
        {
            // שליחה רק ללקוחות עם אימייל תקין (לא ManagedClient)
            if (app.Client == null || string.IsNullOrEmpty(app.Client.Email) ||
                app.Client.Role == UserRole.ManagedClient)
                continue;

            // אידמפוטנטיות: אם כבר נשלחה תזכורת - דלג (מונע כפילות ב-retry של Hangfire)
            if (app.ReminderSentAt.HasValue) continue;

            try
            {
                var settings = await uow.Settings.GetSettingsAsync();
                string businessName = settings?.BusinessName ?? "SmartClinic";
                string subject = "תזכורת לתור ב-SmartClinic";
                string body = EmailTemplates.AppointmentReminder(app.Client.FullName, app.StartTime, businessName);

                await emailService.SendEmailAsync(app.Client.Email, subject, body);

                app.SetReminderSent(DateTime.UtcNow);
                await uow.Appointments.UpdateAsync(app);
                await uow.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "שגיאה בשליחת תזכורת לתור {AppointmentId} ללקוח {Email}",
                    app.Id, app.Client.Email);
                // ממשיך ללקוח הבא - כשל אחד לא קורס את כל ה-Job
            }
        }
    }

    /// <summary>
    /// שולח דוח יומי למנהל/ת עם מספר התורים למחר.
    /// מופעלת כל יום ב-20:00 על ידי Hangfire.
    /// </summary>
    public async Task SendDailyAdminReportAsync()
    {
        // יצירת Scope חדש
        using var scope = scopeFactory.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        // שליפת הגדרות מערכת - להשגת האימייל של המנהל/ת
        var settings = await uow.Settings.GetSettingsAsync();
        if (settings == null || string.IsNullOrWhiteSpace(settings.AdminContactEmail)) return;

        var tomorrow = DateTime.UtcNow.AddDays(1).Date;

        // שליפת כל התורים למחר (כולל פרטי לקוחות לספירה)
        var apps = await uow.Appointments.GetWithClientsByDateRangeAsync(tomorrow, tomorrow);
        var sortedApps = apps.OrderBy(a => a.StartTime).ToList();

        if (sortedApps.Count == 0) return;

        string subject = $"דו\"ח תורים מפורט למחר ({tomorrow:dd/MM/yyyy})";

        var reportRows = sortedApps
            .Select(a => new EmailTemplates.AppointmentReportRow(
                a.StartTime,
                a.EndTime,
                a.Client?.FullName ?? "לקוח לא ידוע"))
            .ToList();

        string body = EmailTemplates.AdminDailyReport(tomorrow, reportRows, settings.BusinessName ?? "SmartClinic");

        await emailService.SendEmailAsync(settings.AdminContactEmail, subject, body);
    }
}
//http://localhost:5225/hangfire-כאן אפשר לראות את לוח הבקרה של Hangfire עם כל המשימות המתוזמנות והסטטוס שלהן.
