// =====================================
// קובץ: WorkShiftsController.cs
// שכבה: API → Controllers
// תפקיד: Controller לניהול משמרות עבודה.
//         שליפה: כל משתמש מחובר יכול לראות משמרות.
//         הוספה/עדכון/מחיקה: Admin בלבד.
// =====================================

using AppointmentManager.Api.Extensions;
using AppointmentManager.Application.DTOs;
using AppointmentManager.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentManager.Api.Controllers
{
    /// <summary>
    /// Controller לניהול משמרות עבודה.
    /// נתיב בסיס: api/workshifts
    /// </summary>
    [ApiController]
    [Route("api/[controller]")] // "workshifts"
    [Authorize] // כל הפעולות דורשות Token (Admin עוד יצמצם)
    public class WorkShiftsController(IWorkShiftService workShiftService) : ControllerBase
    {
        /// <summary>
        /// שליפת משמרות ביום ספציפי.
        /// נתיב: GET api/workshifts/2026-06-01
        /// דורש: כל משתמש מחובר.
        /// </summary>
        /// <param name="date">התאריך לשליפת משמרות</param>
        [HttpGet("{date}")] // {date} = Route Parameter
        public async Task<IActionResult> GetByDate(DateTime date)
        {
            var result = await workShiftService.GetWorkShiftsByDateAsync(date);
            return result.IsSuccess ? Ok(result.Value) : result.ToActionResult();
        }

        /// <summary>
        /// הוספת משמרת חדשה.
        /// נתיב: POST api/workshifts
        /// דורש: Token של Admin בלבד.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")] // רק מנהל/ת
        public async Task<IActionResult> Add([FromBody] WorkShiftRequest request)
        {
            // [FromBody] = קריאת הנתונים מגוף ה-HTTP Request (JSON)
            var result = await workShiftService.AddWorkShiftAsync(request);
            return result.IsSuccess ? Ok(new { Message = "משמרת נוספה" }) : result.ToActionResult();
        }

        /// <summary>
        /// עדכון משמרת קיימת.
        /// נתיב: PUT api/workshifts/{id}
        /// דורש: Token של Admin בלבד.
        /// </summary>
        /// <param name="id">מזהה המשמרת לעדכון</param>
        /// <param name="request">הנתונים החדשים</param>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(Guid id, [FromBody] WorkShiftRequest request)
        {
            var result = await workShiftService.UpdateWorkShiftAsync(id, request);
            return result.IsSuccess ? Ok(new { Message = "משמרת עודכנה" }) : result.ToActionResult();
        }

        /// <summary>
        /// מחיקת משמרת.
        /// נתיב: DELETE api/workshifts/{id}
        /// דורש: Token של Admin בלבד.
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await workShiftService.DeleteWorkShiftAsync(id);
            return result.IsSuccess ? Ok(new { Message = "משמרת נמחקה" }) : result.ToActionResult();
        }
    }
}
