// =====================================
// קובץ: IUnitOfWork.cs
// שכבה: Domain → Interfaces
// תפקיד: מגדיר "חוזה" ל-Unit of Work Pattern.
//         Unit of Work = גורם מרכזי שמנהל את כל ה-Repositories
//         ומאפשר ביצוע מספר פעולות בבסיס הנתונים כ"עסקה אחת".
//         רעיון: או שכל הפעולות מצליחות ונשמרות יחד, או שאף אחת לא נשמרת (Atomicity).
//         לדוגמה: יצירת משתמש + קביעת תור = שתי פעולות שחייבות להצליח יחד.
//         המימוש האמיתי ב-Infrastructure/Repositories/UnitOfWork.cs.
// =====================================

using System.Data;

namespace AppointmentManager.Domain.Interfaces
{
    /// <summary>
    /// חוזה ל-Unit of Work - מנהל מרכזי לכל הגישה לבסיס הנתונים.
    /// IDisposable = כשגמרים להשתמש, משחררים את המשאבים (חיבורי DB).
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        // --- גישה ל-Repositories ---
        // כל Property מחזיר את ה-Repository המתאים לסוג הנתונים.

        /// <summary>Repository לגישה לנתוני משמרות עבודה</summary>
        IWorkShiftRepository Shifts { get; }

        /// <summary>Repository לגישה לנתוני תורים</summary>
        IAppointmentRepository Appointments { get; }

        /// <summary>Repository לגישה להגדרות המערכת</summary>
        ISettingsRepository Settings { get; }

        /// <summary>Repository לגישה לנתוני משתמשים</summary>
        IUserRepository Users { get; }

        /// <summary>Repository לגישה להצעות החלפת תורים</summary>
        ISwapOfferRepository SwapOffers { get; }

        // --- ניהול עסקאות (Transactions) ---

        /// <summary>
        /// פותח עסקה חדשה בבסיס הנתונים.
        /// IsolationLevel = רמת הבידוד מפעולות מקביליות (ReadCommitted, Serializable וכו').
        /// ReadCommitted = מבטיח שנקרא רק נתונים שכבר נשמרו (לא "נתוני רוח").
        /// </summary>
        Task BeginTransactionAsync(IsolationLevel isolationLevel);

        /// <summary>
        /// מאשר (Commit) את כל הפעולות בעסקה הנוכחית ושומר לבסיס הנתונים לצמיתות.
        /// </summary>
        Task CommitAsync();

        /// <summary>
        /// מבטל (Rollback) את כל הפעולות בעסקה הנוכחית.
        /// משמש בעת שגיאה - מחזיר את בסיס הנתונים למצבו לפני תחילת העסקה.
        /// </summary>
        Task RollbackAsync();

        /// <summary>
        /// שולח את כל השינויים הממתינים לבסיס הנתונים (ללא עסקה מפורשת).
        /// יזרוק ConcurrencyConflictException אם יש התנגשות במקביל.
        /// </summary>
        Task SaveChangesAsync();
    }
}
