// =====================================
// קובץ: Result.cs
// שכבה: Domain → Common (משותף לכל השכבות)
// תפקיד: מגדיר את "עטיפת התוצאה" שכל פעולה במערכת מחזירה.
//         במקום לזרוק חריגות (Exceptions) לכל בעיה, כל פעולה מחזירה Result.
//         Result יכול להיות: הצלחה (Success) או כישלון (Failure עם שגיאה מצורפת).
//         גישה זו (Railway-Oriented Programming) מאפשרת קוד נקי וצפוי.
// =====================================

using System;

namespace AppointmentManager.Domain.Common
{
    /// <summary>
    /// מייצג תוצאה של פעולה שאינה מחזירה ערך (פשוט הצלחה/כישלון).
    /// לדוגמה: מחיקת תור, עדכון הגדרות.
    /// </summary>
    public class Result
    {

        /// <summary>
        /// האם הפעולה הצליחה? true = כן, false = לא.
        /// "{ get; }" = ניתן לקרוא בלבד, לא לשנות מחוץ למחלקה.
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        /// האם הפעולה נכשלה? היפוך של IsSuccess לנוחות קריאת הקוד.
        /// "=>" = מחושב מ-IsSuccess בזמן ריצה.
        /// </summary>
        public bool IsFailure => !IsSuccess;

        /// <summary>
        /// השגיאה המצורפת לתוצאה. אם הצליח - יהיה Error.None (ריק).
        /// </summary>
        public Error Error { get; }

          /// <summary>
        /// קונסטרקטור פרטי - לא ניתן לצור Result ישירות, רק דרך Success() ו-Failure().
        /// "protected" פירושו שרק מחלקה זו ותת-מחלקות שלה יכולות לקרוא לו.
        /// </summary>
        /// <param name="isSuccess">האם הפעולה הצליחה?</param>
        /// <param name="error">השגיאה (חייבת להיות None אם הצליח, חייבת להיות אמיתית אם נכשל)</param>
        protected Result(bool isSuccess, Error error)
        {
            // אימות הגיוניות: לא יכול להיות גם הצלחה וגם שגיאה, וגם לא כישלון ללא שגיאה
            if (isSuccess && error != Error.None || !isSuccess && error == Error.None)
                throw new ArgumentException("שגיאה לא תקינה ביחס למצב ההצלחה", nameof(error));

            IsSuccess = isSuccess; // שמירת מצב ההצלחה
            Error = error;         // שמירת השגיאה
        }

        /// <summary>
        /// יוצר תוצאת הצלחה פשוטה ללא ערך מוחזר.
        /// </summary>
        public static Result Success() => new(true, Error.None);

        /// <summary>
        /// יוצר תוצאת כישלון עם השגיאה שגרמה לכישלון.
        /// </summary>
        /// <param name="error">השגיאה שגרמה לכישלון</param>
        public static Result Failure(Error error) => new(false, error);

        /// <summary>
        /// יוצר תוצאת הצלחה עם ערך מוחזר (גרסה גנרית).
        /// TValue = סוג הערך המוחזר (לדוגמה: AuthResponse, List&lt;AppointmentResponse&gt;).
        /// </summary>
        public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);

        /// <summary>
        /// יוצר תוצאת כישלון ללא ערך (גרסה גנרית).
        /// default! = ערך ברירת מחדל (null לסוגי Reference) שלא יגיש אותו.
        /// </summary>
        public static Result<TValue> Failure<TValue>(Error error) => new(default!, false, error);
    }

    /// <summary>
    /// גרסה גנרית של Result שמחזירה גם ערך בהצלחה.
    /// לדוגמה: Result&lt;AuthResponse&gt; מחזיר AuthResponse אם הצליח.
    /// יורשת מ-Result ומוסיפה את ה-Value.
    /// TValue = "פרמטר טיפוס" - פלייסהולדר לסוג הנתון האמיתי.
    /// </summary>
    public class Result<TValue> : Result
    {
        // שמירת הערך בשדה פרטי - ניתן לגישה רק דרך ה-Value property
        private readonly TValue? _value;

        /// <summary>
        /// קונסטרקטור - "internal" פירושו שנגיש רק מתוך הפרויקט הזה.
        /// </summary>
        protected internal Result(TValue? value, bool isSuccess, Error error) : base(isSuccess, error)
        {
            _value = value; // שמירת הערך
        }

        /// <summary>
        /// הערך המוחזר מהפעולה המוצלחת.
        /// זורק חריגה אם מנסים לגשת לערך כאשר הפעולה נכשלה.
        /// "!" אחרי _value = הצהרה שהערך בטוח ואינו null (כי אנחנו בודקים IsSuccess קודם).
        /// </summary>
        public TValue Value => IsSuccess
            ? _value!                                                            // אם הצליח - החזר את הערך
            : throw new InvalidOperationException("לא ניתן לגשת לערך של תוצאה שנכשלה."); // אם נכשל - זרוק חריגה

        /// <summary>
        /// המרה אוטומטית (Implicit Conversion) מ-TValue ל-Result&lt;TValue&gt;.
        /// מאפשרת לכתוב: "return myValue;" במקום "return Result.Success(myValue);".
        /// </summary>
        public static implicit operator Result<TValue>(TValue? value) => Success(value!);
    }
}
