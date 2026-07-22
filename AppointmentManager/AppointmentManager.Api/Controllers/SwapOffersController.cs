// =====================================
// קובץ: SwapOffersController.cs
// שכבה: API → Controllers
// תפקיד: ניהול לוח העברת תורים.
//         GET    /api/swap-offers                    – שליפת הצעות פעילות (כל לקוח רשום)
//         POST   /api/swap-offers                    – יצירת הצעה
//         POST   /api/swap-offers/{id}/accept        – קבלת הצעה
//         DELETE /api/swap-offers/{id}               – ביטול הצעה
//         GET    /api/swap-offers/admin              – כל ההצעות (מנהלת)
//         POST   /api/swap-offers/admin/create       – פרסום הצעה (מנהלת)
//         POST   /api/swap-offers/{id}/admin-accept  – קבלת הצעה עבור לקוחה (מנהלת)
//         DELETE /api/swap-offers/{id}/admin         – ביטול הצעה (מנהלת)
// =====================================

using System.Security.Claims;
using AppointmentManager.Api.Extensions;
using AppointmentManager.Application.DTOs;
using AppointmentManager.Application.Interfaces;
using AppointmentManager.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentManager.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/swap-offers")]
    public class SwapOffersController(ISwapOfferService swapOfferService) : ControllerBase
    {
        /// <summary>
        /// מחזיר את כל הצעות ההחלפה הפעילות (תורים בתוך 24 שעות).
        /// GET api/swap-offers
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetActiveOffers()
        {
            var result = await swapOfferService.GetActiveOffersAsync();
            return result.IsSuccess ? Ok(result.Value) : result.ToActionResult();
        }

        /// <summary>
        /// יוצר הצעת העברה לתור קיים.
        /// POST api/swap-offers
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateOffer([FromBody] CreateSwapOfferRequest request)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var result = await swapOfferService.CreateOfferAsync(request.AppointmentId, Guid.Parse(userIdClaim));
            return result.IsSuccess ? Ok(new { Message = "ההצעה פורסמה בלוח בהצלחה!" }) : result.ToActionResult();
        }

        /// <summary>
        /// מקבל הצעה — מעביר את הבעלות על התור לקורא.
        /// POST api/swap-offers/{id}/accept
        /// </summary>
        [HttpPost("{id:guid}/accept")]
        public async Task<IActionResult> AcceptOffer(Guid id)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var result = await swapOfferService.AcceptOfferAsync(id, Guid.Parse(userIdClaim));
            return result.IsSuccess ? Ok(new { Message = "התור עבר אליך בהצלחה!" }) : result.ToActionResult();
        }

        /// <summary>
        /// מבטל הצעה — רק המציעה יכולה לבטל.
        /// DELETE api/swap-offers/{id}
        /// </summary>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> CancelOffer(Guid id)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var result = await swapOfferService.CancelOfferAsync(id, Guid.Parse(userIdClaim));
            return result.IsSuccess ? NoContent() : result.ToActionResult();
        }

        // ── endpoints מנהלת ──────────────────────────────────────────────────

        /// <summary>GET api/swap-offers/admin?status=0</summary>
        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllOffersAdmin([FromQuery] SwapOfferStatus? status)
        {
            var result = await swapOfferService.GetAllOffersAdminAsync(status);
            return result.IsSuccess ? Ok(result.Value) : result.ToActionResult();
        }

        /// <summary>POST api/swap-offers/admin/create  body: { appointmentId }</summary>
        [HttpPost("admin/create")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminCreateOffer([FromBody] CreateSwapOfferRequest request)
        {
            var result = await swapOfferService.AdminCreateOfferAsync(request.AppointmentId);
            return result.IsSuccess
                ? Ok(new { Message = "ההצעה פורסמה בהצלחה!" })
                : result.ToActionResult();
        }

        /// <summary>POST api/swap-offers/{id}/admin-accept  body: { targetClientId }</summary>
        [HttpPost("{id:guid}/admin-accept")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminAcceptOffer(Guid id, [FromBody] AdminAcceptOfferRequest request)
        {
            var result = await swapOfferService.AdminAcceptOfferAsync(id, request.TargetClientId);
            return result.IsSuccess
                ? Ok(new { Message = "התור הועבר ללקוחה בהצלחה!" })
                : result.ToActionResult();
        }

        /// <summary>DELETE api/swap-offers/{id}/admin</summary>
        [HttpDelete("{id:guid}/admin")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminCancelOffer(Guid id)
        {
            var result = await swapOfferService.AdminCancelOfferAsync(id);
            return result.IsSuccess ? NoContent() : result.ToActionResult();
        }
    }
}
