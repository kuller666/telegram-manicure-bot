using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using TelegramManicureBot.Common;
using TelegramManicureBot.Data;
using TelegramManicureBot.Models;

namespace TelegramManicureBot.Services.Notifications;

/// <inheritdoc />
public sealed class MasterNotificationService(
    ITelegramBotClient botClient,
    AppDbContext db,
    IOptions<TelegramBotOptions> options,
    ILogger<MasterNotificationService> logger) : IMasterNotificationService
{
    /// <inheritdoc />
    public async Task NotifyNewAppointmentAsync(
        Appointment appointment,
        long clientTelegramUserId,
        string? clientDisplayName,
        CancellationToken ct = default)
    {
        var cfg = options.Value;
        if (!long.TryParse(cfg.MasterChatId, out var masterChatId) || masterChatId == 0)
        {
            logger.LogWarning("MasterChatId не задан — уведомление мастеру пропущено.");
            return;
        }

        var localStart = TimeZoneHelper.ToSalonLocal(appointment.StartUtc);
        var serviceName = await db.SalonServices.AsNoTracking()
            .Where(s => s.Id == appointment.SalonServiceId)
            .Select(s => s.Name)
            .FirstAsync(ct);

        var text =
            $"🔔 <b>Новая запись</b>\n" +
            $"💅 Услуга: {TelegramHtml.Escape(serviceName)}\n" +
            $"📅 {localStart:dd.MM.yyyy HH:mm}\n" +
            $"👤 Клиент: {TelegramHtml.Escape(clientDisplayName ?? "без имени")}\n" +
            $"🆔 Telegram id: <code>{clientTelegramUserId}</code>";

        try
        {
            await botClient.SendMessage(
                chatId: masterChatId,
                text: text,
                parseMode: ParseMode.Html,
                cancellationToken: ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Не удалось отправить уведомление мастеру.");
        }
    }
}
