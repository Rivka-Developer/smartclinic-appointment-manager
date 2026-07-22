// =====================================
// קובץ: AppointmentRepository.cs
// שכבה: Infrastructure → Repositories
// תפקיד: מימוש גישה לנתוני תורים בבסיס הנתונים.
//         מממש את IAppointmentRepository מהדומיין.
//         כל הפעולות מתבצעות דרך Entity Framework Core (LINQ → SQL).
//         LINQ = שפת שאילתות ב-C# שמתורגמת ל-SQL אוטומטית.
// =====================================

using AppointmentManager.Domain;
using AppointmentManager.Domain.Entities;
using AppointmentManager.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AppointmentManager.Infrastructure.Repositories
{
    /// <summary>
    /// מימוש Repository לנתוני תורים.
    /// _context = ה-DbContext דרכו מגיעים לבסיס הנתונים.
    /// </summary>
    public class AppointmentRepository : IAppointmentRepository
    {
        private readonly ApplicationDbContext _context; // "readonly" = לא ניתן לשינוי לאחר אתחול

        /// <summary>
        /// קונסטרקטור - מקבל DbContext מה-DI Container.
        /// "=>" = Expression Body - קיצור לקוד שורה אחת.
        /// </summary>
        public AppointmentRepository(ApplicationDbContext context) => _context = context;

        /// <summary>
        /// מחפש תור לפי מזהה.
        /// FindAsync = חיפוש לפי Primary Key (מהיר - משתמש ב-Identity Map של EF).
        /// </summary>
        public async Task<Appointment?> GetByIdAsync(Guid id) => await _context.Appointments.FindAsync(id);

        /// <summary>
        /// מחזיר תורים פעילים (לא מבוטלים) בטווח תאריכים.
        /// Where = סינון (כמו WHERE ב-SQL).
        /// a.Status != AppointmentStatus.Cancelled = רק תורים שלא בוטלו.
        /// ToListAsync = ביצוע השאילתה ומחזיר List.
        /// </summary>
        public async Task<IEnumerable<Appointment>> GetByDateRangeAsync(DateTime start, DateTime end)
        {
            return await _context.Appointments
                .Where(a => a.StartTime >= start && a.StartTime <= end && a.Status != AppointmentStatus.Cancelled)
                .ToListAsync();
        }

        /// <summary>
        /// מחזיר תורים פעילים בטווח תאריכים כולל פרטי הלקוח.
        /// Include = JOIN עם טבלת Users (טעינת נתוני הלקוח בשאילתה אחת).
        /// משמש ביומן המנהל/ת שצריך להציג שמות.
        /// </summary>
        public async Task<IEnumerable<Appointment>> GetWithClientsByDateRangeAsync(DateTime start, DateTime end)
        {
            var endOfDay = end.Date.AddDays(1);
            return await _context.Appointments
                .Where(a => a.StartTime >= start && a.StartTime < endOfDay && a.Status != AppointmentStatus.Cancelled)
                .Include(a => a.Client) // JOIN עם Users
                .ToListAsync();
        }

        /// <summary>
        /// מחזיר תורים פעילים (Scheduled) ביום ספציפי, ממוינים לפי שעת התחלה.
        /// a.StartTime.Date == date.Date = השוואת תאריכים ללא שעה.
        /// OrderBy = מיון עולה לפי StartTime - חיוני לאלגוריתם חישוב בלוקים.
        /// </summary>
        public async Task<IEnumerable<Appointment>> GetActiveAppointmentsByDateAsync(DateTime date)
        {
            return await _context.Appointments
                .Where(a => a.StartTime.Date == date.Date && a.Status == AppointmentStatus.Scheduled)
                .Include(a => a.Client)
                .OrderBy(a => a.StartTime)
                .ToListAsync();
        }

        /// <summary>
        /// מחזיר תורים פעילים (Scheduled) שמתחילים בטווח זמן מדויק.
        /// משמש לשליחת תזכורות: רק תורים בין 20 ל-28 שעות קדימה.
        /// </summary>
        public async Task<IEnumerable<Appointment>> GetActiveAppointmentsByTimeRangeAsync(DateTime from, DateTime to)
        {
            return await _context.Appointments
                .Where(a => a.StartTime >= from && a.StartTime < to && a.Status == AppointmentStatus.Scheduled)
                .Include(a => a.Client)
                .OrderBy(a => a.StartTime)
                .ToListAsync();
        }

        /// <summary>
        /// מחזיר את כל התורים של לקוח ספציפי לפי מזהה, ממוינים מהחדש לישן.
        /// OrderByDescending = מיון יורד (הכי חדש ראשון).
        /// </summary>
        public async Task<IEnumerable<Appointment>> GetByClientIdAsync(Guid clientId) =>
           await _context.Appointments
               .Where(a => a.ClientId == clientId)
               .OrderByDescending(a => a.StartTime) // מהחדש לישן
               .ToListAsync();

        /// <summary>
        /// מוסיף תור חדש לתור השינויים (ללא שמירה מיידית).
        /// AddAsync = מסמן לאובייקט כ-"Added" ב-EF Change Tracker.
        /// השמירה תתבצע ב-SaveChangesAsync של UnitOfWork.
        /// </summary>
        public async Task AddAsync(Appointment appointment)
        {
            await _context.Appointments.AddAsync(appointment);
        }

        /// <summary>
        /// מסמן תור קיים לעדכון (ללא שמירה מיידית).
        /// Update = מסמן כ-"Modified" ב-EF Change Tracker.
        /// Task.CompletedTask = מחזיר Task שכבר הסתיים (כי אין פעולה async כאן).
        /// </summary>
        public Task UpdateAsync(Appointment appointment)
        {
            _context.Appointments.Update(appointment); // סימון לעדכון
            return Task.CompletedTask;                 // אין פעולה async - מחזירים Task ריק
        }

        /// <summary>
        /// "מוחק" תור - בפועל מסמן כ-Cancelled (Soft Delete).
        /// הנתון נשמר בבסיס הנתונים לצורכי היסטוריה.
        /// </summary>
        public async Task DeleteAsync(Guid id)
        {
            var app = await GetByIdAsync(id); // מחפש את התור
            if (app != null)
            {
                app.Status = AppointmentStatus.Cancelled; // שינוי סטטוס לביטול
                _context.Appointments.Update(app);        // סימון לעדכון
            }
        }
    }
}
