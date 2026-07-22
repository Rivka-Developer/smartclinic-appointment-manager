using Hangfire.Dashboard;

namespace AppointmentManager.Api.Middleware;

/// <summary>
/// מגביל גישה ל-Dashboard של Hangfire למנהלים מחוברים בלבד.
/// </summary>
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User.Identity?.IsAuthenticated == true &&
               httpContext.User.IsInRole("Admin");
    }
}
