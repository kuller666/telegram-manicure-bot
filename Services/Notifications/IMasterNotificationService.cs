using TelegramManicureBot.Models;

namespace TelegramManicureBot.Services.Notifications;

/// <summary>Уведомление мастера о событиях записи.</summary>
public interface IMasterNotificationService
{
    Task NotifyNewAppointmentAsync(
        Appointment appointment,
        long clientTelegramUserId,
        string? clientDisplayName,
        CancellationToken ct = default);
}
