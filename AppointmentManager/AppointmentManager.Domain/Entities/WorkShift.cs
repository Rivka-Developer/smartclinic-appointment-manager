// =====================================
// קובץ: WorkShift.cs
// שכבה: Domain → Entities (ישויות ליבה)
// תפקיד: מגדיר את מבנה נתוני משמרת עבודה.
//         "משמרת" = פרק זמן שבו הקליניקה פתוחה ומקבלת לקוחות.
//         רק בתוך פרקי הזמן האלה ניתן לקבוע תורים.
//         לדוגמה: משמרת בוקר 09:00-13:00, משמרת ערב 16:00-20:00.
// =====================================

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppointmentManager.Domain.Entities
{
    /// <summary>
    /// מייצג משמרת עבודה - פרק זמן שבו הקליניקה פתוחה.
    /// המנהל/ת מגדיר/ת משמרות לכל יום, והמערכת מאפשרת קביעת תורים רק בתוכן.
    /// </summary>
    public class WorkShift
    {
        /// <summary>
        /// מזהה ייחודי של המשמרת.
        /// Guid.NewGuid() יוצר מזהה אקראי חדש אוטומטית בכל יצירת משמרת.
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// התאריך שבו מתקיימת המשמרת (לדוגמה: 01/06/2026).
        /// DateTime.Date - שומר תאריך בלבד (ללא שעה).
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// שעת תחילת המשמרת (לדוגמה: 09:00:00).
        /// TimeSpan = משך זמן / שעה ביום (שעות, דקות, שניות).
        /// שונה מ-DateTime שמייצג נקודת זמן מלאה עם תאריך.
        /// </summary>
        public TimeSpan StartTime { get; set; }

        /// <summary>
        /// שעת סיום המשמרת (לדוגמה: 13:00:00).
        /// חייבת להיות מאוחרת מ-StartTime - בדיקה זו מתבצעת ב-WorkShiftService.
        /// </summary>
        public TimeSpan EndTime { get; set; }

        /// <summary>
        /// שדה גרסה לבקרת מקביליות (Optimistic Concurrency Token).
        /// [Timestamp] מגדיר ש-SQL Server יעדכן שדה זה אוטומטית בכל שמירה.
        /// מונע עדכון משמרת על ידי שני משתמשים בו-זמנית.
        /// </summary>
        [Timestamp]
        public byte[] RowVersion { get; set; } = default!;
    }
}
