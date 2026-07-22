// =====================================
// קובץ: IWorkShiftRepository.cs
// שכבה: Domain → Interfaces
// תפקיד: מגדיר "חוזה" לגישה לנתוני משמרות עבודה בבסיס הנתונים.
//         המימוש האמיתי ב-Infrastructure/Repositories/WorkShiftRepository.cs.
// =====================================

using AppointmentManager.Domain.Entities;

namespace AppointmentManager.Domain.Interfaces
{
    /// <summary>
    /// חוזה לפעולות גישה לנתוני משמרות עבודה בבסיס הנתונים.
    /// </summary>
    public interface IWorkShiftRepository
    {
        /// <summary>
        /// מחפש משמרת לפי מזהה ייחודי.
        /// מחזיר null אם המשמרת לא קיימת.
        /// </summary>
        Task<WorkShift?> GetByIdAsync(Guid id);

        /// <summary>
        /// מחזיר את כל המשמרות ביום ספציפי, ממוינות לפי שעת התחלה.
        /// ה"Sorted" בשם חשוב - המיון קריטי לאלגוריתמים שמחברים משמרות חופפות.
        /// </summary>
        Task<IEnumerable<WorkShift>> GetSortedShiftsByDateAsync(DateTime date);

        /// <summary>
        /// מחזיר משמרות בטווח תאריכים (מ-start עד end).
        /// משמש כאשר צריך לבדוק זמינות על פני מספר ימים.
        /// </summary>
        Task<IEnumerable<WorkShift>> GetByDateRangeAsync(DateTime start, DateTime end);

        /// <summary>
        /// מוסיף משמרת חדשה לבסיס הנתונים (ללא שמירה מיידית).
        /// </summary>
        Task AddAsync(WorkShift shift);

        /// <summary>
        /// מסמן משמרת קיימת לעדכון (ללא שמירה מיידית).
        /// </summary>
        Task UpdateAsync(WorkShift shift);

        /// <summary>
        /// מוחק משמרת מבסיס הנתונים לחלוטין (Hard Delete - בניגוד לתורים).
        /// </summary>
        Task DeleteAsync(Guid id);
    }
}
