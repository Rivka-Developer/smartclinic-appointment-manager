// =====================================
// קובץ: GlobalExceptionHandler.cs
// שכבה: API → Middleware
// תפקיד: תופס כל חריגה לא-מטופלת שנזרקת בשרת ומחזיר תגובה מסודרת ללקוח.
//         Middleware = רכיב שנמצא "באמצע" בין קבלת הבקשה לטיפול בה.
//         ללא handler זה - חריגה לא-מטופלת תחזיר HTML מכוער לחזית במקום JSON.
//         IExceptionHandler = ממשק של ASP.NET Core לטיפול גלובלי בחריגות.
// =====================================

using AppointmentManager.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AppointmentManager.Api.Middleware;

/// <summary>
/// מטפל גלובלי בחריגות לא-צפויות.
/// ממפה סוגי חריגות ידועים לקודי HTTP מתאימים במקום 500 גורף.
/// </summary>
public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title, detail) = exception switch
        {
            ConcurrencyConflictException =>
                (StatusCodes.Status409Conflict,
                 "Conflict",
                 "הנתונים השתנו על ידי משתמש אחר. אנא נסה שנית."),

            DbUpdateConcurrencyException =>
                (StatusCodes.Status409Conflict,
                 "Conflict",
                 "התנגשות בעדכון – הנתונים עודכנו על ידי תהליך אחר."),

            UnauthorizedAccessException =>
                (StatusCodes.Status401Unauthorized,
                 "Unauthorized",
                 "אין הרשאה לבצע פעולה זו."),

            _ =>
                (StatusCodes.Status500InternalServerError,
                 "Server Error",
                 "התרחשה שגיאה פנימית בשרת, אנא נסה שנית מאוחר יותר.")
        };

        // שגיאות 500 בלבד נרשמות כ-Error; שאר הסוגים הידועים – כ-Warning
        if (status == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "שגיאה לא צפויה: {Message}", exception.Message);
        else
            logger.LogWarning(exception, "חריגה מטופלת ({Status}): {Message}", status, exception.Message);

        var problemDetails = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail
        };

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
