// =====================================
// קובץ: Appointment.cs
// שכבה: Domain → Entities (ישויות ליבה)
// תפקיד: מגדיר את מבנה נתוני תור בודד.
//         כל שורה בטבלת Appointments בבסיס הנתונים מיוצגת על ידי אובייקט מסוג Appointment.
//         כולל גם לוגיקת אימות בסיסית (Validate) שאינה תלויה בשום שירות חיצוני.
// =====================================

using System.ComponentModel.DataAnnotations;
using AppointmentManager.Domain.Common;

namespace AppointmentManager.Domain.Entities
{
    /// <summary>
    /// מייצג תור בודד ביומן הקליניקה.
    /// כולל את זמן התחלת התור, משכו, הלקוח שקבע אותו, ומצבו הנוכחי.
    /// </summary>
    public class Appointment
    {
        /// <summary>
        /// מזהה ייחודי של התור.
        /// Guid.NewGuid() יוצר מזהה אקראי חדש אוטומטית בכל יצירת תור.
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// מזהה הלקוח שקבע את התור (מפתח זר לטבלת Users).
        /// זהו ה"קשר" בין התור לבין הלקוח - שומר רק את ה-Id, לא את כל נתוני הלקוח.
        /// </summary>
        public Guid ClientId { get; set; }

        /// <summary>
        /// אובייקט הלקוח המלא - נטען מבסיס הנתונים בעת הצורך (Lazy/Eager Loading).
        /// default! אומר: "השדה יאותחל לפני שימוש, אל תזהיר על null".
        /// </summary>
        public User Client { get; set; } = default!;

        /// <summary>
        /// זמן תחילת התור (תאריך + שעה) בפורמט UTC (זמן עולמי אחיד).
        /// UTC מונע בעיות של אזורי זמן שונים בין שרת ולקוח.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// משך התור בדקות (לדוגמה: 30 לחצי שעה, 60 לשעה שלמה).
        /// </summary>
        public int DurationMinutes { get; set; }

        /// <summary>
        /// זמן סיום התור - מחושב אוטומטית מ-StartTime + DurationMinutes.
        /// "=>" פירושו: "כל פעם שגישים לשדה זה, חשב ושלח את התוצאה".
        /// לא נשמר בבסיס הנתונים - מחושב בזמן ריצה.
        /// </summary>
        public DateTime EndTime => StartTime.AddMinutes(DurationMinutes);

        /// <summary>
        /// מצב התור הנוכחי: Scheduled (קבוע) או Cancelled (בוטל).
        /// ראה הגדרה מלאה ב-Enums.cs.
        /// </summary>
        public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;

        /// <summary>
        /// שדה גרסה לבקרת מקביליות (Optimistic Concurrency Token).
        /// [Timestamp] מגדיר ש-SQL Server יעדכן שדה זה אוטומטית בכל שמירה.
        /// אם שני משתמשים מנסים לשנות את אותו תור בו-זמנית,
        /// הגרסאות לא יתאימו ותיזרק ConcurrencyConflictException.
        /// byte[] = מערך בייטים (0-255) המייצג בינארי.
        /// </summary>
        [Timestamp]
        public byte[] RowVersion { get; set; } = default!;

        /// <summary>
        /// זמן שליחת תזכורת האימייל. null = טרם נשלחה.
        /// משמש לאידמפוטנטיות: Hangfire לא ישלח תזכורת כפולה גם ב-retry.
        /// </summary>
        public DateTime? ReminderSentAt { get; private set; }

        public void SetReminderSent(DateTime sentAt) => ReminderSentAt = sentAt;

        /// <summary>
        /// מאמת שהתור תקין לפי כללי עסקיים בסיסיים.
        /// מחזיר Result.Success() אם הכל תקין,
        /// או Result.Failure(שגיאה) אם יש בעיה.
        /// </summary>
        public Result Validate()
        {
            // בדיקה 1: האם זמן התחלת התור הוא בעבר?
            // DateTime.UtcNow = הזמן הנוכחי בפורמט UTC
            if (StartTime < DateTime.UtcNow)
                return Result.Failure(AppointmentErrors.PastDate); // שגיאה: לא ניתן לקבוע תור בעבר

            // בדיקה 2: האם הדקות הן כפולה של 5? (לדוגמה: 10:00, 10:05, 10:10... ולא 10:03)
            // % = שארית מחלוקה. אם 10:03, אז 3 % 5 = 3 (לא 0) → שגיאה.
            if (StartTime.Minute % 5 != 0)
                return Result.Failure(AppointmentErrors.InvalidInterval); // שגיאה: זמן לא בכפולות של 5

            // כל הבדיקות עברו - התור תקין
            return Result.Success();
        }
    }
}
