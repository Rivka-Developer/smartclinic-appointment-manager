// =====================================
// קובץ: SwapOfferRepository.cs
// שכבה: Infrastructure → Repositories
// תפקיד: גישה לנתוני הצעות החלפת תורים.
// =====================================

using AppointmentManager.Domain;
using AppointmentManager.Domain.Entities;
using AppointmentManager.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace AppointmentManager.Infrastructure.Repositories
{
    public class SwapOfferRepository : ISwapOfferRepository
    {
        private readonly ApplicationDbContext _context;

        public SwapOfferRepository(ApplicationDbContext context) => _context = context;

        /// <summary>
        /// מחזיר הצעות Active שהתור שלהן יתחיל בין עכשיו לעוד 24 שעות, ממוינות לפי שעת התור.
        /// </summary>
        public async Task<IEnumerable<SwapOffer>> GetActiveOffersAsync()
        {
            var now = DateTime.UtcNow;
            var ceiling = now.AddHours(24);

            return await _context.SwapOffers
                .Where(o => o.Status == SwapOfferStatus.Active
                         && o.Appointment.StartTime > now
                         && o.Appointment.StartTime <= ceiling)
                .Include(o => o.Appointment)
                .Include(o => o.OfferedByClient)
                .OrderBy(o => o.Appointment.StartTime)
                .ToListAsync();
        }

        public async Task<SwapOffer?> GetByIdAsync(Guid id) =>
            await _context.SwapOffers
                .Include(o => o.Appointment)
                .Include(o => o.OfferedByClient)
                .FirstOrDefaultAsync(o => o.Id == id);

        public async Task<SwapOffer?> GetActiveOfferByAppointmentIdAsync(Guid appointmentId) =>
            await _context.SwapOffers
                .FirstOrDefaultAsync(o => o.AppointmentId == appointmentId
                                       && o.Status == SwapOfferStatus.Active);

        public async Task AddAsync(SwapOffer offer) =>
            await _context.SwapOffers.AddAsync(offer);

        public Task UpdateAsync(SwapOffer offer)
        {
            _context.SwapOffers.Update(offer);
            return Task.CompletedTask;
        }

        public async Task<IEnumerable<SwapOffer>> GetAllOffersAsync(SwapOfferStatus? status = null)
        {
            var query = _context.SwapOffers
                .Include(o => o.Appointment).ThenInclude(a => a.Client)
                .Include(o => o.OfferedByClient)
                .Include(o => o.AcceptedByClient)
                .AsQueryable();

            if (status.HasValue)
                query = query.Where(o => o.Status == status.Value);

            return await query
                .OrderBy(o => o.Status == SwapOfferStatus.Active ? 0 : 1)
                .ThenByDescending(o => o.Appointment.StartTime)
                .ToListAsync();
        }
    }
}
