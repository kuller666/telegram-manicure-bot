using TelegramManicureBot.Models;

namespace TelegramManicureBot.Services.Booking;

/// <summary>
/// Расчёт свободных слотов на дату с учётом длительности услуги и уже занятых записей.
/// </summary>
public interface IAvailabilityService
{
    /// <summary>Список начал свободных слотов (UTC) на указанную локальную дату салона.</summary>
    Task<IReadOnlyList<TimeSlot>> GetAvailableSlotsAsync(
        DateOnly salonLocalDate,
        int serviceDurationMinutes,
        CancellationToken ct = default);
}
