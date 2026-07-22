// =====================================
// קובץ: WorkShiftResponse.cs
// שכבה: Application → DTOs
// תפקיד: מגדיר את מבנה המשמרת כפי שמוחזרת ללקוח ב-API.
//         שונה מ-WorkShift (ישות הדומיין) - לא כולל RowVersion ונתונים פנימיים.
// =====================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppointmentManager.Application.DTOs
{
    /// <summary>
    /// פרטי משמרת עבודה כפי שמוחזרים ללקוח.
    /// </summary>
    /// <param name="Id">מזהה ייחודי של המשמרת</param>
    /// <param name="Date">התאריך שבו מתקיימת המשמרת</param>
    /// <param name="StartTime">שעת תחילת המשמרת (TimeSpan = שעה ביום)</param>
    /// <param name="EndTime">שעת סיום המשמרת</param>
    public record WorkShiftResponse(Guid Id, DateTime Date, TimeSpan StartTime, TimeSpan EndTime)
    {
        /// <summary>
        /// בנאי ריק הנדרש ל-AutoMapper ול-JSON Deserialization.
        /// Guid.Empty = מזהה אפסים "00000000-0000-0000-0000-000000000000".
        /// </summary>
        public WorkShiftResponse() : this(Guid.Empty, DateTime.MinValue, TimeSpan.Zero, TimeSpan.Zero) { }
    }
}
