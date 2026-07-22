// =====================================
// קובץ: WorkShiftRepository.cs
// שכבה: Infrastructure → Repositories
// תפקיד: מימוש גישה לנתוני משמרות עבודה בבסיס הנתונים.
//         מממש את IWorkShiftRepository מהדומיין.
// =====================================

using AppointmentManager.Domain.Entities;
using AppointmentManager.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AppointmentManager.Infrastructure.Repositories
{
    /// <summary>
    /// מימוש Repository לנתוני משמרות עבודה.
    /// </summary>
    public class WorkShiftRepository : IWorkShiftRepository
    {
        private readonly ApplicationDbContext _context; // ה-DbContext לגישה לבסיס הנתונים

        /// <summary>קונסטרקטור</summary>
        public WorkShiftRepository(ApplicationDbContext context) => _context = context;

        /// <summary>
        /// מחפש משמרת לפי מזהה.
        /// FindAsync = חיפוש מהיר לפי Primary Key.
        /// </summary>
        public async Task<WorkShift?> GetByIdAsync(Guid id) =>
            await _context.WorkShifts.FindAsync(id);

        /// <summary>
        /// מחזיר משמרות ביום ספציפי, ממוינות לפי שעת התחלה.
        /// ws.Date.Date == date.Date = השוואת תאריך בלבד (ללא שעה).
        /// OrderBy = מיון עולה - חיוני לאלגוריתם איחוד משמרות ב-AvailabilityService.
        /// </summary>
        public async Task<IEnumerable<WorkShift>> GetSortedShiftsByDateAsync(DateTime date) =>
            await _context.WorkShifts
                .Where(ws => ws.Date.Date == date.Date) // סינון לפי יום ספציפי
                .OrderBy(ws => ws.StartTime)            // מיון עולה לפי שעת התחלה
                .ToListAsync();

        /// <summary>
        /// מחזיר משמרות בטווח תאריכים.
        /// ws.Date >= start && ws.Date <= end = BETWEEN ב-SQL.
        /// </summary>
        public async Task<IEnumerable<WorkShift>> GetByDateRangeAsync(DateTime start, DateTime end) =>
            await _context.WorkShifts
                .Where(ws => ws.Date >= start && ws.Date <= end)
                .ToListAsync();

        /// <summary>
        /// מוסיף משמרת חדשה לתור השינויים (ללא שמירה מיידית).
        /// </summary>
        public async Task AddAsync(WorkShift shift)
        {
            await _context.WorkShifts.AddAsync(shift);
        }

        /// <summary>
        /// מסמן משמרת לעדכון (ללא שמירה מיידית).
        /// Task.CompletedTask = אין פעולה async - מחזירים Task שכבר הסתיים.
        /// </summary>
        public Task UpdateAsync(WorkShift shift)
        {
            _context.WorkShifts.Update(shift); // סימון לעדכון
            return Task.CompletedTask;
        }

        /// <summary>
        /// מוחק משמרת לחלוטין מבסיס הנתונים (Hard Delete).
        /// שונה מתורים שנמחקים Soft - משמרות נמחקות לגמרי.
        /// </summary>
        public async Task DeleteAsync(Guid id)
        {
            var shift = await _context.WorkShifts.FindAsync(id); // מציאת המשמרת
            if (shift != null)
            {
                _context.WorkShifts.Remove(shift); // מחיקה אמיתית מבסיס הנתונים
            }
        }
    }
}
