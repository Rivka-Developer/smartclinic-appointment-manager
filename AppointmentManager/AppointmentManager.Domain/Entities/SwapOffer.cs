// =====================================
// קובץ: SwapOffer.cs
// שכבה: Domain → Entities
// תפקיד: מייצג הצעת החלפת תור בלוח ההעברה.
//         כאשר לקוחה אינה יכולה לבטל תור (פחות מ-24 שעות), היא יכולה
//         להציע אותו בלוח ולקוחה אחרת יכולה לקבל את הבעלות עליו.
// =====================================

namespace AppointmentManager.Domain.Entities
{
    public class SwapOffer
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>התור המוצע להעברה.</summary>
        public Guid AppointmentId { get; set; }
        public Appointment Appointment { get; set; } = null!;

        /// <summary>הלקוחה שמציעה את התור.</summary>
        public Guid OfferedByClientId { get; set; }
        public User OfferedByClient { get; set; } = null!;

        public SwapOfferStatus Status { get; set; } = SwapOfferStatus.Active;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>מולא בקבלת ההצעה.</summary>
        public DateTime? AcceptedAt { get; set; }
        public Guid? AcceptedByClientId { get; set; }
        public User? AcceptedByClient { get; set; }
    }
}
