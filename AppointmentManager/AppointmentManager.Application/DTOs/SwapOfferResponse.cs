using AppointmentManager.Domain;

namespace AppointmentManager.Application.DTOs
{
    public record SwapOfferResponse(
        Guid Id,
        Guid AppointmentId,
        DateTime AppointmentStartTime,
        int AppointmentDurationMinutes,
        string OfferedByName,
        SwapOfferStatus Status,
        DateTime CreatedAt
    );
}
