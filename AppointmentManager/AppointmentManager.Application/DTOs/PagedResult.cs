// =====================================
// קובץ: PagedResult.cs
// שכבה: Application → DTOs
// תפקיד: מגדיר תגובה כללית לשאילתות עם דפדוף (Pagination).
//         Pagination = חלוקת רשימות ארוכות לעמודים קטנים.
//         לדוגמה: 500 לקוחות → 25 לעמוד → 20 עמודים.
//         T = סוג הפריטים (Generic) - מאפשר שימוש לכל סוג נתון.
// =====================================

namespace AppointmentManager.Application.DTOs
{
    /// <summary>
    /// תגובה כללית לשאילתות עם דפדוף.
    /// T = "Generic Type Parameter" - פלייסהולדר לסוג הנתון האמיתי.
    /// לדוגמה: PagedResult&lt;UserResponse&gt; מחזיר עמוד של UserResponse.
    /// </summary>
    public class PagedResult<T>
    {
        /// <summary>
        /// הפריטים בעמוד הנוכחי.
        /// IEnumerable = ממשק המאפשר מעבר על כל האלמנטים ברשימה.
        /// "init" = ניתן לאתחול בלבד (לא לשינוי לאחר מכן).
        /// </summary>
        public IEnumerable<T> Items { get; init; }

        /// <summary>המספר הכולל של כל הפריטים (בכל העמודים יחד)</summary>
        public int TotalCount { get; init; }

        /// <summary>מספר העמוד הנוכחי (מתחיל מ-1)</summary>
        public int PageNumber { get; init; }

        /// <summary>מספר הפריטים בכל עמוד</summary>
        public int PageSize { get; init; }

        /// <summary>
        /// מספר העמודים הכולל - מחושב אוטומטית.
        /// Math.Ceiling = עיגול כלפי מעלה (כדי שלא "ייחתכו" פריטים).
        /// לדוגמה: 101 פריטים / 20 לעמוד = 5.05 → עיגול = 6 עמודים.
        /// (double) = המרה ל-double לאפשור חילוק עם שארית.
        /// </summary>
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

        /// <summary>
        /// האם יש עמוד הבא? true אם העמוד הנוכחי אינו האחרון.
        /// </summary>
        public bool HasNextPage => PageNumber < TotalPages;

        /// <summary>
        /// האם יש עמוד קודם? true אם הדפדוף לא בעמוד הראשון.
        /// </summary>
        public bool HasPreviousPage => PageNumber > 1;

        /// <summary>
        /// בנאי - מאתחל את כל שדות התוצאה.
        /// </summary>
        public PagedResult(IEnumerable<T> items, int totalCount, int pageNumber, int pageSize)
        {
            Items = items;           // רשימת הפריטים בעמוד זה
            TotalCount = totalCount; // סך כל הפריטים
            PageNumber = pageNumber; // עמוד נוכחי
            PageSize = pageSize;     // גודל עמוד
        }
    }
}
