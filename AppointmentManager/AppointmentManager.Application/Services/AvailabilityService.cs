// =====================================
// קובץ: AvailabilityService.cs
// שכבה: Application → Services
// תפקיד: הלב האלגוריתמי של המערכת - חישוב זמינות תורים.
//         מכיל שני אלגוריתמים מרכזיים:
//         1. CalculateFreeBlocks: מחשב חלונות זמן פנויים (מפחית תורים ממשמרות)
//         2. CalculateMergedBusyBlocks: מאחד תורים חופפים לגושים תפוסים
//         כמו גם פונקציות שמשתמשות בהם לצרכים שונים.
// =====================================

using AppointmentManager.Application.DTOs;
using AppointmentManager.Application.Helpers;
using AppointmentManager.Application.Interfaces;
using AppointmentManager.Domain;
using AppointmentManager.Domain.Common;
using AppointmentManager.Domain.Entities;
using AppointmentManager.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace AppointmentManager.Application.Services;

/// <summary>
/// מימוש שירות חישוב זמינות.
/// unitOfWork = גישה לנתוני משמרות, תורים והגדרות מבסיס הנתונים.
/// cache = SystemSettings נשמרות בזיכרון לשעה כדי להפחית שאילתות DB חוזרות.
/// </summary>
public class AvailabilityService(IUnitOfWork unitOfWork, IMemoryCache cache) : IAvailabilityService
{
    private const string SettingsCacheKey = "SystemSettings";
    private static readonly TimeSpan SettingsCacheDuration = TimeSpan.FromHours(1);

    private async Task<SystemSettings?> GetCachedSettingsAsync()
    {
        if (!cache.TryGetValue(SettingsCacheKey, out SystemSettings? settings))
        {
            settings = await unitOfWork.Settings.GetSettingsAsync();
            if (settings != null)
                cache.Set(SettingsCacheKey, settings, SettingsCacheDuration);
        }
        return settings;
    }

    /// <summary>
    /// מחשב ומחזיר את כל חלונות הזמן הפנויים עבור תאריך ספציפי.
    /// לימים שעברו מוצגים כל הבלוקים; להיום ועתיד מסננים לפי הזמן הנוכחי.
    /// </summary>
    public async Task<Result<AvailableSlotsResponse>> GetAvailableSlotsAsync(DateTime date)
    {
        // שליפת הנתונים הדרושים לחישוב
        var shifts = await unitOfWork.Shifts.GetSortedShiftsByDateAsync(date);
        var apps = await unitOfWork.Appointments.GetActiveAppointmentsByDateAsync(date);
        var settings = await GetCachedSettingsAsync();

        if (settings == null)
            return Result.Failure<AvailableSlotsResponse>(Error.NotFound("Settings.NotFound", "הגדרות מערכת לא נמצאו"));

        // לימים שעברו אין סינון (DateTime.MinValue = בלי סף); להיום ועתיד מסננים לפי הזמן הנוכחי
        bool isPastDate = date.Date < DateTime.Today;
        var notBefore = isPastDate ? DateTime.MinValue : DateTimeHelpers.RoundUpTo5Minutes(DateTime.UtcNow);

        // חישוב בלוקים פנויים (מסנן בלוקים שמסתיימים לפני notBefore)
        var freeBlocks = CalculateFreeBlocks(shifts, apps, settings, notBefore: notBefore);

        // המרת הבלוקים ל-DTOs עם מידע על משכים מותרים לכל בלוק
        var blockDtos = freeBlocks
            .Select(b =>
            {
                // תחילת אפקטיבית: אם הבלוק מתחיל לפני notBefore – נתחיל מ-notBefore
                var effectiveStart = b.Start < notBefore ? notBefore : b.Start;
                int effectiveDuration = (int)(b.End - effectiveStart).TotalMinutes; // משך בפועל

                // קביעת מקסימום לפי שעה ביום
                bool isEvening = effectiveStart.TimeOfDay >= settings.EveningStartTime;
                int maxAllowed = isEvening ? settings.EveningMaxDuration : settings.MorningMaxDuration;

                // בניית רשימת משכים חוקיים (כפולות של 5 שמשאירות Gap מספיק)
                var validDurations = new List<int>();
                for (int dur = 5; dur <= maxAllowed; dur += 5) // מ-5 עד maxAllowed בקפיצות של 5
                {
                    int totalNeeded = dur + settings.BufferTime; // סך הזמן הנדרש
                    if (totalNeeded > effectiveDuration) break;  // הבלוק קצר מדי - הפסק

                    int gap = effectiveDuration - totalNeeded; // הזמן שנשאר אחרי התור
                    // מותר: Gap=0 (מנצלים הכל) או Gap>=MinGapSize (חור מספיק גדול)
                    if (gap == 0 || gap >= settings.MinGapSize)
                        validDurations.Add(dur);
                }

                // יצירת DTO עם כל המידע
                return new TimeBlockDto(effectiveStart, b.End, true)
                {
                    MaxAllowedDuration = Math.Min(maxAllowed, effectiveDuration), // מינימום בין השניים
                    ValidDurations = validDurations
                };
            }).ToList();

        return Result.Success(new AvailableSlotsResponse(date, blockDtos));
    }

    /// <summary>
    /// מחשב אפשרויות שיבוץ עבור בלוק פנוי ספציפי שהמשתמש לחץ עליו.
    /// </summary>
    public async Task<Result<PlacementOptionsResponse>> GetPlacementOptionsForBlockAsync(
        DateTime date,
        int durationMinutes,
        TimeSpan selectedBlockStart) // שעת ההתחלה של הבלוק שנבחר, כפי שהוצגה ללקוחה מקריאת available-slots קודמת
    {
        // שלב 1: שליפת הגדרות ונתונים גולמיים
        var settings = await GetCachedSettingsAsync();
        if (settings == null)
            return Result.Failure<PlacementOptionsResponse>(Error.NotFound("Settings.NotFound", "הגדרות מערכת לא נמצאו"));

        var shifts = await unitOfWork.Shifts.GetSortedShiftsByDateAsync(date);
        var apps = await unitOfWork.Appointments.GetActiveAppointmentsByDateAsync(date);

        // שלב 2: חישוב בלוקים פנויים גולמיים (ללא סינון "מעכשיו") - כדי שההשוואה לפי selectedBlockStart
        // תהיה יציבה גם אם עברו כמה דקות בין הצגת הבלוק ללקוחה לבין הבחירה שלה במשך הטיפול.
        // (אם נשווה מול בלוק שכבר "נחתך" לפי הזמן הנוכחי, קפיצה של הזמן לחלון 5 הדקות הבא
        // תזיז את תחילת הבלוק קדימה ותגרום ל-404 שגוי על בלוק שעדיין פנוי בפועל)
        var rawFreeBlocks = CalculateFreeBlocks(shifts, apps, settings);

        var buffer = settings.BufferTime;
        var minGap = settings.MinGapSize;
        var totalNeeded = durationMinutes + buffer;

        // שלב 3: מציאת הבלוק הספציפי לפי שעת ההתחלה המקורית (הגולמית)
        var rawBlock = rawFreeBlocks.FirstOrDefault(b => b.Start.TimeOfDay == selectedBlockStart);

        if (rawBlock == null)
            return Result.Failure<PlacementOptionsResponse>(Error.NotFound("Block.NotFound", "הבלוק הנבחר כבר אינו פנוי במערכת"));

        // שלב 4: הצמדת תחילת הבלוק לזמן הנוכחי אם צריך (כמו ב-GetAvailableSlotsAsync)
        bool isPastDate = date.Date < DateTime.Today;
        var notBefore = isPastDate ? DateTime.MinValue : DateTimeHelpers.RoundUpTo5Minutes(DateTime.UtcNow);
        var effectiveStart = rawBlock.Start < notBefore ? notBefore : rawBlock.Start;

        if (effectiveStart >= rawBlock.End)
            return Result.Failure<PlacementOptionsResponse>(Error.NotFound("Block.NotFound", "הבלוק הנבחר כבר אינו פנוי במערכת"));

        var block = rawBlock with { Start = effectiveStart };
        var blockDurationMinutes = (block.End - block.Start).TotalMinutes;

        // בדיקה: האם הבלוק מסוגל להכיל את התור?
        if (blockDurationMinutes < totalNeeded)
            return Result.Failure<PlacementOptionsResponse>(Error.Validation("Block.TooShort", "הבלוק קצר מדי למשך הטיפול שנבחר"));

        // שלב 4: חישוב אפשרויות שיבוץ
        var stickToStart = block.Start.TimeOfDay; // הצמדה להתחלה

        // הצמדה לסוף: שעת התחלה שגורמת לסיום בדיוק בסוף הבלוק
        var stickToEnd = block.End.Subtract(TimeSpan.FromMinutes(totalNeeded)).TimeOfDay;

        List<TimeRangeDto> otherRanges = [];

        // חישוב טווח "אמצע" אם יש מקום לשני מרווחים מינימליים
        if (blockDurationMinutes >= totalNeeded + (2 * minGap))
        {
            otherRanges.Add(new TimeRangeDto(
                block.Start.Add(TimeSpan.FromMinutes(minGap)).TimeOfDay,
                block.End.Subtract(TimeSpan.FromMinutes(totalNeeded + minGap)).TimeOfDay
            ));
        }

        // בניית התגובה עבור הבלוק הבודד
        var placementOption = new BlockPlacementOptions(
            block.Start.TimeOfDay,
            block.End.TimeOfDay,
            stickToStart,
            stickToEnd,
            otherRanges
        );

        return Result.Success(new PlacementOptionsResponse(new List<BlockPlacementOptions> { placementOption }));
    }

    /// <summary>
    /// מחשב את חלונות הזמן הפנויים מתוך רשימת משמרות ותורים קיימים.
    /// זהו האלגוריתם המרכזי של המערכת.
    ///
    /// שלבי האלגוריתם:
    ///   שלב א' - איחוד משמרות: ממיין ומחבר משמרות חופפות לגושי עבודה.
    ///   שלב ב' - הכנת תורים: מוסיף באפר לכל תור.
    ///   שלב ג' - חיתוך: מפחית תורים תפוסים מגושי העבודה.
    /// </summary>
    public List<TimeBlock> CalculateFreeBlocks(
        IEnumerable<WorkShift> shifts,
        IEnumerable<Appointment> apps,
        SystemSettings settings,
        DateTime? notBefore = null) // DateTime? = nullable DateTime (null = אין סינון)
    {
        // ===== שלב א': איחוד משמרות =====

        // מיון המשמרות לפי שעת התחלה
        var sortedShifts = shifts.OrderBy(s => s.StartTime).ToList();
        if (sortedShifts.Count == 0) return []; // אם אין משמרות - אין זמן פנוי

        // אתחול עם המשמרת הראשונה
        var currentWork = new TimeBlock(
            sortedShifts[0].Date.Add(sortedShifts[0].StartTime), // המרת TimeSpan+Date ל-DateTime
            sortedShifts[0].Date.Add(sortedShifts[0].EndTime)
        );

        List<TimeBlock> workBlocks = [];

        // מעבר על שאר המשמרות ואיחוד חופפות
        for (int i = 1; i < sortedShifts.Count; i++)
        {
            var nextStart = sortedShifts[i].Date.Add(sortedShifts[i].StartTime);
            var nextEnd = sortedShifts[i].Date.Add(sortedShifts[i].EndTime);

            if (nextStart <= currentWork.End) // יש חפיפה (הבאה מתחילה לפני שהנוכחית נגמרת)
            {
                // מיזוג: הארך את הבלוק הנוכחי אם הבא מסתיים מאוחר יותר
                // "with" expression = יצירת עותק של record עם שינוי שדה אחד
                currentWork = currentWork with { End = nextEnd > currentWork.End ? nextEnd : currentWork.End };
            }
            else
            {
                // אין חפיפה - שמור את הנוכחי והתחל חדש
                workBlocks.Add(currentWork);
                currentWork = new TimeBlock(nextStart, nextEnd);
            }
        }
        workBlocks.Add(currentWork); // הוספת הבלוק האחרון

        // ===== שלב ב': הכנת תורים תפוסים =====

        // המרת תורים לבלוקי זמן תפוסים כולל באפר
        var taken = apps.Select(a => new TimeBlock(
            a.StartTime,                                              // התחלת הבלוק = התחלת התור
            a.StartTime.AddMinutes(a.DurationMinutes + settings.BufferTime) // סוף = סוף תור + באפר
        )).OrderBy(t => t.Start).ToList(); // מיון הכרחי לאלגוריתם

        // ===== שלב ג': חיתוך תורים מגושי עבודה — O(W+T) במקום O(W*T) =====
        // workBlocks ו-taken כבר ממוינים לפי Start.
        // שני מצביעים מתקדמים קדימה — כל בלוק נסרק פעם אחת בלבד.

        List<TimeBlock> freeBlocks = [];
        int takenStart = 0; // מצביע גלובלי: דלג על taken שסיימו לפני work הנוכחי

        foreach (var work in workBlocks)
        {
            // דלג על taken שמסתיימים לפני תחילת גוש העבודה הנוכחי
            while (takenStart < taken.Count && taken[takenStart].End <= work.Start)
                takenStart++;

            DateTime pointer = work.Start;
            int j = takenStart; // מצביע מקומי: מתקדם רק בתוך גוש זה

            while (j < taken.Count && taken[j].Start < work.End)
            {
                var t = taken[j];
                if (t.Start > pointer)
                    freeBlocks.Add(new TimeBlock(pointer, t.Start));
                pointer = t.End > pointer ? t.End : pointer;
                j++;
            }

            if (pointer < work.End)
                freeBlocks.Add(new TimeBlock(pointer, work.End));
        }

        // סינון לפי "notBefore" - הסרת בלוקים שמסתיימים לפני הסף
        if (notBefore.HasValue)
            freeBlocks = freeBlocks.Where(b => b.End > notBefore.Value).ToList();

        return freeBlocks;
    }

    /// <summary>
    /// מחשב ומאחד בלוקי זמן תפוסים לתצוגת לקוח.
    /// מחבר תורים חופפים (כולל הבאפר) לגוש אחד רציף.
    /// </summary>
    public List<TimeBlock> CalculateMergedBusyBlocks(IEnumerable<Appointment> apps, SystemSettings settings)
    {
        // שלב 1: המרת תורים פעילים לבלוקי זמן תפוסים כולל באפר, ומיון
        var sortedBusyBlocks = apps
            .Where(a => a.Status == AppointmentStatus.Scheduled) // רק תורים פעילים
            .Select(a => new TimeBlock(
                a.StartTime,
                a.StartTime.AddMinutes(a.DurationMinutes + settings.BufferTime))) // עם באפר
            .OrderBy(b => b.Start) // מיון עולה
            .ToList();

        // אם אין תורים - החזר רשימה ריקה
        if (!sortedBusyBlocks.Any())
            return new List<TimeBlock>();

        // שלב 2: תהליך האיחוד
        var mergedBlocks = new List<TimeBlock>();
        var currentBlock = sortedBusyBlocks[0]; // התחלה עם הראשון

        for (int i = 1; i < sortedBusyBlocks.Count; i++)
        {
            var nextBlock = sortedBusyBlocks[i];

            if (nextBlock.Start <= currentBlock.End) // יש חפיפה
            {
                // מיזוג: הארך את הנוכחי אם הבא מסתיים מאוחר יותר
                currentBlock = currentBlock with
                {
                    End = nextBlock.End > currentBlock.End ? nextBlock.End : currentBlock.End
                };
            }
            else
            {
                // אין חפיפה - שמור את הנוכחי והתחל חדש
                mergedBlocks.Add(currentBlock);
                currentBlock = nextBlock;
            }
        }

        mergedBlocks.Add(currentBlock); // הוספת הבלוק האחרון
        return mergedBlocks;
    }

    /// <summary>
    /// בודק האם חלון זמן ספציפי פנוי לתור.
    /// בודק: 1. האם הסלוט בתוך חלון פנוי? 2. האם לא נוצרים חורים קטנים מדי?
    /// </summary>
    public async Task<Result> IsSlotAvailableAsync(DateTime start, int durationMinutes)
    {
        // שליפת נתונים
        var settings = await GetCachedSettingsAsync();
        if (settings == null) return Result.Failure(AppointmentErrors.NotFound);

        var shifts = await unitOfWork.Shifts.GetSortedShiftsByDateAsync(start.Date);
        var apps = await unitOfWork.Appointments.GetActiveAppointmentsByDateAsync(start.Date);

        // חישוב חלונות פנויים
        var freeBlocks = CalculateFreeBlocks(shifts, apps, settings);
        var totalNeeded = durationMinutes + settings.BufferTime; // סך הזמן הנדרש
        var endTimeWithBuffer = start.AddMinutes(totalNeeded);   // סוף התור כולל באפר

        // חיפוש חלון שמכיל את הסלוט הנדרש לחלוטין
        // start >= b.Start = התחלה תקינה | endTimeWithBuffer <= b.End = סוף תקין
        var targetBlock = freeBlocks.FirstOrDefault(b => start >= b.Start && endTimeWithBuffer <= b.End);

        if (targetBlock == null)
            return Result.Failure(AppointmentErrors.NoSlotFound); // לא נמצא חלון מתאים

        // בדיקת Gap: האם נוצרים חורים קטנים מדי לפני/אחרי התור?
        double gapBefore = (start - targetBlock.Start).TotalMinutes;         // חור לפני
        double gapAfter = (targetBlock.End - endTimeWithBuffer).TotalMinutes; // חור אחרי

        // חור > 0 ו-< MinGapSize = חור בלתי-שימושי
        if ((gapBefore > 0 && gapBefore < settings.MinGapSize) ||
            (gapAfter > 0 && gapAfter < settings.MinGapSize))
        {
            return Result.Failure(AppointmentErrors.SmallGap);
        }

        return Result.Success(); // הכל תקין - הסלוט זמין
    }
}
