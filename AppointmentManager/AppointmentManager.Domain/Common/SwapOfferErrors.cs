// =====================================
// קובץ: SwapOfferErrors.cs
// שכבה: Domain → Common
// תפקיד: שגיאות מוגדרות מראש עבור פעולות לוח ההעברה.
// =====================================

namespace AppointmentManager.Domain.Common
{
    public static class SwapOfferErrors
    {
        public static Error NotFound =>
            Error.NotFound("SwapOffer.NotFound", "ההצעה המבוקשת לא נמצאה.");

        public static Error AlreadyOffered =>
            Error.Conflict("SwapOffer.AlreadyOffered", "תור זה כבר מוצע להעברה.");

        public static Error NotYourOffer =>
            Error.Unauthorized("SwapOffer.NotYourOffer", "אין לך הרשאה לבטל הצעה זו.");

        public static Error OfferNotActive =>
            Error.Conflict("SwapOffer.OfferNotActive", "ההצעה כבר אינה פעילה.");

        public static Error CannotAcceptOwn =>
            Error.Validation("SwapOffer.CannotAcceptOwn", "לא ניתן לקבל תור שהצעת בעצמך.");

        public static Error AppointmentExpired =>
            Error.Validation("SwapOffer.AppointmentExpired", "זמן התור עבר ולא ניתן להעבירו.");

        public static Error NotWithin24Hours =>
            Error.Validation("SwapOffer.NotWithin24Hours", "ניתן להציע להעברה רק תורים שבמסגרת 24 השעות הקרובות.");

        public static Error AppointmentNotFound =>
            Error.NotFound("SwapOffer.AppointmentNotFound", "התור המבוקש לא נמצא.");

        public static Error NotYourAppointment =>
            Error.Unauthorized("SwapOffer.NotYourAppointment", "תור זה אינו שלך.");

        public static Error TargetClientNotFound =>
            Error.NotFound("SwapOffer.TargetClientNotFound", "לקוחה היעד לא נמצאה.");
    }
}
