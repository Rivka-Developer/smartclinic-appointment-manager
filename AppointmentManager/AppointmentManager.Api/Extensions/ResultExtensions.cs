// =====================================
// קובץ: ResultExtensions.cs
// שכבה: API → Extensions
// תפקיד: מגדיר Extension Methods להמרת Result לתגובת HTTP מתאימה.
//         Extension Method = מתודה שנוספת לסוג קיים מבלי לשנות אותו.
//         הממיר מחבר בין לוגיקת הדומיין (Result עם Error) לפרוטוקול HTTP (קודי Status).
//         שימוש: result.ToActionResult() במקום switch/if ארוך בכל Controller.
// =====================================

using AppointmentManager.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentManager.Api.Extensions
{
    /// <summary>
    /// Extension Methods להמרת Result לתגובת HTTP (IActionResult).
    /// "static class" = לא ניתן ליצור מופע.
    /// </summary>
    public static class ResultExtensions
    {
        /// <summary>
        /// ממיר Result (ללא ערך) לתגובת HTTP מתאימה.
        /// extension method על "this Result result" - נקרא כ-result.ToActionResult().
        /// </summary>
        public static IActionResult ToActionResult(this Result result)
        {
            // אם הצליח - החזר 200 OK ריק
            if (result.IsSuccess) return new OkResult();

            // אחרת - הגדר תגובת שגיאה לפי סוג השגיאה
            // switch expression = switch מודרני שמחזיר ערך
            return result.Error.Type switch
            {
                ErrorType.Validation   => new BadRequestObjectResult(CreateProblemDetails(result.Error, 400)),  // 400 = קלט שגוי
                ErrorType.NotFound     => new NotFoundObjectResult(CreateProblemDetails(result.Error, 404)),    // 404 = לא נמצא
                ErrorType.Conflict     => new ConflictObjectResult(CreateProblemDetails(result.Error, 409)),    // 409 = התנגשות
                ErrorType.Unauthorized => new UnauthorizedObjectResult(CreateProblemDetails(result.Error, 401)), // 401 = אין הרשאה
                _                      => new ObjectResult(CreateProblemDetails(result.Error, 500)) { StatusCode = 500 } // 500 = שגיאת שרת
            };
        }

        /// <summary>
        /// ממיר Result&lt;T&gt; (עם ערך) לתגובת HTTP מתאימה.
        /// בהצלחה - 200 OK עם הערך. בכישלון - קוד שגיאה מתאים.
        /// </summary>
        public static IActionResult ToActionResult<T>(this Result<T> result)
        {
            // אם הצליח - 200 OK עם הערך בגוף התגובה
            if (result.IsSuccess) return new OkObjectResult(result.Value);

            // בכישלון - אותו switch כמו למעלה
            return result.Error.Type switch
            {
                ErrorType.Validation   => new BadRequestObjectResult(CreateProblemDetails(result.Error, 400)),
                ErrorType.NotFound     => new NotFoundObjectResult(CreateProblemDetails(result.Error, 404)),
                ErrorType.Conflict     => new ConflictObjectResult(CreateProblemDetails(result.Error, 409)),
                ErrorType.Unauthorized => new UnauthorizedObjectResult(CreateProblemDetails(result.Error, 401)),
                _                      => new ObjectResult(CreateProblemDetails(result.Error, 500)) { StatusCode = 500 }
            };
        }

        /// <summary>
        /// יוצר ProblemDetails - פורמט סטנדרטי לתגובות שגיאה (RFC 7807).
        /// ProblemDetails = JSON אחיד לשגיאות: { "title": "...", "detail": "...", "status": 400 }
        /// </summary>
        /// <param name="error">השגיאה מהדומיין</param>
        /// <param name="statusCode">קוד ה-HTTP Status</param>
        private static ProblemDetails CreateProblemDetails(Error error, int statusCode) => new()
        {
            Title = error.Code,        // קוד השגיאה (למשל "Auth.InvalidCredentials")
            Detail = error.Description, // תיאור בעברית
            Status = statusCode         // קוד HTTP
        };
    }
}
