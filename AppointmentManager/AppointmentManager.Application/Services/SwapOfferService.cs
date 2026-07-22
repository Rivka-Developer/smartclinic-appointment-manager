// =====================================
// קובץ: SwapOfferService.cs
// שכבה: Application → Services
// תפקיד: לוגיקה עסקית של לוח העברת תורים.
//         מאפשר ללקוחות להציע תורים שלא ניתן לבטל ולהעביר בעלות.
// =====================================

using AppointmentManager.Application.DTOs;
using AppointmentManager.Application.Helpers;
using AppointmentManager.Application.Interfaces;
using AppointmentManager.Domain;
using AppointmentManager.Domain.Common;
using AppointmentManager.Domain.Entities;
using AppointmentManager.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Data;

namespace AppointmentManager.Application.Services;

public class SwapOfferService(
    IUnitOfWork uow,
    IEmailService emailService,
    ILogger<SwapOfferService> logger)
    : ISwapOfferService
{
    /// <summary>
    /// מחזיר הצעות פעילות בלוח — תורים שיתחילו בין עכשיו לעוד 24 שעות.
    /// </summary>
    public async Task<Result<List<SwapOfferResponse>>> GetActiveOffersAsync()
    {
        var offers = await uow.SwapOffers.GetActiveOffersAsync();
        var response = offers.Select(o => new SwapOfferResponse(
            o.Id,
            o.AppointmentId,
            o.Appointment.StartTime,
            o.Appointment.DurationMinutes,
            o.OfferedByClient.FullName,
            o.Status,
            o.CreatedAt
        )).ToList();
        return Result.Success(response);
    }

    /// <summary>
    /// יוצר הצעת העברה. מותר רק לתורים שבמסגרת 24 השעות הקרובות.
    /// </summary>
    public async Task<Result> CreateOfferAsync(Guid appointmentId, Guid clientId)
    {
        var appointment = await uow.Appointments.GetByIdAsync(appointmentId);
        if (appointment == null)
            return Result.Failure(SwapOfferErrors.AppointmentNotFound);

        if (appointment.ClientId != clientId)
            return Result.Failure(SwapOfferErrors.NotYourAppointment);

        if (appointment.Status != AppointmentStatus.Scheduled)
            return Result.Failure(SwapOfferErrors.AppointmentExpired);

        var now = DateTime.UtcNow;
        if (appointment.StartTime <= now)
            return Result.Failure(SwapOfferErrors.AppointmentExpired);

        if (appointment.StartTime > now.AddHours(24))
            return Result.Failure(SwapOfferErrors.NotWithin24Hours);

        var existing = await uow.SwapOffers.GetActiveOfferByAppointmentIdAsync(appointmentId);
        if (existing != null)
            return Result.Failure(SwapOfferErrors.AlreadyOffered);

        var offer = new SwapOffer
        {
            AppointmentId      = appointmentId,
            OfferedByClientId  = clientId
        };

        await uow.SwapOffers.AddAsync(offer);
        await uow.SaveChangesAsync();

        logger.LogInformation("הצעת החלפה נוצרה — תור {AppointmentId} ע\"י לקוח {ClientId}", appointmentId, clientId);
        return Result.Success();
    }

    /// <summary>
    /// מקבל הצעה — מעביר בעלות על התור. טרנזקציה עם RepeatableRead נגד race condition.
    /// </summary>
    public async Task<Result> AcceptOfferAsync(Guid offerId, Guid acceptingClientId)
    {
        await uow.BeginTransactionAsync(IsolationLevel.RepeatableRead);
        try
        {
            var offer = await uow.SwapOffers.GetByIdAsync(offerId);
            if (offer == null)
            {
                await uow.RollbackAsync();
                return Result.Failure(SwapOfferErrors.NotFound);
            }

            if (offer.Status != SwapOfferStatus.Active)
            {
                await uow.RollbackAsync();
                return Result.Failure(SwapOfferErrors.OfferNotActive);
            }

            if (offer.OfferedByClientId == acceptingClientId)
            {
                await uow.RollbackAsync();
                return Result.Failure(SwapOfferErrors.CannotAcceptOwn);
            }

            var appointment = await uow.Appointments.GetByIdAsync(offer.AppointmentId);
            if (appointment == null || appointment.StartTime <= DateTime.UtcNow)
            {
                await uow.RollbackAsync();
                return Result.Failure(SwapOfferErrors.AppointmentExpired);
            }

            // העברת בעלות
            var previousClientId = appointment.ClientId;
            appointment.ClientId = acceptingClientId;
            await uow.Appointments.UpdateAsync(appointment);

            offer.Status              = SwapOfferStatus.Accepted;
            offer.AcceptedByClientId  = acceptingClientId;
            offer.AcceptedAt          = DateTime.UtcNow;
            await uow.SwapOffers.UpdateAsync(offer);

            // SaveChangesAsync יזרוק ConcurrencyConflictException אם שני לקוחות ניסו בו-זמנית
            await uow.SaveChangesAsync();
            await uow.CommitAsync();

            logger.LogInformation("הצעת החלפה התקבלה — הצעה {OfferId}, תור עבר מ-{From} ל-{To}",
                offerId, previousClientId, acceptingClientId);

            // שליחת אימיילים (fire-and-forget מחוץ לטרנזקציה)
            await TrySendSwapEmailsAsync(appointment, offer.OfferedByClientId, acceptingClientId);

            return Result.Success();
        }
        catch (ConcurrencyConflictException)
        {
            await uow.RollbackAsync();
            logger.LogWarning("race condition בקבלת הצעת החלפה {OfferId}", offerId);
            return Result.Failure(SwapOfferErrors.OfferNotActive);
        }
        catch (Exception ex)
        {
            await uow.RollbackAsync();
            logger.LogError(ex, "שגיאה בקבלת הצעת החלפה {OfferId}", offerId);
            throw;
        }
    }

    /// <summary>
    /// מבטל הצעה — רק המציעה יכולה לבטל, והתור נשאר שלה.
    /// </summary>
    public async Task<Result> CancelOfferAsync(Guid offerId, Guid clientId)
    {
        var offer = await uow.SwapOffers.GetByIdAsync(offerId);
        if (offer == null)
            return Result.Failure(SwapOfferErrors.NotFound);

        if (offer.OfferedByClientId != clientId)
            return Result.Failure(SwapOfferErrors.NotYourOffer);

        if (offer.Status != SwapOfferStatus.Active)
            return Result.Failure(SwapOfferErrors.OfferNotActive);

        offer.Status = SwapOfferStatus.Cancelled;
        await uow.SwapOffers.UpdateAsync(offer);
        await uow.SaveChangesAsync();

        logger.LogInformation("הצעת החלפה בוטלה — הצעה {OfferId} ע\"י לקוח {ClientId}", offerId, clientId);
        return Result.Success();
    }

    // ── פעולות מנהלת ─────────────────────────────────────────────────────────

    public async Task<Result<List<AdminSwapOfferResponse>>> GetAllOffersAdminAsync(SwapOfferStatus? status = null)
    {
        var offers = await uow.SwapOffers.GetAllOffersAsync(status);
        var response = offers.Select(o => new AdminSwapOfferResponse(
            o.Id,
            o.AppointmentId,
            o.Appointment.StartTime,
            o.Appointment.DurationMinutes,
            o.OfferedByClient.FullName,
            o.OfferedByClient.Email ?? string.Empty,
            o.Appointment.Client.FullName,
            o.Status,
            o.CreatedAt,
            o.AcceptedAt,
            o.AcceptedByClient?.FullName
        )).ToList();
        return Result.Success(response);
    }

    public async Task<Result> AdminCreateOfferAsync(Guid appointmentId)
    {
        var appointment = await uow.Appointments.GetByIdAsync(appointmentId);
        if (appointment == null)
            return Result.Failure(SwapOfferErrors.AppointmentNotFound);

        if (appointment.Status != AppointmentStatus.Scheduled)
            return Result.Failure(SwapOfferErrors.AppointmentExpired);

        var now = DateTime.UtcNow;
        if (appointment.StartTime <= now)
            return Result.Failure(SwapOfferErrors.AppointmentExpired);

        if (appointment.StartTime > now.AddHours(24))
            return Result.Failure(SwapOfferErrors.NotWithin24Hours);

        var existing = await uow.SwapOffers.GetActiveOfferByAppointmentIdAsync(appointmentId);
        if (existing != null)
            return Result.Failure(SwapOfferErrors.AlreadyOffered);

        var offer = new SwapOffer
        {
            AppointmentId     = appointmentId,
            OfferedByClientId = appointment.ClientId
        };
        await uow.SwapOffers.AddAsync(offer);
        await uow.SaveChangesAsync();

        logger.LogInformation("מנהלת פרסמה הצעה עבור תור {AppointmentId}", appointmentId);
        return Result.Success();
    }

    public async Task<Result> AdminAcceptOfferAsync(Guid offerId, Guid targetClientId)
    {
        var targetClient = await uow.Users.GetByIdAsync(targetClientId);
        if (targetClient == null)
            return Result.Failure(SwapOfferErrors.TargetClientNotFound);

        await uow.BeginTransactionAsync(IsolationLevel.RepeatableRead);
        try
        {
            var offer = await uow.SwapOffers.GetByIdAsync(offerId);
            if (offer == null) { await uow.RollbackAsync(); return Result.Failure(SwapOfferErrors.NotFound); }
            if (offer.Status != SwapOfferStatus.Active) { await uow.RollbackAsync(); return Result.Failure(SwapOfferErrors.OfferNotActive); }

            var appointment = await uow.Appointments.GetByIdAsync(offer.AppointmentId);
            if (appointment == null || appointment.StartTime <= DateTime.UtcNow)
            {
                await uow.RollbackAsync();
                return Result.Failure(SwapOfferErrors.AppointmentExpired);
            }

            var previousClientId  = appointment.ClientId;
            appointment.ClientId  = targetClientId;
            await uow.Appointments.UpdateAsync(appointment);

            offer.Status             = SwapOfferStatus.Accepted;
            offer.AcceptedByClientId = targetClientId;
            offer.AcceptedAt         = DateTime.UtcNow;
            await uow.SwapOffers.UpdateAsync(offer);

            await uow.SaveChangesAsync();
            await uow.CommitAsync();

            logger.LogInformation("מנהלת קיבלה הצעה {OfferId} עבור לקוח {TargetClientId}", offerId, targetClientId);

            await TrySendSwapEmailsAsync(appointment, offer.OfferedByClientId, targetClientId);
            return Result.Success();
        }
        catch (ConcurrencyConflictException)
        {
            await uow.RollbackAsync();
            logger.LogWarning("race condition בקבלת הצעה {OfferId} ע\"י מנהלת", offerId);
            return Result.Failure(SwapOfferErrors.OfferNotActive);
        }
        catch (Exception ex)
        {
            await uow.RollbackAsync();
            logger.LogError(ex, "שגיאה בקבלת הצעה {OfferId} ע\"י מנהלת", offerId);
            throw;
        }
    }

    public async Task<Result> AdminCancelOfferAsync(Guid offerId)
    {
        var offer = await uow.SwapOffers.GetByIdAsync(offerId);
        if (offer == null)
            return Result.Failure(SwapOfferErrors.NotFound);
        if (offer.Status != SwapOfferStatus.Active)
            return Result.Failure(SwapOfferErrors.OfferNotActive);

        offer.Status = SwapOfferStatus.Cancelled;
        await uow.SwapOffers.UpdateAsync(offer);
        await uow.SaveChangesAsync();

        logger.LogInformation("מנהלת ביטלה הצעה {OfferId}", offerId);
        return Result.Success();
    }

    // ── עזרים פרטיים ─────────────────────────────────────────────────────────

    private async Task TrySendSwapEmailsAsync(Appointment appointment, Guid offererClientId, Guid acceptorClientId)
    {
        try
        {
            var settings = await uow.Settings.GetSettingsAsync();
            string businessName = settings?.BusinessName ?? "SmartClinic";

            var offerer = await uow.Users.GetByIdAsync(offererClientId);
            var acceptor = await uow.Users.GetByIdAsync(acceptorClientId);

            if (offerer != null && offerer.Role != UserRole.ManagedClient && !string.IsNullOrEmpty(offerer.Email))
            {
                string bodyOfferer = EmailTemplates.SwapTransferredAway(offerer.FullName, appointment.StartTime, businessName);
                await emailService.SendEmailAsync(offerer.Email, "העברת תור בהצלחה - SmartClinic", bodyOfferer);
            }

            if (acceptor != null && acceptor.Role != UserRole.ManagedClient && !string.IsNullOrEmpty(acceptor.Email))
            {
                string bodyAcceptor = EmailTemplates.BookingConfirmation(acceptor.FullName, appointment.StartTime, appointment.DurationMinutes, businessName);
                await emailService.SendEmailAsync(acceptor.Email, "אישור קביעת תור - SmartClinic", bodyAcceptor);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "שגיאה בשליחת אימיילי החלפת תור {AppointmentId}", appointment.Id);
        }
    }
}
