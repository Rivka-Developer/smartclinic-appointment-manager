// =====================================
// קובץ: UserHistoryResponse.cs
// שכבה: Application → DTOs
// תפקיד: מגדיר את מבנה התגובה המכילה פרטי לקוח יחד עם היסטוריית התורים המלאה שלו.
//         משמש ב-endpoint שמחזיר "כרטיס לקוח" כולל כל ההיסטוריה.
// =====================================

namespace AppointmentManager.Application.DTOs;

/// <summary>
/// פרטי לקוח מלאים יחד עם כל היסטוריית התורים שלו/ה.
/// מוחזר כשמנהל/ת מבקש/ת לצפות בפרופיל מלא של לקוח.
/// </summary>
/// <param name="Id">מזהה ייחודי של הלקוח/ה</param>
/// <param name="FullName">שם מלא</param>
/// <param name="PhoneNumber">מספר טלפון</param>
/// <param name="Email">כתובת אימייל</param>
/// <param name="Appointments">רשימת כל התורים (עבר וגם עתיד, כולל מבוטלים)</param>
public record UserHistoryResponse(
    Guid Id,
    string FullName,
    string PhoneNumber,
    string Email,
    List<AppointmentResponse> Appointments // רשימת AppointmentResponse - לא Appointment (ישות הדומיין)
){
    /// <summary>
    /// בנאי ריק הנדרש ל-AutoMapper ו-JSON Deserialization.
    /// new List&lt;AppointmentResponse&gt;() = רשימה ריקה כברירת מחדל.
    /// </summary>
    public UserHistoryResponse() : this(Guid.Empty, "", "", "", new List<AppointmentResponse>()) { }
}
