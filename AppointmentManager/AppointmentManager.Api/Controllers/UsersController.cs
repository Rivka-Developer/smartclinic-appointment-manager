// =====================================
// קובץ: UsersController.cs
// שכבה: API → Controllers
// תפקיד: Controller לניהול לקוחות - גישה מנהלתית בלבד.
//         [Authorize(Roles = "Admin")] = כל הפעולות מוגבלות למנהלים.
//         מאפשר: צפייה ברשימת לקוחות ובפרטי לקוח ספציפי.
// =====================================

using AppointmentManager.Api.Extensions;
using AppointmentManager.Application.DTOs;
using AppointmentManager.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentManager.Api.Controllers;

/// <summary>
/// Controller לניהול לקוחות - לשימוש מנהלים בלבד.
/// נתיב בסיס: api/users
/// </summary>
[ApiController]
[Route("api/[controller]")] // "users"
[Authorize(Roles = "Admin")] // כל ה-Controller מוגבל לAdmins
public class UsersController(IUserService userService) : ControllerBase
{
    /// <summary>
    /// מחזיר רשימה ממוספרת (Paginated) של כל הלקוחות.
    /// נתיב: GET api/users/clients?pageNumber=1&pageSize=20
    /// </summary>
    /// <param name="pageNumber">מספר עמוד (ברירת מחדל: 1, מינימום: 1)</param>
    /// <param name="pageSize">גודל עמוד (ברירת מחדל: 20, בין 1 ל-100)</param>
    /// <returns>PagedResult עם רשימת הלקוחות ומידע על הדפדוף</returns>
    [HttpGet("clients")]
    // [ProducesResponseType] = תיעוד Swagger של סוגי התגובות האפשריים
    [ProducesResponseType(200, Type = typeof(PagedResult<UserResponse>))]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> GetAllClients(
        [FromQuery] int pageNumber = 1,   // ברירת מחדל: עמוד 1
        [FromQuery] int pageSize = 20)    // ברירת מחדל: 20 לקוחות
    {
        // תיקוני קלט: אם הערכים לא הגיוניים - איפוס לברירת מחדל
        if (pageNumber < 1) pageNumber = 1;               // לא מתחת ל-1
        if (pageSize < 1 || pageSize > 100) pageSize = 20; // בין 1 ל-100

        var result = await userService.GetAllClientsAsync(pageNumber, pageSize);

        return result.IsSuccess ? Ok(result.Value) : result.ToActionResult();
    }

    /// <summary>
    /// מחזיר פרטי לקוח ספציפי כולל כל היסטוריית התורים.
    /// נתיב: GET api/users/{id}/history
    /// </summary>
    /// <param name="id">מזהה הלקוח</param>
    [HttpGet("{id}/history")] // {id} = Route Parameter
    public async Task<IActionResult> GetClientHistory(Guid id)
    {
        var result = await userService.GetClientHistoryAsync(id);

        // אם לא נמצא - 404 Not Found עם פרטי השגיאה
        if (!result.IsSuccess) return NotFound(result.Error);

        return Ok(result.Value); // 200 OK עם UserHistoryResponse
    }

    /// <summary>
    /// מוחק לקוח ואת כל התורים שלו לצמיתות.
    /// נתיב: DELETE api/users/{id}
    /// </summary>
    /// <param name="id">מזהה הלקוח למחיקה</param>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteClient(Guid id)
    {
        var result = await userService.DeleteUserAsync(id);

        if (!result.IsSuccess) return result.ToActionResult();

        return NoContent(); // 204 No Content - מחיקה הצליחה
    }
}
