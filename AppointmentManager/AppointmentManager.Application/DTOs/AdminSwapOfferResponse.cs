using AppointmentManager.Domain;

namespace AppointmentManager.Application.DTOs
{
    public record AdminSwapOfferResponse(
        Guid Id,
        Guid AppointmentId,
        DateTime AppointmentStartTime,
        int AppointmentDurationMinutes,
        string OfferedByName,
        string OfferedByEmail,
        string CurrentOwnerName,
        SwapOfferStatus Status,
        DateTime CreatedAt,
        DateTime? AcceptedAt,
        string? AcceptedByName
    );
}
