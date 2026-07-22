// =====================================
// קובץ: UnitOfWork.cs
// שכבה: Infrastructure → Repositories
// תפקיד: מימוש Unit of Work Pattern.
//         מספק נקודת גישה אחת לכל ה-Repositories ומנהל עסקאות (Transactions).
//         "עסקה" = קבוצת פעולות שחייבות להצליח כולן יחד, או לא להצליח כלל.
//         לדוגמה: יצירת לקוח + קביעת תור = שמירה אחת אטומית.
// =====================================

using System.Data;
using AppointmentManager.Domain.Common;
using AppointmentManager.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AppointmentManager.Infrastructure.Repositories
{
    /// <summary>
    /// מימוש Unit of Work - מנהל מרכזי לכל הגישה לבסיס הנתונים.
    /// IDisposable = חובה לממש Dispose() לשחרור משאבים.
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;          // ה-DbContext המשותף
        private IDbContextTransaction? _currentTransaction;     // העסקה הנוכחית (אם קיימת)

        // --- ה-Repositories - נגישים לכל ה-Services ---

        /// <summary>Repository לניהול משמרות</summary>
        public IWorkShiftRepository Shifts { get; }

        /// <summary>Repository לניהול תורים</summary>
        public IAppointmentRepository Appointments { get; }

        /// <summary>Repository לניהול הגדרות</summary>
        public ISettingsRepository Settings { get; }

        /// <summary>Repository לניהול משתמשים</summary>
        public IUserRepository Users { get; }

        /// <summary>Repository לניהול הצעות החלפת תורים</summary>
        public ISwapOfferRepository SwapOffers { get; }

        /// <summary>
        /// קונסטרקטור - יוצר את כל ה-Repositories עם אותו DbContext.
        /// שיתוף DbContext אחד = שיתוף אותו חיבור לבסיס הנתונים → עסקה אחת מקיפה הכל.
        /// </summary>
        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
            Shifts = new WorkShiftRepository(context);             // יוצר Repository למשמרות
            Appointments = new AppointmentRepository(context);   // יוצר Repository לתורים
            Settings = new SettingsRepository(context);          // יוצר Repository להגדרות
            Users = new UserRepository(context);                 // יוצר Repository למשתמשים
            SwapOffers = new SwapOfferRepository(context);       // יוצר Repository להצעות החלפה
        }

        /// <summary>
        /// פותח עסקה חדשה.
        /// IsolationLevel = רמת הבידוד מפעולות מקביליות.
        /// ReadCommitted = קרא רק נתונים שכבר אושרו (commit) - מונע "קריאת רוח".
        /// </summary>
        public async Task BeginTransactionAsync(IsolationLevel isolationLevel)
        {
            _currentTransaction = await _context.Database.BeginTransactionAsync(isolationLevel);
        }

        
        /// <summary>
        /// שולח את כל השינויים הממתינים לבסיס הנתונים.
        /// תופס DbUpdateConcurrencyException וממיר ל-ConcurrencyConflictException שלנו.
        /// DbUpdateConcurrencyException = EF זרק כי RowVersion השתנה מאז הקריאה.
        /// </summary>
        public async Task SaveChangesAsync()
        {
            try
            {
                await _context.SaveChangesAsync(); // שמירה לבסיס הנתונים
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException ex)
            {
                // המרת שגיאת EF לשגיאת הדומיין שלנו (ידועה לכל השכבות)
                throw new ConcurrencyConflictException("A concurrency conflict occurred while saving changes.", ex);
            }
        }

        /// <summary>
        /// שחרור כל המשאבים - חיבורי בסיס הנתונים, עסקאות פתוחות.
        /// נקרא אוטומטית כשה-Service מסיים (IDisposable + DI Lifetime = Scoped).
        /// "?" = null-safe: רק אם _currentTransaction אינו null.
        /// </summary>
        public void Dispose()
        {
            _currentTransaction?.Dispose(); // שחרור עסקה אם קיימת
            _context.Dispose();             // שחרור חיבור לבסיס הנתונים
        }

        /// <summary>
        /// מאשר ושומר את כל פעולות העסקה הנוכחית.
        /// DisposeAsync = שחרור משאבי העסקה.
        /// _currentTransaction = null = איפוס (אין עסקה פתוחה).
        /// </summary>
        public async Task CommitAsync()
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.CommitAsync(); // אישור סופי
                await _currentTransaction.DisposeAsync(); // שחרור משאבים
                _currentTransaction = null;              // איפוס
            }
        }

        /// <summary>
        /// מבטל את כל פעולות העסקה הנוכחית.
        /// מחזיר את בסיס הנתונים למצב לפני תחילת העסקה.
        /// נקרא ב-catch block כאשר יש שגיאה.
        /// </summary>
        public async Task RollbackAsync()
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.RollbackAsync(); // ביטול כל הפעולות
                await _currentTransaction.DisposeAsync();  // שחרור משאבים
                _currentTransaction = null;               // איפוס
            }
        }

    }
}
