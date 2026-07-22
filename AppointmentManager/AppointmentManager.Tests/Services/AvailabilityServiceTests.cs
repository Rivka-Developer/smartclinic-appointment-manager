// =====================================
// קובץ: AvailabilityServiceTests.cs
// שכבה: Tests → Services
// תפקיד: בדיקות יחידה (Unit Tests) לאלגוריתם חישוב הזמינות (AvailabilityService).
//         AvailabilityService אחראי על שתי פונקציות מרכזיות:
//
//         1. CalculateFreeBlocks: מקבל משמרות ותורים קיימים → מחשב את הבלוקים הפנויים.
//            בדיקות: אין משמרות, ללא תורים, תור באמצע, תור בהתחלה,
//                     תור ממלא משמרת, שתי משמרות חופפות, מספר תורים,
//                     פילטר notBefore, זמן buffer.
//
//         2. CalculateMergedBusyBlocks: מקבל תורים → מחשב את הבלוקים התפוסים המאוחדים.
//            בדיקות: אין תורים, תור בודד, תור מבוטל, שני תורים לא חופפים,
//                     שני תורים חופפים דרך buffer, תורים מעורבים.
//
//         הערה: AvailabilityService מוגדר עם new(null!) - כי הבדיקות משתמשות רק
//         במתודות הסינכרוניות (CalculateFreeBlocks, CalculateMergedBusyBlocks)
//         שאינן דורשות IUnitOfWork.
// =====================================

using AppointmentManager.Application.DTOs;
using AppointmentManager.Application.Services;
using AppointmentManager.Domain;
using AppointmentManager.Domain.Entities;

namespace AppointmentManager.Tests.Services;

/// <summary>
/// מחלקת בדיקות ל-AvailabilityService.
/// בודקת את האלגוריתם של חישוב זמינות בבידוד מוחלט - ללא בסיס נתונים.
/// </summary>
public class AvailabilityServiceTests
{
    // Svc = מופע AvailabilityService לשימוש בכל הבדיקות
    // static = נוצר פעם אחת ומשותף לכל הבדיקות (מיטוב)
    // null! = IUnitOfWork לא נדרש לפונקציות הסינכרוניות שאנו בודקים
    //         "!" = operator של C# שאומר "אני יודע שזה null אבל לא יגרום לשגיאה"
    private static readonly AvailabilityService Svc = new(null!, null!);

    // ─── הגדרות ברירת מחדל לבדיקות ──────────────────────────────────────────

    /// <summary>
    /// הגדרות מערכת לבדיקות.
    /// מוגדרות כ-property (לא field) כדי שכל בדיקה תקבל עותק חדש (immutable).
    /// </summary>
    private static SystemSettings DefaultSettings => new()
    {
        BufferTime = 10,             // 10 דקות בין תורים
        MinGapSize = 15,             // חור מינימלי שנחשב "שמיש" = 15 דקות
        MorningMaxDuration = 120,    // בוקר: עד 120 דקות
        EveningMaxDuration = 40,     // ערב: עד 40 דקות
        EveningStartTime = new TimeSpan(16, 0, 0) // ערב מ-16:00
    };

    // ─── מתודות עזר (Helpers) ────────────────────────────────────────────────

    /// <summary>
    /// עוזר ליצירת WorkShift לבדיקות.
    /// במקום לכתוב new WorkShift { Date = ..., StartTime = ..., EndTime = ... } בכל בדיקה,
    /// משתמשים ב-Shift(date, start, end) - תמציתי וקריא יותר.
    /// </summary>
    /// <param name="date">התאריך של המשמרת</param>
    /// <param name="start">שעת התחלה (TimeSpan = שעה ללא תאריך, לדוגמה new TimeSpan(9, 0, 0) = 09:00)</param>
    /// <param name="end">שעת סיום</param>
    private static WorkShift Shift(DateTime date, TimeSpan start, TimeSpan end) =>
        new() { Date = date, StartTime = start, EndTime = end };

    /// <summary>
    /// עוזר ליצירת Appointment לבדיקות.
    /// ברירת מחדל של status = Scheduled (תזמון רגיל).
    /// </summary>
    /// <param name="start">שעת התחלה מלאה (DateTime = תאריך + שעה)</param>
    /// <param name="duration">משך בדקות</param>
    /// <param name="status">סטטוס התור (ברירת מחדל: Scheduled)</param>
    private static Appointment App(DateTime start, int duration, AppointmentStatus status = AppointmentStatus.Scheduled) =>
        new() { StartTime = start, DurationMinutes = duration, Status = status };

    // ─── בדיקות CalculateFreeBlocks ──────────────────────────────────────────

    /// <summary>
    /// בדיקה: כאשר אין משמרות - אין בלוקים פנויים.
    /// פשוטה מאוד: אם אין שעות עבודה - אין מה לקבוע.
    /// </summary>
    [Fact]
    public void CalculateFreeBlocks_NoShifts_ReturnsEmpty()
    {
        // [] = מערך ריק (C# 12 - תחביר מקוצר)
        var result = Svc.CalculateFreeBlocks([], [], DefaultSettings);
        Assert.Empty(result); // תוצאה ריקה
    }

    /// <summary>
    /// בדיקה: משמרת ללא תורים - הבלוק הפנוי הוא כל המשמרת.
    /// משמרת 09:00–17:00 + 0 תורים → בלוק פנוי: 09:00–17:00.
    /// </summary>
    [Fact]
    public void CalculateFreeBlocks_ShiftWithNoAppointments_ReturnsFullShift()
    {
        // === Arrange ===
        var date = new DateTime(2026, 6, 1); // 1 ביוני 2026 (תאריך דוגמה)
        var shifts = new[] { Shift(date, new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0)) }; // 09:00–17:00

        // === Act ===
        var result = Svc.CalculateFreeBlocks(shifts, [], DefaultSettings);

        // === Assert ===
        Assert.Single(result); // בלוק פנוי אחד בלבד
        Assert.Equal(date.AddHours(9), result[0].Start);  // מתחיל ב-09:00
        Assert.Equal(date.AddHours(17), result[0].End);   // מסתיים ב-17:00
    }

    /// <summary>
    /// בדיקה: תור באמצע משמרת מחלק אותה לשני בלוקים פנויים.
    /// משמרת 09:00–17:00, תור 12:00 למשך 60 דקות (עם buffer 10 דקות = בלוק עד 13:10):
    ///   בלוק 1: 09:00–12:00 (לפני התור)
    ///   בלוק 2: 13:10–17:00 (אחרי התור + buffer)
    /// </summary>
    [Fact]
    public void CalculateFreeBlocks_AppointmentInMiddle_ReturnsTwoBlocks()
    {
        // === Arrange ===
        var date = new DateTime(2026, 6, 1);
        var shifts = new[] { Shift(date, new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0)) };

        // date.AddHours(12) = 12:00 של אותו יום
        var apps = new[] { App(date.AddHours(12), 60) }; // תור 12:00 למשך שעה

        // === Act ===
        var result = Svc.CalculateFreeBlocks(shifts, apps, DefaultSettings);

        // === Assert ===
        Assert.Equal(2, result.Count); // שני בלוקים פנויים

        // בלוק ראשון: לפני התור
        Assert.Equal(date.AddHours(9), result[0].Start);  // 09:00
        Assert.Equal(date.AddHours(12), result[0].End);   // 12:00 (עד תחילת התור)

        // בלוק שני: אחרי התור + buffer
        // תור: 12:00 + 60 דקות = 13:00. Buffer: +10 דקות = 13:10
        // date.AddHours(12).AddMinutes(70) = 12:00 + 70 דקות = 13:10
        Assert.Equal(date.AddHours(12).AddMinutes(70), result[1].Start); // 13:10
        Assert.Equal(date.AddHours(17), result[1].End);                  // 17:00
    }

    /// <summary>
    /// בדיקה: תור בתחילת המשמרת - נשאר רק בלוק אחד בסוף (אחרי התור + buffer).
    /// משמרת 09:00–17:00, תור 09:00 למשך 60 דקות:
    ///   בלוק: 10:10–17:00 (09:00 + 60 + 10 buffer = 10:10)
    /// </summary>
    [Fact]
    public void CalculateFreeBlocks_AppointmentAtStart_ReturnsOneBlockAfter()
    {
        // === Arrange ===
        var date = new DateTime(2026, 6, 1);
        var shifts = new[] { Shift(date, new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0)) };
        var apps = new[] { App(date.AddHours(9), 60) }; // תור בדיוק בתחילת המשמרת

        // === Act ===
        var result = Svc.CalculateFreeBlocks(shifts, apps, DefaultSettings);

        // === Assert ===
        Assert.Single(result); // בלוק אחד בלבד (לפני התור אין זמן פנוי)

        // date.AddHours(9).AddMinutes(70) = 09:00 + 60 תור + 10 buffer = 10:10
        Assert.Equal(date.AddHours(9).AddMinutes(70), result[0].Start); // 10:10
        Assert.Equal(date.AddHours(17), result[0].End);                  // 17:00
    }

    /// <summary>
    /// בדיקה: תור שממלא את כל המשמרת - אין בלוקים פנויים.
    /// משמרת 09:00–10:00 (60 דקות), תור 09:00 למשך 60 דקות → אין מקום פנוי.
    /// </summary>
    [Fact]
    public void CalculateFreeBlocks_AppointmentFillsEntireShift_ReturnsEmpty()
    {
        // === Arrange ===
        var date = new DateTime(2026, 6, 1);
        var shifts = new[] { Shift(date, new TimeSpan(9, 0, 0), new TimeSpan(10, 0, 0)) }; // משמרת קצרה - 60 דקות
        var apps = new[] { App(date.AddHours(9), 60) }; // תור 60 דקות = ממלא הכל

        // === Act ===
        var result = Svc.CalculateFreeBlocks(shifts, apps, DefaultSettings);

        // === Assert ===
        Assert.Empty(result); // לא נשאר זמן פנוי
    }

    /// <summary>
    /// בדיקה: שתי משמרות חופפות מתמזגות לאחת לפני חישוב הבלוקים.
    /// משמרת 1: 09:00–13:00 + משמרת 2: 12:00–17:00 → מוזגת: 09:00–17:00.
    /// </summary>
    [Fact]
    public void CalculateFreeBlocks_TwoOverlappingShifts_MergedCorrectly()
    {
        // === Arrange ===
        var date = new DateTime(2026, 6, 1);
        var shifts = new[]
        {
            Shift(date, new TimeSpan(9, 0, 0), new TimeSpan(13, 0, 0)),  // 09:00–13:00
            Shift(date, new TimeSpan(12, 0, 0), new TimeSpan(17, 0, 0))  // 12:00–17:00 (חופף עם הראשונה)
        };

        // === Act ===
        var result = Svc.CalculateFreeBlocks(shifts, [], DefaultSettings);

        // === Assert ===
        Assert.Single(result); // בלוק מאוחד אחד (לא שניים!)

        // האלגוריתם ממזג לפני חישוב: 09:00–17:00
        Assert.Equal(date.AddHours(9), result[0].Start);  // 09:00
        Assert.Equal(date.AddHours(17), result[0].End);   // 17:00
    }

    /// <summary>
    /// בדיקה: מספר תורים ביום מחלקים את המשמרת לפי מספר תורים + 1 בלוקים.
    /// משמרת 09:00–18:00, שלושה תורים ב-10:00, 12:00, 15:00 → ארבעה בלוקים.
    /// </summary>
    [Fact]
    public void CalculateFreeBlocks_MultipleAppointments_CorrectGapsBetweenAll()
    {
        // === Arrange ===
        var date = new DateTime(2026, 6, 1);
        var shifts = new[] { Shift(date, new TimeSpan(9, 0, 0), new TimeSpan(18, 0, 0)) }; // 9 שעות

        var apps = new[]
        {
            App(date.AddHours(10), 30),  // תור 1: 10:00–10:30
            App(date.AddHours(12), 30),  // תור 2: 12:00–12:30
            App(date.AddHours(15), 30)   // תור 3: 15:00–15:30
        };

        // === Act ===
        var result = Svc.CalculateFreeBlocks(shifts, apps, DefaultSettings);

        // === Assert ===
        // 3 תורים = 4 בלוקים: לפני תור1, בין תור1-תור2, בין תור2-תור3, אחרי תור3
        Assert.Equal(4, result.Count);

        // בדיקת הבלוק הראשון (לפני התור הראשון)
        Assert.Equal(date.AddHours(9), result[0].Start);  // 09:00
        Assert.Equal(date.AddHours(10), result[0].End);   // 10:00 (עד תחילת תור 1)
    }

    /// <summary>
    /// בדיקה: פילטר notBefore מסנן בלוקים שמסתיימים לפני הזמן שהוגדר.
    /// שתי משמרות: 08:00–10:00 ו-14:00–18:00. notBefore = 12:00.
    /// רק המשמרת השנייה נשארת (הראשונה מסתיימת ב-10:00 < 12:00).
    /// </summary>
    [Fact]
    public void CalculateFreeBlocks_NotBeforeFilter_RemovesPastBlocks()
    {
        // === Arrange ===
        var date = new DateTime(2026, 6, 1);
        var shifts = new[]
        {
            Shift(date, new TimeSpan(8, 0, 0), new TimeSpan(10, 0, 0)),  // 08:00–10:00 (בעבר)
            Shift(date, new TimeSpan(14, 0, 0), new TimeSpan(18, 0, 0))  // 14:00–18:00 (בעתיד)
        };

        var notBefore = date.AddHours(12); // "עכשיו" לצורך בדיקה = 12:00

        // === Act ===
        // notBefore = מסנן בלוקים שמסתיימים לפני שעה זו
        var result = Svc.CalculateFreeBlocks(shifts, [], DefaultSettings, notBefore: notBefore);

        // === Assert ===
        Assert.Single(result); // רק בלוק אחד - המשמרת השנייה
        Assert.Equal(date.AddHours(14), result[0].Start); // מתחיל ב-14:00
    }

    /// <summary>
    /// בדיקה: Buffer Time מיושם נכון - הבלוק שאחרי תור מתחיל ב-EndTime+Buffer.
    /// תור 10:00 למשך 30 דקות, Buffer = 15 דקות → בלוק הבא מתחיל ב-10:45.
    /// </summary>
    [Fact]
    public void CalculateFreeBlocks_BufferTimeApplied_GapIsAppointmentPlusBuffer()
    {
        // === Arrange ===
        var date = new DateTime(2026, 6, 1);

        // הגדרות עם Buffer שונה מה-default (15 במקום 10) לצורך בדיקה
        var settings = new SystemSettings
        {
            BufferTime = 15,                                       // 15 דקות buffer
            MinGapSize = DefaultSettings.MinGapSize,               // שאר ההגדרות מ-Default
            MorningMaxDuration = DefaultSettings.MorningMaxDuration,
            EveningMaxDuration = DefaultSettings.EveningMaxDuration,
            EveningStartTime = DefaultSettings.EveningStartTime
        };

        var shifts = new[] { Shift(date, new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0)) };
        var apps = new[] { App(date.AddHours(10), 30) }; // תור 10:00 למשך 30 דקות

        // === Act ===
        var result = Svc.CalculateFreeBlocks(shifts, apps, settings);

        // === Assert ===
        // בלוק ראשון: 09:00–10:00 (לפני התור)
        // בלוק שני: 10:00 + 30 תור + 15 buffer = 10:45
        // result[1] = הבלוק שאחרי התור
        Assert.Equal(date.AddHours(10).AddMinutes(45), result[1].Start); // 10:45
    }

    // ─── בדיקות CalculateMergedBusyBlocks ────────────────────────────────────

    /// <summary>
    /// בדיקה: כאשר אין תורים - אין בלוקים תפוסים.
    /// </summary>
    [Fact]
    public void CalculateMergedBusyBlocks_NoAppointments_ReturnsEmpty()
    {
        // [] = רשימת תורים ריקה
        var result = Svc.CalculateMergedBusyBlocks([], DefaultSettings);
        Assert.Empty(result); // אין בלוקים תפוסים
    }

    /// <summary>
    /// בדיקה: תור בודד מחזיר בלוק תפוס אחד שכולל את ה-buffer.
    /// תור 10:00 למשך 30 דקות, buffer 10 → בלוק תפוס: 10:00–10:40.
    /// </summary>
    [Fact]
    public void CalculateMergedBusyBlocks_SingleAppointment_ReturnsSingleBlockWithBuffer()
    {
        // === Arrange ===
        var start = new DateTime(2026, 6, 1, 10, 0, 0); // 10:00 ב-1/6/2026
        var apps = new[] { App(start, 30) }; // תור 30 דקות

        // === Act ===
        var result = Svc.CalculateMergedBusyBlocks(apps, DefaultSettings);

        // === Assert ===
        Assert.Single(result); // בלוק תפוס אחד

        Assert.Equal(start, result[0].Start);                     // מתחיל ב-10:00
        Assert.Equal(start.AddMinutes(40), result[0].End);        // מסתיים ב-10:40 (30 + 10 buffer)
    }

    /// <summary>
    /// בדיקה: תור מבוטל (Cancelled) לא נחשב כבלוק תפוס.
    /// מוצג ללקוח שהשעה פנויה, למרות שהייתה קביעה שבוטלה.
    /// </summary>
    [Fact]
    public void CalculateMergedBusyBlocks_CancelledAppointment_Ignored()
    {
        // === Arrange ===
        var start = new DateTime(2026, 6, 1, 10, 0, 0);

        // App עם סטטוס Cancelled
        var apps = new[] { App(start, 30, AppointmentStatus.Cancelled) };

        // === Act ===
        var result = Svc.CalculateMergedBusyBlocks(apps, DefaultSettings);

        // === Assert ===
        Assert.Empty(result); // תור מבוטל = לא נלקח בחשבון
    }

    /// <summary>
    /// בדיקה: שני תורים שאינם חופפים (כולל ה-buffer) מחזירים שני בלוקים תפוסים נפרדים.
    /// תור 1: 10:00–10:30 (+buffer = 10:40), תור 2: 14:00 (הפרש גדול מספיק).
    /// </summary>
    [Fact]
    public void CalculateMergedBusyBlocks_TwoNonOverlapping_ReturnsTwoBlocks()
    {
        // === Arrange ===
        var date = new DateTime(2026, 6, 1);
        var apps = new[]
        {
            App(date.AddHours(10), 30), // תור 1: 10:00–10:30 (+buffer = 10:40)
            App(date.AddHours(14), 30)  // תור 2: 14:00 → לא חופף עם 10:40
        };

        // === Act ===
        var result = Svc.CalculateMergedBusyBlocks(apps, DefaultSettings);

        // === Assert ===
        Assert.Equal(2, result.Count); // שני בלוקים נפרדים - לא מאוחדים
    }

    /// <summary>
    /// בדיקה: שני תורים שחופפים דרך ה-buffer - מתמזגים לבלוק תפוס אחד.
    /// תור 1: 10:00–10:30 (+buffer 10 = 10:40), תור 2: 10:35 (לפני 10:40 → חפיפה).
    /// תוצאה: 10:00–11:15 (10:35 + 30 + 10 buffer = 11:15).
    /// </summary>
    [Fact]
    public void CalculateMergedBusyBlocks_TwoOverlappingViaBuffer_MergedIntoOne()
    {
        // === Arrange ===
        var date = new DateTime(2026, 6, 1);
        var apps = new[]
        {
            // תור ראשון: 10:00–10:30, עם buffer = תפוס עד 10:40
            App(date.AddHours(10), 30),

            // תור שני: 10:35 - מתחיל *לפני* סיום ה-buffer הראשון (10:40)
            // לכן האלגוריתם ממזג אותם לבלוק תפוס אחד
            App(date.AddHours(10).AddMinutes(35), 30)
        };

        // === Act ===
        var result = Svc.CalculateMergedBusyBlocks(apps, DefaultSettings);

        // === Assert ===
        Assert.Single(result); // בלוק מאוחד אחד!

        // הבלוק המאוחד: מתחיל עם התור הראשון
        Assert.Equal(date.AddHours(10), result[0].Start); // 10:00

        // ומסתיים עם סיום התור השני + buffer:
        // 10:35 + 30 דקות + 10 buffer = 11:15 = date.AddHours(10).AddMinutes(75)
        Assert.Equal(date.AddHours(10).AddMinutes(75), result[0].End); // 11:15
    }

    /// <summary>
    /// בדיקה: תורים מעורבים (מבוטל + מתוכנן) - רק המתוכנן נחשב.
    /// תור מבוטל ב-10:00 + תור מתוכנן ב-14:00 → רק בלוק אחד תפוס (14:00).
    /// </summary>
    [Fact]
    public void CalculateMergedBusyBlocks_MixedCancelledAndScheduled_OnlyScheduledCounted()
    {
        // === Arrange ===
        var date = new DateTime(2026, 6, 1);
        var apps = new[]
        {
            App(date.AddHours(10), 30, AppointmentStatus.Cancelled), // מבוטל - לא נחשב
            App(date.AddHours(14), 30, AppointmentStatus.Scheduled)  // מתוכנן - נחשב
        };

        // === Act ===
        var result = Svc.CalculateMergedBusyBlocks(apps, DefaultSettings);

        // === Assert ===
        Assert.Single(result); // בלוק אחד בלבד (התור המתוכנן)

        // הבלוק התפוס = רק התור ב-14:00
        Assert.Equal(date.AddHours(14), result[0].Start); // 14:00
    }
}
