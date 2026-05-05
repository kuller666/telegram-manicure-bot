using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using TelegramManicureBot.Common;
using TelegramManicureBot.Data;
using TelegramManicureBot.Models;

namespace TelegramManicureBot.Services.Reminders;

/// <summary>
/// Фоновый цикл: раз в минуту проверяет активные записи и шлёт напоминания клиенту в Telegram.
/// Интервалы «за 24 часа» и «за 2 часа» считаются относительно времени начала записи (UTC).
/// </summary>
public sealed class AppointmentReminderHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<AppointmentReminderHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Небольшая задержка при старте API, чтобы успела подняться БД.
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var bot = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();

                await TickAsync(db, bot, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка в цикле напоминаний.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private static async Task TickAsync(AppDbContext db, ITelegramBotClient bot, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        // Берём только ближайшие активные записи с незакрытыми флагами напоминаний.
        var candidates = await db.Appointments
            .Include(a => a.TelegramUser)
            .Include(a => a.SalonService)
            .Where(a => a.Status == AppointmentStatus.Active && a.StartUtc > now)
            .Where(a => !a.Reminder24hSent || !a.Reminder2hSent)
            .ToListAsync(ct);

        foreach (var a in candidates)
        {
            var chatId = a.TelegramUser.TelegramUserId;

            // Окно «за сутки»: уже прошёл момент (start - 24h), но визит ещё впереди.
            if (!a.Reminder24hSent &&
                now >= a.StartUtc.AddHours(-24) &&
                now < a.StartUtc)
            {
                await SendAsync(bot, chatId, FormatReminder(a, "24 часа"), ct);
                a.Reminder24hSent = true;
            }

            // «За 2 часа»
            if (!a.Reminder2hSent &&
                now >= a.StartUtc.AddHours(-2) &&
                now < a.StartUtc)
            {
                await SendAsync(bot, chatId, FormatReminder(a, "2 часа"), ct);
                a.Reminder2hSent = true;
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private static string FormatReminder(Appointment a, string whenLabel)
    {
        var local = TimeZoneHelper.ToSalonLocal(a.StartUtc);
        return
            $"⏰ Напоминание ({whenLabel} до визита)\n" +
            $"💅 {TelegramHtml.Escape(a.SalonService.Name)}\n" +
            $"📅 {local:dd.MM.yyyy HH:mm}\n\n" +
            "Если планы изменились — отмените запись в меню бота.";
    }

    private static async Task SendAsync(ITelegramBotClient bot, long chatId, string text, CancellationToken ct)
    {
        await bot.SendMessage(
            chatId: chatId,
            text: text,
            parseMode: ParseMode.Html,
            cancellationToken: ct);
    }
}
