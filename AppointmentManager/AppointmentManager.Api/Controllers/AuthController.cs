using AppointmentManager.Application.DTOs.Auth;
using AppointmentManager.Application.Interfaces;
using AppointmentManager.Api.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AppointmentManager.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("AuthLimiter")]
    public class AuthController(IAuthService authService, IWebHostEnvironment env) : ControllerBase
    {
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var result = await authService.RegisterAsync(request);
            if (!result.IsSuccess) return result.ToActionResult();
            SetAuthCookie(result.Value.Token);
            return Ok(new { result.Value.FullName, result.Value.Role });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await authService.LoginAsync(request);
            if (!result.IsSuccess) return result.ToActionResult();
            SetAuthCookie(result.Value.Token);
            return Ok(new { result.Value.FullName, result.Value.Role });
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("access_token");
            return NoContent();
        }

        private void SetAuthCookie(string token)
        {
            var isDev = env.IsDevelopment();
            Response.Cookies.Append("access_token", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = !isDev,                          // Secure רק בייצור
                SameSite = isDev ? SameSiteMode.Lax : SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddHours(2)
            });
        }
    }
}
