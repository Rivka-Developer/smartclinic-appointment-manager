// =====================================
// קובץ: IAppointmentRepository.cs
// שכבה: Domain → Interfaces
// תפקיד: מגדיר "חוזה" לגישה לנתוני תורים בבסיס הנתונים.
//         המימוש האמיתי ב-Infrastructure/Repositories/AppointmentRepository.cs.
// =====================================

using AppointmentManager.Domain.Entities;

namespace AppointmentManager.Domain.Interfaces
{
    /// <summary>
    /// חוזה לפעולות גישה לנתוני תורים בבסיס הנתונים.
    /// </summary>
    public interface IAppointmentRepository
    {
        /// <summary>
        /// מחפש תור לפי מזהה ייחודי.
        /// מחזיר null אם התור לא קיים.
        /// </summary>
        Task<Appointment?> GetByIdAsync(Guid id);

        /// <summary>
        /// מחזיר תורים פעילים בטווח תאריכים מוגדר (ללא נתוני הלקוח).
        /// start = תאריך התחלה, end = תאריך סיום.
        /// משמש בחישובים פנימיים שלא צריכים שמות לקוחות.
        /// </summary>
        Task<IEnumerable<Appointment>> GetByDateRangeAsync(DateTime start, DateTime end);

        /// <summary>
        /// מחזיר תורים פעילים בטווח תאריכים כולל פרטי הלקוח המלאים.
        /// "WithClients" = מבצע JOIN עם טבלת Users לטעינת שם ומספר טלפון.
        /// משמש ביומן המנהל/ת שצריך להציג שמות.
        /// </summary>
        Task<IEnumerable<Appointment>> GetWithClientsByDateRangeAsync(DateTime start, DateTime end);

        /// <summary>
        /// מחזיר את כל התורים של לקוח ספציפי לפי מזההו.
        /// משמש להצגת היסטוריית תורים אישית.
        /// ממוין לפי תאריך בסדר יורד (מהחדש לישן).
        /// </summary>
        Task<IEnumerable<Appointment>> GetByClientIdAsync(Guid clientId);

        /// <summary>
        /// מוסיף תור חדש לבסיס הנתונים (ללא שמירה מיידית).
        /// השמירה מתבצעת ב-UnitOfWork.SaveChangesAsync().
        /// </summary>
        Task AddAsync(Appointment appointment);

        /// <summary>
        /// מסמן תור קיים לעדכון (ללא שמירה מיידית).
        /// Entity Framework עוקב אחרי השינויים ושומר ב-SaveChangesAsync.
        /// </summary>
        Task UpdateAsync(Appointment appointment);

        /// <summary>
        /// "מוחק" תור - בפועל מסמן אותו כ-Cancelled (Soft Delete).
        /// הנתונים נשמרים לצורכי היסטוריה, רק הסטטוס משתנה.
        /// </summary>
        Task DeleteAsync(Guid id);

        /// <summary>
        /// מחזיר תורים פעילים (Scheduled) ביום ספציפי, ממוינים לפי שעת התחלה.
        /// הסדר הממוין קריטי לאלגוריתם חישוב הבלוקים הפנויים ב-AvailabilityService.
        /// </summary>
        Task<IEnumerable<Appointment>> GetActiveAppointmentsByDateAsync(DateTime date);

        /// <summary>
        /// מחזיר תורים פעילים (Scheduled) בטווח זמן מדויק, כולל פרטי לקוח.
        /// משמש לשליחת תזכורות במרחק ~24 שעות לפני התור.
        /// </summary>
        Task<IEnumerable<Appointment>> GetActiveAppointmentsByTimeRangeAsync(DateTime from, DateTime to);
    }
}
