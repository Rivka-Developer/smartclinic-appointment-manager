// =====================================
// קובץ: IEmailService.cs
// שכבה: Application → Interfaces
// תפקיד: מגדיר "חוזה" לשירות שליחת אימיילים.
//         הפרדה לממשק מאפשרת: בסביבת פיתוח - לוג בלבד, בסביבת ייצור - SMTP אמיתי.
//         המימוש האמיתי ב-Infrastructure/Services/EmailService.cs.
// =====================================

namespace AppointmentManager.Application.Interfaces
{
    /// <summary>
    /// חוזה לשירות שליחת אימיילים.
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// שולח אימייל לכתובת המצוינת.
        /// בסביבת פיתוח (ללא SMTP): כותב ל-Log בלבד (ללא שליחה אמיתית).
        /// בסביבת ייצור: שולח אימייל אמיתי דרך SMTP.
        /// </summary>
        /// <param name="to">כתובת הנמען (לדוגמה: customer@gmail.com)</param>
        /// <param name="subject">נושא האימייל</param>
        /// <param name="body">גוף האימייל (טקסט רגיל)</param>
        Task SendEmailAsync(string to, string subject, string body);
    }
}
