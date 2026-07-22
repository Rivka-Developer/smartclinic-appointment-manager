// =====================================
// קובץ: IAvailabilityService.cs
// שכבה: Application → Interfaces
// תפקיד: מגדיר "חוזה" לשירות בדיקת זמינות - הלב האלגוריתמי של המערכת.
//         שירות זה אחראי לחישוב:
//         1. אילו חלונות זמן פנויים ביום?
//         2. היכן בדיוק ניתן לשבץ תור ממשך מסוים?
//         3. האם חלון זמן ספציפי פנוי לתור?
//         המימוש האמיתי ב-Services/AvailabilityService.cs.
// =====================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AppointmentManager.Application.DTOs;
using AppointmentManager.Domain.Common;
using AppointmentManager.Domain.Entities;

namespace AppointmentManager.Application.Interfaces
{
    /// <summary>
    /// חוזה לשירות חישוב זמינות תורים.
    /// </summary>
    public interface IAvailabilityService
    {
        /// <summary>
        /// מחשב ומחזיר את כל חלונות הזמן הפנויים ביום ספציפי.
        /// לוקח בחשבון: משמרות + תורים קיימים + באפר + MinGap + שעת "עכשיו".
        /// </summary>
        Task<Result<AvailableSlotsResponse>> GetAvailableSlotsAsync(DateTime date);

        /// <summary>
        /// מחשב את חלונות הזמן הפנויים מתוך משמרות ותורים קיימים.
        /// זהו האלגוריתם הבסיסי שכל שאר המתודות מסתמכות עליו.
        /// נחשף בממשק כדי לאפשר שימוש ישיר ובדיקות יחידה.
        /// </summary>
        /// <param name="shifts">רשימת משמרות (יכולות לחפוף, יאוחדו אוטומטית)</param>
        /// <param name="apps">רשימת תורים קיימים</param>
        /// <param name="settings">הגדרות מערכת (BufferTime, MinGapSize)</param>
        /// <param name="notBefore">אם נמסר - מסנן בלוקים שמסתיימים לפני זמן זה</param>
        List<TimeBlock> CalculateFreeBlocks(IEnumerable<WorkShift> shifts, IEnumerable<Appointment> apps, SystemSettings settings, DateTime? notBefore = null);

        /// <summary>
        /// מחשב ומאחד בלוקי זמן תפוסים לתצוגת לקוח.
        /// מחבר תורים חופפים + באפרים לגושים רציפים.
        /// </summary>
        List<TimeBlock> CalculateMergedBusyBlocks(IEnumerable<Appointment> apps, SystemSettings settings);

        /// <summary>
        /// מחשב אפשרויות שיבוץ עבור בלוק ספציפי שהמשתמש בחר.
        /// </summary>
        /// <param name="selectedBlockStart">שעת ההתחלה של הבלוק שנבחר</param>
        Task<Result<PlacementOptionsResponse>> GetPlacementOptionsForBlockAsync(DateTime date, int durationMinutes, TimeSpan selectedBlockStart);

        /// <summary>
        /// בודק האם חלון זמן ספציפי פנוי לתור ממשך מסוים.
        /// בודק גם שאין "חורים קטנים" (SmallGap) לפני/אחרי.
        /// </summary>
        Task<Result> IsSlotAvailableAsync(DateTime start, int durationMinutes);
    }
}
