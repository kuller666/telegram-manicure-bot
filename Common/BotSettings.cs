namespace TelegramManicureBot.Common;

/// <summary>
/// Настройки Telegram-бота из appsettings (токен, секрет webhook, чат мастера для уведомлений).
/// </summary>
public sealed class TelegramBotOptions
{
    public const string SectionName = "Telegram";

    /// <summary>Токен выдаётся @BotFather.</summary>
    public string BotToken { get; set; } = string.Empty;

    /// <summary>
    /// Необязательный секрет для заголовка X-Telegram-Bot-Api-Secret-Token при установке webhook.
    /// Пустая строка — проверка отключена.
    /// </summary>
    public string WebhookSecretToken { get; set; } = string.Empty;

    /// <summary>
    /// Telegram chat id мастера (число как строка). Уведомления о новых записях уходят сюда.
    /// </summary>
    public string MasterChatId { get; set; } = "0";
}

/// <summary>
/// Расписание салона: шаг слота, рабочие часы, горизонт бронирования.
/// </summary>
public sealed class SalonScheduleOptions
{
    public const string SectionName = "Salon";

    /// <summary>Длительность одного слота в минутах (например 30).</summary>
    public int SlotMinutes { get; set; } = 30;

    public int DayStartHour { get; set; } = 10;
    public int DayEndHour { get; set; } = 19;

    /// <summary>Сколько дней вперёд можно выбрать дату.</summary>
    public int BookingDaysAhead { get; set; } = 14;
}
