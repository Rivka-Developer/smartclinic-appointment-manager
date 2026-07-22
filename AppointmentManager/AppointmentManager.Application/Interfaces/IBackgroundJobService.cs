// =====================================
// קובץ: IBackgroundJobService.cs
// שכבה: Application → Interfaces
// תפקיד: מגדיר "חוזה" לשירות משימות הרקע (Background Jobs).
//         משימות אלה מבוצעות אוטומטית לפי לוח זמנים - ללא קריאה ידנית מהמשתמש.
//         Hangfire (ספריה חיצונית) מריצה את המשימות לפי ה-Cron schedule שהוגדר ב-Program.cs.
//         המימוש האמיתי ב-Services/BackgroundJobService.cs.
// =====================================

namespace AppointmentManager.Application.Interfaces
{
    /// <summary>
    /// חוזה לשירות משימות רקע אוטומטיות.
    /// </summary>
    public interface IBackgroundJobService
    {
        /// <summary>
        /// שולח תזכורות אימייל ללקוחות שיש להם תור מחר.
        /// מופעלת פעם בשעה על ידי Hangfire (Cron.Hourly).
        /// </summary>
        Task SendAppointmentRemindersAsync();

        /// <summary>
        /// שולח דוח יומי למנהל/ת עם מספר התורים המתוכננים למחר.
        /// מופעלת כל יום ב-20:00 על ידי Hangfire ("0 20 * * *").
        /// </summary>
        Task SendDailyAdminReportAsync();
    }
}
