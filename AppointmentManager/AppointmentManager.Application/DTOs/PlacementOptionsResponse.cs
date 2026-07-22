// =====================================
// קובץ: PlacementOptionsResponse.cs
// שכבה: Application → DTOs
// תפקיד: מגדיר את מבנה התגובה לשאלה "איפה בדיוק אפשר לשבץ את התור?"
//         כשלקוח בחר בלוק פנוי ומשך תור, המערכת מחשבת שלוש אסטרטגיות שיבוץ:
//         1. הצמדה להתחלת הבלוק (StickToStart)
//         2. הצמדה לסוף הבלוק (StickToEnd)
//         3. שיבוץ ב"אמצע" - טווח חופשי שמשאיר מרווח משני הצדדים (ValidOtherRanges)
// =====================================

using System;
using System.Collections.Generic;

namespace AppointmentManager.Application.DTOs
{
    /// <summary>
    /// תגובה המכילה את כל האפשרויות לשיבוץ תור בכל הבלוקים הפנויים ביום.
    /// ה-Frontend משתמש בנתונים אלה להציג ללקוח היכן בדיוק ניתן לשבץ את התור.
    /// </summary>
    /// <param name="AvailableBlocks">רשימת בלוקי הזמן הפנויים עם אפשרויות שיבוץ בכל אחד</param>
    public record PlacementOptionsResponse(List<BlockPlacementOptions> AvailableBlocks);

    /// <summary>
    /// אפשרויות השיבוץ עבור בלוק פנוי אחד ספציפי.
    /// </summary>
    /// <param name="BlockStart">שעת תחילת הבלוק הפנוי (TimeSpan = שעה ביום)</param>
    /// <param name="BlockEnd">שעת סיום הבלוק הפנוי</param>
    /// <param name="StickToStart">
    /// שעת תחילת אפשרות "הצמדה להתחלה" = תחילת הבלוק עצמו.
    /// התור יתחיל בדיוק כשהבלוק מתחיל.
    /// </param>
    /// <param name="StickToEnd">
    /// שעת תחילת אפשרות "הצמדה לסוף" = סוף הבלוק פחות (משך + באפר).
    /// התור ייסתיים בדיוק כשהבלוק מסתיים.
    /// </param>
    /// <param name="ValidOtherRanges">
    /// טווחי שעות ל"אפשרות אמצע" - שיבוץ שמשאיר מרווח (MinGap) משני הצדדים.
    /// ריק אם הבלוק קטן מדי להשאיר מרווח כפול.
    /// </param>
    public record BlockPlacementOptions(
        TimeSpan BlockStart,
        TimeSpan BlockEnd,
        TimeSpan StickToStart,
        TimeSpan StickToEnd,
        List<TimeRangeDto> ValidOtherRanges
    );

    /// <summary>
    /// מייצג טווח שעות חוקי לשיבוץ אמצעי.
    /// הלקוח יכול לבחור כל שעה שנמצאת בין Start ל-End.
    /// </summary>
    /// <param name="Start">שעת ההתחלה המוקדמת ביותר לאפשרות אמצע</param>
    /// <param name="End">שעת ההתחלה המאוחרת ביותר לאפשרות אמצע</param>
    public record TimeRangeDto(TimeSpan Start, TimeSpan End);
}
