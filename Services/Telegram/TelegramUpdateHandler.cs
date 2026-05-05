using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramManicureBot.Common;
using TelegramManicureBot.Data;
using TelegramManicureBot.Services.Booking;
using TelegramManicureBot.Services.Conversation;
using TelegramManicureBot.Services.Notifications;
using TelegramManicureBot.Services.Users;

namespace TelegramManicureBot.Services.Telegram;

/// <inheritdoc />
/// <remarks>
/// Здесь сосредоточена вся «линейная» логика диалога: главное меню, inline-мастер записи,
/// списки записей и отмена. Для роста проекта этот класс можно разбить на несколько хендлеров по типам апдейтов.
/// </remarks>
public sealed class TelegramUpdateHandler(
    ITelegramBotClient bot,
    AppDbContext db,
    IAppUserService users,
    IBookingService booking,
    IAvailabilityService availability,
    BookingDraftStore drafts,
    IMasterNotificationService masterNotifications,
    IOptions<SalonScheduleOptions> salonOptions,
    ILogger<TelegramUpdateHandler> logger) : ITelegramUpdateHandler
{
    /// <inheritdoc />
    public async Task HandleAsync(Update update, CancellationToken ct = default)
    {
        try
        {
            switch (update.Type)
            {
                case UpdateType.Message when update.Message is { } msg:
                    await OnMessageAsync(msg, ct);
                    break;
                case UpdateType.CallbackQuery when update.CallbackQuery is { } cq:
                    await OnCallbackAsync(cq, ct);
                    break;
                default:
                    // Игнорируем прочие типы (edited_message и т.д.) в минимальной версии.
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при обработке апдейта Telegram.");
        }
    }

    private async Task OnMessageAsync(Message message, CancellationToken ct)
    {
        if (message.From is null || message.Chat.Id == default)
            return;

        var tgUserId = message.From.Id;
        var text = message.Text?.Trim() ?? string.Empty;

        // Регистрируем пользователя при любом сообщении (или только на /start — здесь при каждом входе для простоты).
        var (appUser, _) = await users.GetOrCreateAsync(
            tgUserId,
            message.From.Username,
            message.From.FirstName,
            message.From.LastName,
            ct);

        if (text.StartsWith("/start", StringComparison.OrdinalIgnoreCase))
        {
            await bot.SendMessage(
                chatId: message.Chat.Id,
                text:
                "Привет! 👋 Я помогу записаться на маникюр.\n\n" +
                "Выберите действие в меню ниже или следуйте подсказкам после нажатия «📅 Записаться».",
                replyMarkup: MainMenuKeyboard(),
                cancellationToken: ct);
            drafts.Reset(tgUserId);
            return;
        }

        switch (text)
        {
            case UiTexts.BtnServices:
                await SendServicesOverviewAsync(message.Chat.Id, ct);
                return;
            case UiTexts.BtnBook:
                await StartBookingWizardAsync(message.Chat.Id, tgUserId, ct);
                return;
            case UiTexts.BtnMyAppointments:
            case UiTexts.BtnCancelAppointment:
                await SendMyAppointmentsAsync(message.Chat.Id, tgUserId, ct);
                return;
            case UiTexts.BtnHelp:
                await bot.SendMessage(
                    chatId: message.Chat.Id,
                    text:
                    "ℹ️ <b>Помощь</b>\n" +
                    "• «Услуги» — прайс и описание.\n" +
                    "• «Записаться» — пошаговый выбор услуги, даты и времени.\n" +
                    "• «Мои записи» — список предстоящих визитов.\n" +
                    "• «Отмена записи» — выберите запись для отмены.\n\n" +
                    "Если что-то пошло не так — напишите мастеру напрямую.",
                    parseMode: ParseMode.Html,
                    cancellationToken: ct);
                return;
        }

        // Неизвестный текст — мягкая подсказка.
        await bot.SendMessage(
            chatId: message.Chat.Id,
            text: "Я пока не понимаю эту фразу 🤔 Используйте кнопки меню ниже.",
            replyMarkup: MainMenuKeyboard(),
            cancellationToken: ct);
    }

    private async Task OnCallbackAsync(CallbackQuery cq, CancellationToken ct)
    {
        if (cq.From is null || cq.Message is null || cq.Data is null)
            return;

        var tgUserId = cq.From.Id;
        var chatId = cq.Message.Chat.Id;

        await bot.AnswerCallbackQuery(callbackQueryId: cq.Id, cancellationToken: ct);

        await users.GetOrCreateAsync(tgUserId, cq.From.Username, cq.From.FirstName, cq.From.LastName, ct);

        var parts = cq.Data.Split(':', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
            return;

        var prefix = parts[0];
        var payload = parts[1];

        switch (prefix)
        {
            case CallbackPrefixes.Service when int.TryParse(payload, out var serviceId):
                await OnServiceSelectedAsync(chatId, tgUserId, serviceId, ct);
                break;
            case CallbackPrefixes.Date when DateOnly.TryParse(payload, out var date):
                await OnDateSelectedAsync(chatId, tgUserId, date, ct);
                break;
            case CallbackPrefixes.Time:
                await OnTimeSelectedAsync(chatId, tgUserId, payload, cq.From, ct);
                break;
            case CallbackPrefixes.CancelAppointment when int.TryParse(payload, out var appointmentId):
                await OnCancelAppointmentAsync(chatId, tgUserId, appointmentId, ct);
                break;
            case CallbackPrefixes.RefreshMyAppointments:
                await SendMyAppointmentsAsync(chatId, tgUserId, ct);
                break;
        }
    }

    private async Task SendServicesOverviewAsync(ChatId chatId, CancellationToken ct)
    {
        var list = await db.SalonServices.AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.Id)
            .ToListAsync(ct);

        if (list.Count == 0)
        {
            await bot.SendMessage(chatId, "Пока нет доступных услуг.", cancellationToken: ct);
            return;
        }

        var lines = list.Select(s =>
            $"💅 <b>{TelegramHtml.Escape(s.Name)}</b>\n" +
            $"{TelegramHtml.Escape(s.Description ?? "")}\n" +
            $"⏱ {s.DurationMinutes} мин · 💰 {s.PriceRub} ₽");

        var text = "✨ <b>Наши услуги</b>\n\n" + string.Join("\n\n", lines);

        await bot.SendMessage(
            chatId,
            text,
            parseMode: ParseMode.Html,
            cancellationToken: ct);
    }

    private async Task StartBookingWizardAsync(ChatId chatId, long tgUserId, CancellationToken ct)
    {
        drafts.Reset(tgUserId);
        var d = drafts.GetOrCreate(tgUserId);
        d.Step = BookingWizardStep.WaitingServicePick;

        var services = await db.SalonServices.AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.Id)
            .ToListAsync(ct);

        if (services.Count == 0)
        {
            await bot.SendMessage(chatId, "Услуги временно недоступны.", cancellationToken: ct);
            return;
        }

        var rows = services.Select(s =>
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    $"{s.Name} · {s.PriceRub} ₽",
                    $"{CallbackPrefixes.Service}:{s.Id}")
            });

        await bot.SendMessage(
            chatId,
            "📌 Шаг 1 из 3\nВыберите услугу:",
            replyMarkup: new InlineKeyboardMarkup(rows),
            cancellationToken: ct);
    }

    private async Task OnServiceSelectedAsync(ChatId chatId, long tgUserId, int serviceId, CancellationToken ct)
    {
        var exists = await db.SalonServices.AsNoTracking()
            .AnyAsync(s => s.Id == serviceId && s.IsActive, ct);
        if (!exists)
        {
            await bot.SendMessage(chatId, "Эта услуга недоступна.", cancellationToken: ct);
            return;
        }

        var d = drafts.GetOrCreate(tgUserId);
        d.ServiceId = serviceId;
        d.Step = BookingWizardStep.WaitingDatePick;

        var keyboard = BuildDateKeyboard(salonOptions.Value);

        await bot.SendMessage(
            chatId,
            "📌 Шаг 2 из 3\nВыберите дату:",
            replyMarkup: keyboard,
            cancellationToken: ct);
    }

    private InlineKeyboardMarkup BuildDateKeyboard(SalonScheduleOptions opt)
    {
        var tz = TimeZoneHelper.Moscow;
        var todayLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz).Date;
        var dates = Enumerable.Range(0, Math.Max(1, opt.BookingDaysAhead))
            .Select(i => DateOnly.FromDateTime(todayLocal.AddDays(i)))
            .ToList();

        // По 3 кнопки в ряд — компактно на телефоне.
        var rows = new List<IEnumerable<InlineKeyboardButton>>();
        foreach (var chunk in dates.Chunk(3))
        {
            rows.Add(chunk.Select(d =>
                InlineKeyboardButton.WithCallbackData(
                    d.ToString("dd.MM"),
                    $"{CallbackPrefixes.Date}:{d:yyyy-MM-dd}")));
        }

        return new InlineKeyboardMarkup(rows);
    }

    private async Task OnDateSelectedAsync(ChatId chatId, long tgUserId, DateOnly date, CancellationToken ct)
    {
        var d = drafts.GetOrCreate(tgUserId);
        if (d.ServiceId is null)
        {
            await bot.SendMessage(chatId, "Сначала выберите услугу через «📅 Записаться».", cancellationToken: ct);
            return;
        }

        var duration = await db.SalonServices.AsNoTracking()
            .Where(s => s.Id == d.ServiceId)
            .Select(s => s.DurationMinutes)
            .FirstAsync(ct);

        var slots = await availability.GetAvailableSlotsAsync(date, duration, ct);
        if (slots.Count == 0)
        {
            await bot.SendMessage(
                chatId,
                "😔 На выбранную дату нет свободных окон. Попробуйте другую дату.",
                cancellationToken: ct);
            return;
        }

        d.Date = date;
        d.Step = BookingWizardStep.WaitingTimePick;

        var rows = slots
            .Chunk(4)
            .Select(chunk => chunk.Select(s =>
                    InlineKeyboardButton.WithCallbackData(
                        s.FormatLocalLabel(),
                        $"{CallbackPrefixes.Time}:{s.StartUtc:O}")
                )
                .ToArray());

        await bot.SendMessage(
            chatId,
            $"📌 Шаг 3 из 3\nДата: <b>{date:dd.MM.yyyy}</b>\nВыберите время:",
            parseMode: ParseMode.Html,
            replyMarkup: new InlineKeyboardMarkup(rows),
            cancellationToken: ct);
    }

    private async Task OnTimeSelectedAsync(ChatId chatId, long tgUserId, string isoUtc, User from, CancellationToken ct)
    {
        var d = drafts.GetOrCreate(tgUserId);
        if (d.ServiceId is null || d.Date is null)
        {
            await bot.SendMessage(chatId, "Начните запись заново: «📅 Записаться».", cancellationToken: ct);
            return;
        }

        if (!DateTime.TryParse(isoUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out var startUtc))
        {
            await bot.SendMessage(chatId, "Не удалось разобрать время. Попробуйте снова.", cancellationToken: ct);
            return;
        }

        startUtc = DateTime.SpecifyKind(startUtc, DateTimeKind.Utc);

        var (appUser, _) = await users.GetOrCreateAsync(tgUserId, from.Username, from.FirstName, from.LastName, ct);

        var created = await booking.CreateAsync(appUser.Id, d.ServiceId.Value, startUtc, ct);
        if (created is null)
        {
            await bot.SendMessage(
                chatId,
                "⚠️ Это время только что заняли. Выберите другое или другую дату.",
                cancellationToken: ct);
            return;
        }

        drafts.Reset(tgUserId);

        var local = TimeZoneHelper.ToSalonLocal(created.StartUtc);
        var displayName = string.Join(' ', new[] { from.FirstName, from.LastName }.Where(x => !string.IsNullOrWhiteSpace(x)));

        await masterNotifications.NotifyNewAppointmentAsync(created, tgUserId, displayName, ct);

        await bot.SendMessage(
            chatId,
            "✅ Запись создана!\n" +
            $"💅 Услуга зафиксирована.\n" +
            $"📅 {local:dd.MM.yyyy HH:mm}\n\n" +
            "Мы напомним за сутки и за 2 часа до визита ⏰",
            replyMarkup: MainMenuKeyboard(),
            cancellationToken: ct);
    }

    private async Task SendMyAppointmentsAsync(ChatId chatId, long tgUserId, CancellationToken ct)
    {
        var items = await booking.GetUpcomingForTelegramUserAsync(tgUserId, ct);
        if (items.Count == 0)
        {
            await bot.SendMessage(chatId, "У вас пока нет активных предстоящих записей.", cancellationToken: ct);
            return;
        }

        var lines = items.Select(a =>
        {
            var local = TimeZoneHelper.ToSalonLocal(a.StartUtc);
            return $"• #{a.Id} · {TelegramHtml.Escape(a.SalonService.Name)} · {local:dd.MM HH:mm}";
        });

        var keyboardRows = items.Select(a => new[]
        {
            InlineKeyboardButton.WithCallbackData(
                $"❌ Отменить #{a.Id}",
                $"{CallbackPrefixes.CancelAppointment}:{a.Id}")
        });

        keyboardRows = keyboardRows.Append(new[]
        {
            InlineKeyboardButton.WithCallbackData("🔄 Обновить список", $"{CallbackPrefixes.RefreshMyAppointments}:1")
        });

        await bot.SendMessage(
            chatId,
            "📋 <b>Ваши записи</b>\n\n" + string.Join('\n', lines),
            parseMode: ParseMode.Html,
            replyMarkup: new InlineKeyboardMarkup(keyboardRows),
            cancellationToken: ct);
    }

    private async Task OnCancelAppointmentAsync(ChatId chatId, long tgUserId, int appointmentId, CancellationToken ct)
    {
        var ok = await booking.CancelOwnAppointmentAsync(tgUserId, appointmentId, ct);
        if (!ok)
        {
            await bot.SendMessage(chatId, "Не удалось отменить запись (не найдена или уже отменена).", cancellationToken: ct);
            return;
        }

        await bot.SendMessage(
            chatId,
            $"Запись #{appointmentId} отменена. Будем рады видеть вас в другой день!",
            replyMarkup: MainMenuKeyboard(),
            cancellationToken: ct);
    }

    /// <summary>Главное меню: ReplyKeyboard остаётся под строкой ввода — удобно для клиентов салона.</summary>
    private static ReplyKeyboardMarkup MainMenuKeyboard() =>
        new(new[]
        {
            new KeyboardButton[] { new(UiTexts.BtnServices), new(UiTexts.BtnBook) },
            new KeyboardButton[] { new(UiTexts.BtnMyAppointments), new(UiTexts.BtnCancelAppointment) },
            new KeyboardButton[] { new(UiTexts.BtnHelp) },
        })
        {
            ResizeKeyboard = true
        };
}

/// <summary>Тексты кнопок главного меню (совпадают с тем, что ловим в обработчике сообщений).</summary>
internal static class UiTexts
{
    public const string BtnServices = "💅 Услуги";
    public const string BtnBook = "📅 Записаться";
    public const string BtnMyAppointments = "📋 Мои записи";
    public const string BtnCancelAppointment = "❌ Отмена записи";
    public const string BtnHelp = "ℹ️ Помощь";
}
