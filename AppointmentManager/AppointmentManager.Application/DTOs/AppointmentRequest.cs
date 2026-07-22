// =====================================
// קובץ: AppointmentRequest.cs
// שכבה: Application → DTOs
// תפקיד: מגדיר את מבנה הבקשה לקביעת תור.
//         נשלח מה-Frontend ל-API כאשר המנהל/ת קובע/ת תור ידנית ללקוח.
//         כולל נתוני הזמן וגם פרטי הלקוח (שם + טלפון לאיתור/יצירת הלקוח).
// =====================================

using System.ComponentModel.DataAnnotations;

namespace AppointmentManager.Application.DTOs
{
    /// <summary>
    /// נתוני בקשת קביעת תור ידנית על ידי המנהל/ת.
    /// מכיל גם פרטי תור (זמן, משך) וגם פרטי לקוח לאיתורו/יצירתו.
    /// </summary>
    /// <param name="StartTime">זמן תחילת התור (תאריך + שעה)</param>
    /// <param name="DurationMinutes">משך התור בדקות (5-240 דקות)</param>
    /// <param name="ClientName">שם הלקוח/ה (2-100 תווים)</param>
    /// <param name="ClientPhone">מספר טלפון ישראלי של הלקוח/ה</param>
    public record AppointmentRequest(
        [Required] DateTime StartTime,                               // [Required] = שדה חובה
        [Required][Range(5, 240)] int DurationMinutes,              // [Range] = ערך בין 5 ל-240 דקות בלבד
        [Required][MinLength(2)][MaxLength(100)] string ClientName, // שם בין 2 ל-100 תווים
        [Required][RegularExpression(                               // [RegularExpression] = אימות מספר טלפון ישראלי
            @"^0[2-9]\d{7,8}$",
            ErrorMessage = "מספר טלפון לא תקין. יש להזין מספר ישראלי תקין (לדוגמה: 0501234567)")]
            string ClientPhone)
    {
        /// <summary>
        /// בנאי ריק הנדרש ל-JSON Deserialization.
        /// DateTime.MinValue = ערך ה-DateTime הקטן ביותר האפשרי (01/01/0001).
        /// </summary>
        public AppointmentRequest() : this(DateTime.MinValue, 0, string.Empty, string.Empty) { }
    }
}
