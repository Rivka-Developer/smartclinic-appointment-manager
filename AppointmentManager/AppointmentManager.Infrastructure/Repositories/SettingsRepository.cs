// =====================================
// קובץ: SettingsRepository.cs
// שכבה: Infrastructure → Repositories
// תפקיד: מימוש גישה להגדרות המערכת בבסיס הנתונים.
//         מיוחד: רק רשומה אחת קיימת בטבלה (Singleton).
//         אם אין רשומה - יוצר ברירת מחדל אוטומטית.
//         מממש את ISettingsRepository מהדומיין.
// =====================================

using AppointmentManager.Domain.Entities;
using AppointmentManager.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AppointmentManager.Infrastructure.Repositories
{
    /// <summary>
    /// מימוש Repository להגדרות מערכת.
    /// </summary>
    public class SettingsRepository : ISettingsRepository
    {
        private readonly ApplicationDbContext _context; // ה-DbContext

        /// <summary>קונסטרקטור</summary>
        public SettingsRepository(ApplicationDbContext context) => _context = context;

        /// <summary>
        /// מחזיר את הגדרות המערכת.
        /// FirstOrDefaultAsync = מחזיר ראשון שמתאים, או null אם אין כלל.
        /// אם אין הגדרות - יוצר אוטומטית עם ברירות מחדל.
        /// זהו "Lazy Initialization" - יוצר רק כשצריך.
        /// </summary>
        public async Task<SystemSettings> GetSettingsAsync()
        {
            // שליפת הרשומה הראשונה (היחידה) מהטבלה
            var settings = await _context.Settings.FirstOrDefaultAsync();

            // אם אין הגדרות (לדוגמה: בסיס נתונים ריק) - צור ברירת מחדל
            if (settings == null)
            {
                settings = new SystemSettings
                {
                    BufferTime = 5,   // 5 דקות בין תורים
                    MinGapSize = 15   // חור מינימלי 15 דקות
                };
                await _context.Settings.AddAsync(settings); // הוסף לתור השינויים
                await _context.SaveChangesAsync();           // שמור מיידית (לא דרך UoW)
            }

            return settings;
        }

        /// <summary>
        /// מסמן הגדרות קיימות לעדכון (ללא שמירה מיידית).
        /// השמירה תתבצע ב-UnitOfWork.SaveChangesAsync().
        /// </summary>
        public Task UpdateAsync(SystemSettings settings)
        {
            _context.Settings.Update(settings); // סימון לעדכון
            return Task.CompletedTask;           // אין פעולה async
        }
    }
}
