// =====================================
// קובץ: ISwapOfferRepository.cs
// שכבה: Domain → Interfaces
// תפקיד: חוזה לגישה לנתוני הצעות החלפת תורים.
// =====================================

using AppointmentManager.Domain.Entities;
using AppointmentManager.Domain;

namespace AppointmentManager.Domain.Interfaces
{
    public interface ISwapOfferRepository
    {
        /// <summary>
        /// מחזיר הצעות Active שהתור שלהן מתחיל בין עכשיו לעוד 24 שעות.
        /// </summary>
        Task<IEnumerable<SwapOffer>> GetActiveOffersAsync();

        Task<SwapOffer?> GetByIdAsync(Guid id);

        /// <summary>בודק אם יש הצעה Active על תור מסוים.</summary>
        Task<SwapOffer?> GetActiveOfferByAppointmentIdAsync(Guid appointmentId);

        Task AddAsync(SwapOffer offer);
        Task UpdateAsync(SwapOffer offer);

        /// <summary>ללא סינון זמן, עם סינון סטטוס אופציונלי — לשימוש מנהלת בלבד.</summary>
        Task<IEnumerable<SwapOffer>> GetAllOffersAsync(SwapOfferStatus? status = null);
    }
}
