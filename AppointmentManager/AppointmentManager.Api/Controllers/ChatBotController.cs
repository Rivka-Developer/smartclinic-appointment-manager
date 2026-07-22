using AppointmentManager.Application.DTOs.Chat;
using AppointmentManager.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AppointmentManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatBotController(IChatBotService chatBotService) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting("ChatLimiter")]
    public async Task<ActionResult<ChatResponse>> Chat([FromBody] ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { message = "ההודעה לא יכולה להיות ריקה." });

        var reply = await chatBotService.GetReplyAsync(request.Message, request.History);
        return Ok(new ChatResponse { Reply = reply });
    }
}
