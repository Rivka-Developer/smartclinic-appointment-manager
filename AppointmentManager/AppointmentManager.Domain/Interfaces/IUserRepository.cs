// =====================================
// קובץ: IUserRepository.cs
// שכבה: Domain → Interfaces
// תפקיד: מגדיר "חוזה" (Interface) לגישה לנתוני משתמשים בבסיס הנתונים.
//         Interface = רשימת מתודות שכל מי שמממש אותה חייב לספק.
//         הפרדה זו (Domain לא יודע על SQL) מאפשרת:
//         1. החלפת בסיס נתונים בעתיד ללא שינוי לוגיקה עסקית.
//         2. שימוש ב-Mocks בבדיקות יחידה (ראה AuthServiceTests).
// =====================================

using AppointmentManager.Domain.Entities;

namespace AppointmentManager.Domain.Interfaces
{
    /// <summary>
    /// חוזה לפעולות גישה לנתוני משתמשים בבסיס הנתונים.
    /// המימוש האמיתי נמצא ב-Infrastructure/Repositories/UserRepository.cs.
    /// כל המתודות הן async (אסינכרוניות) כדי לא לחסום את השרת בזמן המתנה לבסיס הנתונים.
    /// Task<T> = הבטחה (Promise) שבסוף תחזיר ערך מסוג T.
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>
        /// מחפש משתמש לפי מזהה ייחודי (GUID).
        /// מחזיר User? - ה-"?" אומר שהתוצאה יכולה להיות null (אם המשתמש לא קיים).
        /// </summary>
        Task<User?> GetByIdAsync(Guid id);

        /// <summary>
        /// מחפש משתמש לפי כתובת אימייל.
        /// משמש בתהליך התחברות (Login) ובבדיקת כפילות בהרשמה (Register).
        /// </summary>
        Task<User?> GetByEmailAsync(string email);

        /// <summary>
        /// מחפש לקוח לפי מספר טלפון.
        /// משמש כשמנהל/ת קובע/ת תור ידנית - בודק אם הלקוח כבר קיים.
        /// מסנן רק Client ו-ManagedClient (לא Admin).
        /// </summary>
        Task<User?> FindByPhoneAsync(string phone);

        /// <summary>
        /// מוסיף משתמש חדש לבסיס הנתונים.
        /// לא שומר מיידית - השמירה מתבצעת ב-UnitOfWork.SaveChangesAsync().
        /// </summary>
        Task AddAsync(User user);

        /// <summary>
        /// מחזיר את רשימת כל הלקוחות (Client + ManagedClient).
        /// לא כולל מנהלים (Admin).
        /// </summary>
        Task<IEnumerable<User>> GetAllClientsAsync();

        /// <summary>
        /// מחזיר רשימת לקוחות עם דפדוף (Pagination) ומספר כולל.
        /// (IEnumerable, TotalCount) = Tuple - מחזיר שני ערכים יחד.
        /// pageNumber = מספר העמוד הנוכחי.
        /// pageSize = כמה פריטים בכל עמוד.
        /// </summary>
        Task<(IEnumerable<User> Items, int TotalCount)> GetAllClientsWithAppointmentsAsync(int pageNumber, int pageSize);

        /// <summary>
        /// מחזיר לקוח יחד עם כל היסטוריית התורים שלו.
        /// "Include" בבסיס הנתונים = JOIN בין טבלת Users לטבלת Appointments.
        /// </summary>
        Task<User?> GetClientWithHistoryAsync(Guid id);

        /// <summary>
        /// מוחק לקוח ואת כל התורים שלו מבסיס הנתונים.
        /// מחזיר false אם הלקוח לא נמצא.
        /// </summary>
        Task<bool> DeleteAsync(Guid id);
    }
}
