// =====================================
// קובץ: WorkShiftRequest.cs
// שכבה: Application → DTOs
// תפקיד: מגדיר את מבנה הבקשה להוספה/עדכון משמרת עבודה.
//         נשלח מה-Frontend ל-API כאשר המנהל/ת מגדיר/ת שעות עבודה.
// =====================================

using System.ComponentModel.DataAnnotations;

namespace AppointmentManager.Application.DTOs
{
    /// <summary>
    /// נתוני בקשה להוספה או עדכון משמרת עבודה.
    /// </summary>
    /// <param name="Date">תאריך המשמרת (לדוגמה: 2026-06-01)</param>
    /// <param name="StartTime">שעת תחילת המשמרת (לדוגמה: 09:00:00)</param>
    /// <param name="EndTime">שעת סיום המשמרת (לדוגמה: 13:00:00)</param>
    public record WorkShiftRequest(
        [Required] DateTime Date,       // [Required] = שדה חובה - תאריך
        [Required] TimeSpan StartTime,  // [Required] = שדה חובה - שעת התחלה (TimeSpan = שעה ביום)
        [Required] TimeSpan EndTime)    // [Required] = שדה חובה - שעת סיום
    {
        /// <summary>
        /// בנאי ריק הנדרש ל-JSON Deserialization.
        /// TimeSpan.Zero = 00:00:00 (חצות).
        /// </summary>
        public WorkShiftRequest() : this(DateTime.MinValue, TimeSpan.Zero, TimeSpan.Zero) { }
    }
}
