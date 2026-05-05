namespace TelegramManicureBot.Common;

/// <summary>
/// Префиксы callback_data для inline-кнопок (короткие строки — лимит Telegram 64 байта).
/// Формат: prefix:value
/// </summary>
public static class CallbackPrefixes
{
    /// <summary>Выбор услуги при записи: svc:123</summary>
    public const string Service = "svc";

    /// <summary>Выбор даты: dt:yyyy-MM-dd</summary>
    public const string Date = "dt";

    /// <summary>Выбор времени начала слота: tm:HH:mm</summary>
    public const string Time = "tm";

    /// <summary>Отмена конкретной записи: cap:appointmentId</summary>
    public const string CancelAppointment = "cap";

    /// <summary>Список «мои записи» (обновить сообщение)</summary>
    public const string RefreshMyAppointments = "my";
}
