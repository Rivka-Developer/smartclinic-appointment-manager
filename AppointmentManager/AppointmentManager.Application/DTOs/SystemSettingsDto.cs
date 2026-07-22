// =====================================
// קובץ: SystemSettingsDto.cs
// שכבה: Application → DTOs
// תפקיד: מגדיר את מבנה הגדרות המערכת כפי שמוחזרות/מתקבלות ב-API.
//         שונה מ-SystemSettings (ישות הדומיין) - לא כולל את שדה ה-Id.
//         מאפשר עדכון הגדרות בלי לחשוף את ה-Id הפנימי.
// =====================================

using System.ComponentModel.DataAnnotations;

namespace AppointmentManager.Application.DTOs
{
    /// <summary>
    /// הגדרות מערכת לשליחה/קבלה ב-API.
    /// </summary>
    public class SystemSettingsDto
    {
        [Required]
        public TimeSpan EveningStartTime { get; set; }

        [Required][Range(5, 480, ErrorMessage = "משך בוקר מקסימלי חייב להיות בין 5 ל-480 דקות")]
        public int MorningMaxDuration { get; set; }

        [Required][Range(5, 480, ErrorMessage = "משך ערב מקסימלי חייב להיות בין 5 ל-480 דקות")]
        public int EveningMaxDuration { get; set; }

        [Required][Range(0, 60, ErrorMessage = "זמן מאגר חייב להיות בין 0 ל-60 דקות")]
        public int BufferTime { get; set; }

        [Required][Range(0, 120, ErrorMessage = "גודל חור מינימלי חייב להיות בין 0 ל-120 דקות")]
        public int MinGapSize { get; set; }

        [Required][Range(0, 168, ErrorMessage = "מגבלת ביטול חייבת להיות בין 0 ל-168 שעות")]
        public int CancellationDeadlineHours { get; set; }

        [EmailAddress(ErrorMessage = "כתובת אימייל לא תקינה")]
        [MaxLength(150)]
        public string AdminContactEmail { get; set; } = string.Empty;

        [Required][MaxLength(100)]
        public string BusinessName { get; set; } = string.Empty;
    }
}
