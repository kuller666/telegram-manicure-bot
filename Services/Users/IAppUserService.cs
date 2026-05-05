using TelegramManicureBot.Models;

namespace TelegramManicureBot.Services.Users;

/// <summary>
/// Работа с пользователем бота в БД: найти или создать по данным из Telegram.
/// </summary>
public interface IAppUserService
{
    /// <summary>Возвращает сущность пользователя и признак «только что создан».</summary>
    Task<(TelegramUser User, bool Created)> GetOrCreateAsync(
        long telegramUserId,
        string? username,
        string? firstName,
        string? lastName,
        CancellationToken ct = default);
}
