// =====================================
// קובץ: User.cs
// שכבה: Domain → Entities (ישויות ליבה)
// תפקיד: מגדיר את מבנה נתוני המשתמש במערכת.
//         "Entity" = ישות שנשמרת בבסיס הנתונים ויש לה מזהה ייחודי (Id).
//         כל שורה בטבלת Users בבסיס הנתונים מיוצגת על ידי אובייקט מסוג User.
// =====================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppointmentManager.Domain;

namespace AppointmentManager.Domain.Entities
{
    /// <summary>
    /// מייצג משתמש במערכת ניהול התורים.
    /// יכול להיות מנהל/ת (Admin), לקוח/ה עצמאי/ת (Client),
    /// או לקוח/ה מנוהל/ת שנוצר על ידי המנהל/ת (ManagedClient).
    /// </summary>
    public class User
    {
        /// <summary>
        /// מזהה ייחודי של המשתמש.
        /// Guid = מספר ארוך ואקראי שמבטיח ייחודיות ברמה גלובלית.
        /// Guid.NewGuid() יוצר מזהה חדש אוטומטית בעת יצירת משתמש.
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// שם מלא של המשתמש (לדוגמה: "ישראל ישראלי").
        /// default! אומר לקומפיילר: "אני יודע שהשדה יאותחל לפני שימוש, אל תזהיר".
        /// </summary>
        public string FullName { get; set; } = default!;

        /// <summary>
        /// כתובת האימייל - משמשת גם לכניסה למערכת וגם לשליחת התראות.
        /// חייבת להיות ייחודית בבסיס הנתונים (מוגדר ב-ApplicationDbContext).
        /// </summary>
        public string Email { get; set; } = default!;

        /// <summary>
        /// מספר הטלפון - בפורמט ישראלי (לדוגמה: 0501234567).
        /// משמש לחיפוש לקוח קיים בעת קביעת תור ידנית על ידי המנהל/ת.
        /// </summary>
        public string PhoneNumber { get; set; } = default!;

        /// <summary>
        /// הסיסמה המוצפנת (Hash).
        /// הסיסמה המקורית לא נשמרת - רק גרסה מוצפנת שלה באמצעות BCrypt.
        /// BCrypt הוא אלגוריתם הצפנה חד-כיווני: ניתן לאמת סיסמה אבל לא לפענח אותה.
        /// </summary>
        public string PasswordHash { get; set; } = default!;

        /// <summary>
        /// תפקיד המשתמש במערכת: Admin, Client, או ManagedClient.
        /// ראה הגדרה מלאה בקובץ Enums.cs.
        /// </summary>
        public UserRole Role { get; set; }

        /// <summary>
        /// רשימת כל התורים שקבע משתמש זה.
        /// זהו "קשר אחד לרבים" (One-to-Many): לקוח אחד - הרבה תורים.
        /// ICollection מאפשר הוספה, הסרה וספירה של פריטים ברשימה.
        /// מאותחלת כרשימה ריקה כדי למנוע NullReferenceException.
        /// </summary>
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}
