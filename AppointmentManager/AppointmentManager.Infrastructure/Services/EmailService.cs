// =====================================
// קובץ: EmailService.cs
// שכבה: Infrastructure → Services
// תפקיד: מימוש שליחת אימיילים דרך SMTP.
//         SMTP = פרוטוקול שרת הדואר (כמו Gmail, Outlook וכו').
//         בסביבת פיתוח (ללא הגדרת SmtpHost): כותב ל-Log בלבד - לא שולח אימייל אמיתי.
//         בסביבת ייצור: שולח אימייל אמיתי דרך שרת ה-SMTP שהוגדר.
//         מממש את IEmailService מהאפליקציה.
// =====================================

using System.Net;
using System.Net.Mail;
using AppointmentManager.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AppointmentManager.Infrastructure.Services
{
    /// <summary>
    /// מימוש שירות שליחת אימיילים דרך SMTP.
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;       // ללוגים ולהודעות Debug
        private readonly IConfiguration _configuration;        // לקריאת הגדרות SMTP

        /// <summary>
        /// קונסטרקטור - מקבל Logger ו-Configuration מה-DI Container.
        /// </summary>
        public EmailService(ILogger<EmailService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// שולח אימייל. אם SMTP לא מוגדר (בסביבת פיתוח) - כותב ל-Log בלבד.
        /// </summary>
        /// <param name="to">כתובת הנמען</param>
        /// <param name="subject">נושא האימייל</param>
        /// <param name="body">גוף האימייל</param>
        public async Task SendEmailAsync(string to, string subject, string body)
        {
            // קריאת כתובת שרת ה-SMTP מהקונפיגורציה
            var smtpHost = _configuration["Email:SmtpHost"];

            // אם אין הגדרת SMTP - מצב פיתוח: לוג בלבד, ללא שליחה
            if (string.IsNullOrWhiteSpace(smtpHost))
            {
                // LogInformation = הדפסה ל-Console בפורמט Structured Logging
                _logger.LogInformation("[Email - Dev Mode] To: {To} | Subject: {Subject}", to, subject);
                return; // יציאה מוקדמת - לא ממשיכים לשליחה
            }

            // קריאת כל הגדרות ה-SMTP מהקונפיגורציה
            var smtpPort = _configuration.GetValue<int>("Email:SmtpPort", 587); // פורט ברירת מחדל: 587
            var smtpUser = _configuration["Email:SmtpUser"] ?? string.Empty;
            var smtpPassword = _configuration["Email:SmtpPassword"] ?? string.Empty;
            var fromAddress = _configuration["Email:FromAddress"] ?? smtpUser; // אם אין - שתמש ב-User
            var fromName = _configuration["Email:FromName"] ?? "SmartClinic";  // שם שיוצג כשולח
            var enableSsl = _configuration.GetValue<bool>("Email:EnableSsl", true); // SSL ברירת מחדל: true

            // יצירת לקוח SMTP - "using" מבטיח שחרור משאבים אוטומטי
            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl = enableSsl,                                        // הצפנת החיבור
                Credentials = new NetworkCredential(smtpUser, smtpPassword)   // פרטי אימות
            };

            // בניית הודעת האימייל
            var message = new MailMessage
            {
                From = new MailAddress(fromAddress, fromName), // שולח (כתובת + שם)
                Subject = subject,                              // נושא
                Body = body,                                    // גוף ההודעה
                IsBodyHtml = true                               // HTML מעוצב
            };
            message.To.Add(to); // הוספת הנמען

            try
            {
                await client.SendMailAsync(message); // שליחה אסינכרונית
                _logger.LogInformation("אימייל נשלח בהצלחה לכתובת: {To}", to); // לוג הצלחה
            }
            catch (Exception ex)
            {
                // לוג שגיאה (כולל Exception details) ו-Re-Throw (זריקה מחדש)
                _logger.LogError(ex, "שגיאה בשליחת אימייל לכתובת: {To}", to);
                throw; // Re-Throw = הפצת החריגה הלאה
            }
        }
    }
}
