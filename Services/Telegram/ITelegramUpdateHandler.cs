using Telegram.Bot.Types;

namespace TelegramManicureBot.Services.Telegram;

/// <summary>
/// Обрабатывает входящие апдейты Telegram (сообщения и callback-кнопки).
/// Вызывается из контроллера webhook после десериализации JSON.
/// </summary>
public interface ITelegramUpdateHandler
{
    Task HandleAsync(Update update, CancellationToken ct = default);
}
