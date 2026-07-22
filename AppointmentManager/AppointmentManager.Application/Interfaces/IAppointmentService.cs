// =====================================
// קובץ: IAppointmentService.cs
// שכבה: Application → Interfaces
// תפקיד: מגדיר "חוזה" לשירות ניהול התורים.
//         כל פעולה הקשורה לתורים עוברת דרך שירות זה.
//         המימוש האמיתי ב-Services/AppointmentService.cs.
// =====================================

using AppointmentManager.Application.DTOs;
using AppointmentManager.Domain;
using AppointmentManager.Domain.Common;
using AppointmentManager.Domain.Entities;

namespace AppointmentManager.Application.Interfaces
{
    /// <summary>
    /// חוזה לשירות ניהול תורים: קביעה, ביטול, ושליפת מידע.
    /// </summary>
    public interface IAppointmentService
    {
        /// <summary>
        /// קובע תור עבור לקוח רשום (מהממשק של הלקוח).
        /// מבצע אימות, בדיקת זמינות, ושמירה בבסיס הנתונים.
        /// </summary>
        /// <param name="app">אובייקט התור המוכן (כולל ClientId שנשלף מה-Token)</param>
        Task<Result> BookAppointmentAsync(Appointment app);

        /// <summary>
        /// קובע תור ללקוח על ידי המנהל/ת (ממשק ניהולי).
        /// מחפש לקוח לפי טלפון, ואם לא קיים - יוצר ManagedClient חדש.
        /// </summary>
        /// <param name="request">נתוני התור + פרטי הלקוח (שם + טלפון)</param>
        Task<Result> AdminBookForClientAsync(AppointmentRequest request);

        /// <summary>
        /// מחזיר את הזמנים התפוסים ביום ספציפי - לתצוגת "לקוח".
        /// מאחד תורים חופפים + באפרים לגושי זמן תפוסים מחוברים.
        /// </summary>
        /// <param name="date">התאריך לבדיקה</param>
        Task<Result<List<TimeBlock>>> GetClientViewAsync(DateTime date);

        /// <summary>
        /// מחזיר את כל התורים בטווח תאריכים - ליומן המנהל/ת.
        /// כולל פרטי לקוח מלאים (שם, טלפון) לכל תור.
        /// </summary>
        /// <param name="start">תאריך תחילת הטווח</param>
        /// <param name="end">תאריך סיום הטווח</param>
        Task<Result<List<AppointmentResponse>>> GetAdminCalendarAsync(DateTime start, DateTime end);

        /// <summary>
        /// מבטל תור קיים.
        /// לקוח יכול לבטל רק את התור שלו, ורק עד 24 שעות לפני (ברירת מחדל).
        /// מנהל יכול לבטל כל תור ללא הגבלת זמן.
        /// </summary>
        /// <param name="appointmentId">מזהה התור לביטול</param>
        /// <param name="userId">מזהה המבצע הביטול</param>
        /// <param name="userRole">תפקיד המבצע (Admin/Client)</param>
        Task<Result> CancelAppointmentAsync(Guid appointmentId, Guid userId, UserRole userRole);

        /// <summary>
        /// מחזיר את היסטוריית התורים של משתמש ספציפי.
        /// כולל תורים עבר, עתיד, ומבוטלים.
        /// </summary>
        /// <param name="userId">מזהה המשתמש</param>
        Task<Result<List<AppointmentResponse>>> GetUserHistoryAsync(Guid userId);
    }
}
