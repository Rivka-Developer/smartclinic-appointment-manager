// =====================================
// קובץ: SettingsController.cs
// שכבה: API → Controllers
// תפקיד: Controller לניהול הגדרות מערכת.
//         קריאה: כל משתמש מחובר (לדוגמה: כדי לדעת מה שעת הערב).
//         עדכון: Admin בלבד.
// =====================================

using System.Threading.Tasks;
using AppointmentManager.Api.Extensions;
using AppointmentManager.Application.DTOs;
using AppointmentManager.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentManager.Api.Controllers
{
    /// <summary>
    /// Controller לניהול הגדרות מערכת.
    /// נתיב בסיס: api/settings
    /// </summary>
    [Authorize] // שתי הפעולות דורשות Token
    [ApiController]
    [Route("api/[controller]")] // "settings"
    public class SettingsController(ISettingsService settingsService) : ControllerBase
    {
        /// <summary>
        /// קריאת ההגדרות הנוכחיות של המערכת.
        /// נתיב: GET api/settings
        /// דורש: כל משתמש מחובר.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var result = await settingsService.GetSettingsAsync();

            return result.IsSuccess
                ? Ok(result.Value)         // 200 OK עם SystemSettingsDto
                : result.ToActionResult(); // שגיאה מתאימה
        }

        /// <summary>
        /// עדכון הגדרות המערכת.
        /// נתיב: PUT api/settings
        /// דורש: Token של Admin בלבד.
        /// </summary>
        /// <param name="settingsDto">ההגדרות החדשות (JSON)</param>
        [Authorize(Roles = "Admin")] // רק מנהל/ת
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] SystemSettingsDto settingsDto)
        {
            var result = await settingsService.UpdateSettingsAsync(settingsDto);

            return result.IsSuccess
                ? Ok(new { Message = "ההגדרות עודכנו בהצלחה!" })
                : result.ToActionResult();
        }
    }
}
