// =====================================
// קובץ: DateTimeHelpers.cs
// שכבה: Application → Helpers
// תפקיד: פונקציות עזר לחישובי זמן.
//         "static class" = לא ניתן ליצור מופע, פונקציות נגישות ישירות.
// =====================================

namespace AppointmentManager.Application.Helpers;

/// <summary>
/// פונקציות עזר לעיגול ועיבוד ערכי DateTime.
/// </summary>
public static class DateTimeHelpers
{
    /// <summary>
    /// מעגל DateTime לכפולת 5 הדקות הקרובה (תמיד כלפי מעלה).
    /// נדרש כי המערכת מאפשרת תורים רק בכפולות של 5 דקות.
    /// לדוגמה: 10:03 → 10:05 | 10:00 → 10:00 | 10:01 → 10:05.
    /// </summary>
    /// <param name="dt">ה-DateTime לעיגול</param>
    /// <returns>ה-DateTime המעוגל לכפולת 5 דקות הבאה</returns>
    public static DateTime RoundUpTo5Minutes(DateTime dt)
    {
        // שלב 1: חתוך את השניות והמילישניות - עבוד רק עם שעות ודקות
        var truncated = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0, dt.Kind);

        // שלב 2: אם יש שניות/מילישניות - עלה דקה אחת (כי אנחנו מעגלים כלפי מעלה)
        // dt.Second > 0 = יש שניות | dt.Millisecond > 0 = יש מילישניות
        if (dt.Second > 0 || dt.Millisecond > 0) truncated = truncated.AddMinutes(1);

        // שלב 3: חשב השארית לחלוקה ב-5
        // % = שארית מחלוקה. לדוגמה: 13 % 5 = 3 (כי 13 = 2*5 + 3)
        int rem = truncated.Minute % 5;

        // שלב 4: אם השארית 0 - כבר בכפולה, אחרת הוסף את ההפרש
        // לדוגמה: דקה=13, rem=3, הוסף 5-3=2 דקות → דקה=15
        return rem == 0 ? truncated : truncated.AddMinutes(5 - rem);
    }
}
