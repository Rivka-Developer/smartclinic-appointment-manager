// =====================================
// קובץ: ISettingsService.cs
// שכבה: Application → Interfaces
// תפקיד: מגדיר "חוזה" לשירות הגדרות מערכת.
//         המנהל/ת משתמש/ת בשירות זה לקריאה ועדכון הגדרות הקליניקה.
//         המימוש האמיתי ב-Services/SettingsService.cs.
// =====================================

using AppointmentManager.Application.DTOs;
using AppointmentManager.Domain.Common;
using System.Threading.Tasks;

namespace AppointmentManager.Application.Interfaces
{
    /// <summary>
    /// חוזה לשירות הגדרות מערכת.
    /// </summary>
    public interface ISettingsService
    {
        /// <summary>
        /// מחזיר את ההגדרות הנוכחיות של המערכת.
        /// </summary>
        /// <returns>SystemSettingsDto עם כל ההגדרות הנוכחיות</returns>
        Task<Result<SystemSettingsDto>> GetSettingsAsync();

        /// <summary>
        /// מעדכן את הגדרות המערכת.
        /// מחליף את ההגדרות הקיימות בהגדרות החדשות שנשלחו.
        /// </summary>
        /// <param name="settingsDto">ההגדרות החדשות</param>
        Task<Result> UpdateSettingsAsync(SystemSettingsDto settingsDto);
    }
}
