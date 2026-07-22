using AppointmentManager.Application.DTOs;
using AppointmentManager.Domain;
using AppointmentManager.Domain.Common;

namespace AppointmentManager.Application.Interfaces
{
    public interface ISwapOfferService
    {
        Task<Result<List<SwapOfferResponse>>> GetActiveOffersAsync();
        Task<Result> CreateOfferAsync(Guid appointmentId, Guid clientId);
        Task<Result> AcceptOfferAsync(Guid offerId, Guid acceptingClientId);
        Task<Result> CancelOfferAsync(Guid offerId, Guid clientId);

        Task<Result<List<AdminSwapOfferResponse>>> GetAllOffersAdminAsync(SwapOfferStatus? status = null);
        Task<Result> AdminCreateOfferAsync(Guid appointmentId);
        Task<Result> AdminAcceptOfferAsync(Guid offerId, Guid targetClientId);
        Task<Result> AdminCancelOfferAsync(Guid offerId);
    }
}
