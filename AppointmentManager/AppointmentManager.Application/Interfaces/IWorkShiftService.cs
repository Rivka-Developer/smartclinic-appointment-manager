// =====================================
// קובץ: IWorkShiftService.cs
// שכבה: Application → Interfaces
// תפקיד: מגדיר "חוזה" לשירות ניהול משמרות עבודה.
//         המנהל/ת משתמש/ת בשירות זה לניהול שעות הפתיחה של הקליניקה.
//         המימוש האמיתי ב-Services/WorkShiftService.cs.
// =====================================

using AppointmentManager.Application.DTOs;
using AppointmentManager.Domain.Common;

namespace AppointmentManager.Application.Interfaces
{
    /// <summary>
    /// חוזה לשירות ניהול משמרות עבודה.
    /// </summary>
    public interface IWorkShiftService
    {
        /// <summary>
        /// מחזיר את כל המשמרות ביום ספציפי, ממוינות לפי שעת התחלה.
        /// IEnumerable = ממשק לרשימה שניתן לעבור עליה.
        /// </summary>
        Task<Result<IEnumerable<WorkShiftResponse>>> GetWorkShiftsByDateAsync(DateTime date);

        /// <summary>
        /// מוסיף משמרת חדשה.
        /// מבצע אימות (שעת סיום אחרי שעת התחלה) ובדיקת חפיפה עם משמרות קיימות.
        /// </summary>
        Task<Result> AddWorkShiftAsync(WorkShiftRequest request);

        /// <summary>
        /// מעדכן משמרת קיימת לפי מזהה.
        /// מבצע אימות זמנים ובדיקת חפיפה (תוך התעלמות מהמשמרת הנוכחית עצמה).
        /// </summary>
        /// <param name="id">מזהה המשמרת לעדכון</param>
        /// <param name="request">הנתונים החדשים</param>
        Task<Result> UpdateWorkShiftAsync(Guid id, WorkShiftRequest request);

        /// <summary>
        /// מוחק משמרת לפי מזהה.
        /// מחיקה קשיחה (Hard Delete) - המשמרת נמחקת לחלוטין מבסיס הנתונים.
        /// </summary>
        Task<Result> DeleteWorkShiftAsync(Guid id);
    }
}
