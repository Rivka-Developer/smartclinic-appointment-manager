// =====================================
// קובץ: UserRepository.cs
// שכבה: Infrastructure → Repositories
// תפקיד: מימוש גישה לנתוני משתמשים בבסיס הנתונים.
//         מממש את IUserRepository מהדומיין.
// =====================================

using AppointmentManager.Domain;
using AppointmentManager.Domain.Entities;
using AppointmentManager.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AppointmentManager.Infrastructure.Repositories
{
    /// <summary>
    /// מימוש Repository לנתוני משתמשים.
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context; // ה-DbContext לגישה לבסיס הנתונים

        /// <summary>קונסטרקטור - מקבל DbContext מה-DI Container</summary>
        public UserRepository(ApplicationDbContext context) => _context = context;

        /// <summary>
        /// מחפש משתמש לפי מזהה.
        /// FindAsync = חיפוש לפי Primary Key - הכי מהיר.
        /// </summary>
        public async Task<User?> GetByIdAsync(Guid id) => await _context.Users.FindAsync(id);

        /// <summary>
        /// מחפש משתמש לפי כתובת אימייל.
        /// FirstOrDefaultAsync = מחזיר ראשון שמתאים, או null אם לא נמצא.
        /// u.Email == email = השוואת מחרוזות (case-sensitive ב-SQL Server).
        /// </summary>
        public async Task<User?> GetByEmailAsync(string email) =>
            await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        /// <summary>
        /// מחפש לקוח לפי מספר טלפון (Client או ManagedClient בלבד).
        /// משמש כשמנהל/ת קובע/ת תור ידנית - בודק אם הלקוח כבר קיים במערכת.
        /// </summary>
        public async Task<User?> FindByPhoneAsync(string phone) =>
            await _context.Users.FirstOrDefaultAsync(u =>
                u.PhoneNumber == phone &&
                (u.Role == UserRole.Client || u.Role == UserRole.ManagedClient)); // רק לקוחות, לא מנהלים

        /// <summary>
        /// מוסיף משתמש חדש לתור השינויים (ללא שמירה מיידית).
        /// </summary>
        public async Task AddAsync(User user)
        {
            await _context.Users.AddAsync(user);
        }

        /// <summary>
        /// מחזיר את כל הלקוחות (Client + ManagedClient) ללא דפדוף.
        /// Where = מסנן לפי תפקיד.
        /// ToListAsync = מוציא מבסיס הנתונים לזיכרון.
        /// </summary>
        public async Task<IEnumerable<User>> GetAllClientsAsync() =>
            await _context.Users
                .Where(u => u.Role == UserRole.Client || u.Role == UserRole.ManagedClient)
                .ToListAsync();

        /// <summary>
        /// מחזיר לקוחות עם דפדוף (Pagination) ומספר כולל.
        /// Include = טוענים גם את התורים של כל לקוח בשאילתה אחת.
        /// Skip/Take = דפדוף: Skip דולג על עמודים קודמים, Take מגביל לעמוד הנוכחי.
        /// לדוגמה: עמוד 3, 20 פריטים → Skip(40).Take(20).
        /// </summary>
        public async Task<(IEnumerable<User> Items, int TotalCount)> GetAllClientsWithAppointmentsAsync(int pageNumber, int pageSize)
        {
            var query = _context.Users
                .Where(u => u.Role == UserRole.Client || u.Role == UserRole.ManagedClient);

            var total = await query.CountAsync();

            // AsSplitQuery: EF Core שולח שתי שאילתות SQL נפרדות (users + appointments)
            // במקום JOIN אחד שיוצר Cartesian Product (מכפיל שורות).
            var items = await query
                .Include(u => u.Appointments)
                .AsSplitQuery()
                .OrderBy(u => u.FullName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total); // החזרת Tuple עם הפריטים והמספר הכולל
        }

        /// <summary>
        /// מחזיר לקוח ספציפי עם כל היסטוריית התורים שלו.
        /// Include = JOIN - טעינת טבלת Appointments יחד עם User.
        /// </summary>
        public async Task<User?> GetClientWithHistoryAsync(Guid id)
        {
            return await _context.Users
                .Include(u => u.Appointments) // טעינת כל התורים
                .FirstOrDefaultAsync(u => u.Id == id &&
                    (u.Role == UserRole.Client || u.Role == UserRole.ManagedClient)); // רק לקוחות
        }

        /// <summary>
        /// מוחק לקוח ואת כל התורים שלו.
        /// FK מוגדר עם Restrict — חייבים למחוק את התורים לפני המשתמש.
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id)
        {
            var user = await _context.Users
                .Include(u => u.Appointments)
                .FirstOrDefaultAsync(u => u.Id == id &&
                    (u.Role == UserRole.Client || u.Role == UserRole.ManagedClient));

            if (user == null) return false;

            _context.Appointments.RemoveRange(user.Appointments);
            _context.Users.Remove(user);
            return true;
        }
    }
}
