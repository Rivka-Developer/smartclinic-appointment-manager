// =====================================
// קובץ: ISettingsRepository.cs
// שכבה: Domain → Interfaces
// תפקיד: מגדיר "חוזה" לגישה להגדרות המערכת בבסיס הנתונים.
//         שונה מ-Repositories אחרים: רק רשומה אחת קיימת (Singleton pattern).
//         אין מתודת "GetById" - תמיד מוחזרת ההגדרה היחידה.
//         המימוש האמיתי ב-Infrastructure/Repositories/SettingsRepository.cs.
// =====================================

using AppointmentManager.Domain.Entities;

namespace AppointmentManager.Domain.Interfaces
{
    /// <summary>
    /// חוזה לגישה להגדרות המערכת.
    /// קיימת רשומת הגדרות אחת בלבד בבסיס הנתונים.
    /// </summary>
    public interface ISettingsRepository
    {
        /// <summary>
        /// מחזיר את הגדרות המערכת.
        /// אם אין הגדרות - יוצר ברירת מחדל אוטומטית (ראה SettingsRepository.cs).
        /// לכן לא מחזיר SystemSettings? (nullable) - תמיד מחזיר ערך.
        /// </summary>
        Task<SystemSettings> GetSettingsAsync();

        /// <summary>
        /// מעדכן את הגדרות המערכת (ללא שמירה מיידית).
        /// השמירה מתבצעת ב-UnitOfWork.SaveChangesAsync().
        /// </summary>
        Task UpdateAsync(SystemSettings settings);
    }
}
