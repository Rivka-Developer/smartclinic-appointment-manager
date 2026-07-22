// =====================================
// קובץ: AppointmentsController.cs
// שכבה: API → Controllers
// תפקיד: Controller לניהול תורים - קביעה, ביטול, שליפת זמינות.
//         [Authorize] = כל הפעולות דורשות אימות (JWT Token בכותרת Authorization).
//         ישנם endpoints מסוימים עם [AllowAnonymous] - לא דורשים Token.
//         ישנם endpoints עם [Authorize(Roles = "Admin")] - דורשים תפקיד Admin.
// =====================================

using System.Security.Claims;
using AppointmentManager.Api.Extensions;
using AppointmentManager.Application.DTOs;
using AppointmentManager.Application.Interfaces;
using AppointmentManager.Application.Services;
using AppointmentManager.Domain;
using AppointmentManager.Domain.Entities;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AppointmentManager.Api.Controllers
{
    /// <summary>
    /// Controller לכל פעולות הקשורות לתורים.
    /// נתיב בסיס: api/appointments
    /// </summary>
    [Authorize] // ברירת מחדל: כל ה-endpoints דורשים Token תקין
    [ApiController]
    [Route("api/[controller]")] // "appointments"
    public class AppointmentsController(IAppointmentService appointmentService, IAvailabilityService availabilityService, IMapper mapper) : ControllerBase
    {
        /// <summary>
        /// קביעת תור ע"י לקוח רשום (ממשק הלקוח).
        /// נתיב: POST api/appointments/book
        /// דורש: Token תקין של כל משתמש.
        /// </summary>
        [HttpPost("book")]
        [EnableRateLimiting("BookingLimiter")]
        public async Task<IActionResult> Book([FromBody] AppointmentRequest request)
        {
            // שליפת מזהה המשתמש מה-Claims של ה-Token
            // ClaimTypes.NameIdentifier = מזהה המשתמש שנכנס ל-Token בהתחברות
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized(); // אין Token תקין

            // המרת ה-Request ל-Appointment Entity
            var newApp = mapper.Map<Appointment>(request);
            newApp.ClientId = Guid.Parse(userIdClaim); // שיוך התור למשתמש המחובר

            var result = await appointmentService.BookAppointmentAsync(newApp);

            return result.IsSuccess
                ? Ok(new { Message = "התור נקבע בהצלחה!" })
                : result.ToActionResult();
        }

        /// <summary>
        /// קביעת תור ידנית על ידי מנהל/ת עבור לקוח (עם שם + טלפון).
        /// נתיב: POST api/appointments/book-for-client
        /// דורש: Token של Admin בלבד.
        /// </summary>
        [Authorize(Roles = "Admin")] // רק Admin יכול לגשת
        [HttpPost("book-for-client")]
        public async Task<IActionResult> BookForClient([FromBody] AppointmentRequest request)
        {
            var result = await appointmentService.AdminBookForClientAsync(request);
            return result.IsSuccess
                ? Ok(new { Message = "התור נקבע בהצלחה!" })
                : result.ToActionResult();
        }

        /// <summary>
        /// שליפת חלונות הזמן הפנויים ביום ספציפי.
        /// נתיב: GET api/appointments/available-slots?date=2026-06-01
        /// [AllowAnonymous] = לא דורש Token - גם לא-מחוברים יכולים לראות זמינות.
        /// </summary>
        [HttpGet("available-slots")]
        [AllowAnonymous] // ביטול ברירת ה-[Authorize] הגלובלי
        public async Task<IActionResult> GetAvailableSlots([FromQuery] DateTime date)
        {
            // [FromQuery] = קריאת הפרמטר מה-Query String (ה-URL אחרי "?")
            var result = await availabilityService.GetAvailableSlotsAsync(date);
            return result.IsSuccess ? Ok(result.Value) : result.ToActionResult();
        }

        /// <summary>
        /// שליפת הזמנים התפוסים ביום - לתצוגת "לקוח" (בלוקים מאוחדים).
        /// נתיב: GET api/appointments/client-view?date=2026-06-01
        /// </summary>
        [HttpGet("client-view")]
        [AllowAnonymous]
        public async Task<IActionResult> GetClientView([FromQuery] DateTime date)
        {
            var result = await appointmentService.GetClientViewAsync(date);
            return result.IsSuccess ? Ok(result.Value) : result.ToActionResult();
        }

        /// <summary>
        /// שליפת כל התורים בטווח תאריכים - ליומן המנהל/ת.
        /// נתיב: GET api/appointments/admin-calendar?start=...&end=...
        /// דורש: Token של Admin בלבד.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("admin-calendar")]
        public async Task<IActionResult> GetAdminCalendar([FromQuery] DateTime start, [FromQuery] DateTime end)
        {
            var result = await appointmentService.GetAdminCalendarAsync(start, end);
            return result.IsSuccess ? Ok(result.Value) : result.ToActionResult();
        }

        /// <summary>
        /// ביטול תור לפי מזהה.
        /// נתיב: DELETE api/appointments/{id}
        /// לקוח: יכול לבטל רק את התור שלו ורק בתוך חלון הזמן המותר.
        /// מנהל: יכול לבטל כל תור ללא הגבלה.
        /// </summary>
        [HttpDelete("{id}")] // {id} = פרמטר נתיב (Route Parameter)
        public async Task<IActionResult> Cancel(Guid id)
        {
            // שליפת פרטי המשתמש מה-Token
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var roleClaim = User.FindFirstValue(ClaimTypes.Role); // "Admin" / "Client"
            if (string.IsNullOrEmpty(userIdClaim) || string.IsNullOrEmpty(roleClaim)) return Unauthorized();

            var userId = Guid.Parse(userIdClaim);
            var role = Enum.Parse<UserRole>(roleClaim); // המרת מחרוזת ל-Enum

            var result = await appointmentService.CancelAppointmentAsync(id, userId, role);

            return result.IsSuccess ? NoContent() : result.ToActionResult(); // 204 NoContent אם הצליח
        }

        /// <summary>
        /// שליפת היסטוריית תורים של המשתמש המחובר.
        /// נתיב: GET api/appointments/my-history
        /// דורש: Token תקין.
        /// </summary>
        [HttpGet("my-history")]
        [Authorize]
        public async Task<IActionResult> GetMyHistory()
        {
            // שליפת מזהה המשתמש המחובר מה-Token
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
                return Unauthorized();

            var userId = Guid.Parse(userIdClaim.Value);
            var result = await appointmentService.GetUserHistoryAsync(userId);

            return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
        }

        /// <summary>
        /// שליפת אפשרויות שיבוץ עבור בלוק פנוי ספציפי שהמשתמש בחר.
        /// נתיב: GET api/appointments/block-placement-options?date=...&durationMinutes=30&selectedBlockStart=10:00
        /// </summary>
        [HttpGet("block-placement-options")]
        public async Task<IActionResult> GetPlacementOptionsForBlock(
            [FromQuery] DateTime date,
            [FromQuery] int durationMinutes,
            [FromQuery] string selectedBlockStart) // שעה בפורמט "HH:mm" (לדוגמה: "10:00")
        {
            // המרת המחרוזת "10:00" ל-TimeSpan
            // TryParse = מנסה להמיר ומחזיר false אם לא הצליח (בלי לזרוק Exception)
            if (!TimeSpan.TryParse(selectedBlockStart, out var blockStartTime))
            {
                return BadRequest("פורמט שעת התחלה לא תקין. יש לשלוח בפורמט HH:mm");
            }

            var result = await availabilityService.GetPlacementOptionsForBlockAsync(date, durationMinutes, blockStartTime);

            return result.IsSuccess ? Ok(result.Value) : result.ToActionResult();
        }
    }
}
