using TelegramManicureBot.Models;

namespace TelegramManicureBot.Services.Booking;

/// <summary>Создание и отмена записей, выборки для клиента.</summary>
public interface IBookingService
{
    Task<Appointment?> CreateAsync(
        int appUserId,
        int salonServiceId,
        DateTime startUtc,
        CancellationToken ct = default);

    Task<bool> CancelOwnAppointmentAsync(long telegramUserId, int appointmentId, CancellationToken ct = default);

    Task<IReadOnlyList<Appointment>> GetUpcomingForTelegramUserAsync(long telegramUserId, CancellationToken ct = default);
}
