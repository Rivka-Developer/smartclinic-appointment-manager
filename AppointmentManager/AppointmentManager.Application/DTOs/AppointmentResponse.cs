// =====================================
// קובץ: AppointmentResponse.cs
// שכבה: Application → DTOs
// תפקיד: מגדיר את מבנה התגובה שמוחזרת ללקוח לגבי תור.
//         AvailableSlotsResponse = תגובה עם חלונות הזמן הפנויים ביום.
//         AppointmentResponse = תגובה עם פרטי תור בודד.
// =====================================

using System;
using AppointmentManager.Domain;

namespace AppointmentManager.Application.DTOs
{
    /// <summary>
    /// תגובה עם כל חלונות הזמן הפנויים ביום ספציפי.
    /// מוחזרת כשלקוח מבקש לראות מתי ניתן לקבוע תור.
    /// </summary>
    /// <param name="Date">התאריך שעבורו נשלפו החלונות</param>
    /// <param name="FreeBlocks">רשימת חלונות הזמן הפנויים עם פרטי משך ואפשרויות</param>
    public record AvailableSlotsResponse(DateTime Date, List<TimeBlockDto> FreeBlocks)
    {
        /// <summary>בנאי ריק לאפשור JSON Deserialization</summary>
        public AvailableSlotsResponse() : this(DateTime.MinValue, new List<TimeBlockDto>()) { }
    }

    /// <summary>
    /// פרטי תור בודד כפי שמוחזרים ללקוח.
    /// שונה מ-Appointment (ישות הדומיין) - כולל שם ותפקיד לקוח מחושבים.
    /// לא חושף נתונים רגישים כמו PasswordHash.
    /// </summary>
    /// <param name="Id">מזהה ייחודי של התור</param>
    /// <param name="StartTime">זמן תחילת התור</param>
    /// <param name="EndTime">זמן סיום התור (מחושב: StartTime + DurationMinutes)</param>
    /// <param name="DurationMinutes">משך התור בדקות</param>
    /// <param name="Status">מצב התור: Scheduled / Cancelled</param>
    /// <param name="ClientName">שם הלקוח/ה (ממוּפה מ-Client.FullName)</param>
    /// <param name="ClientPhone">טלפון הלקוח/ה (ממוּפה מ-Client.PhoneNumber)</param>
    public record AppointmentResponse(
        Guid Id,
        DateTime StartTime,
        DateTime EndTime,
        int DurationMinutes,
        AppointmentStatus Status,
        string ClientName,
        string ClientPhone)
    {
        /// <summary>בנאי ריק לאפשור JSON Deserialization ו-AutoMapper</summary>
        public AppointmentResponse() : this(Guid.Empty, DateTime.MinValue, DateTime.MinValue, 0, default, string.Empty, string.Empty) { }
    }
}
