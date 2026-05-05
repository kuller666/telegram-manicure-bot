using System.ComponentModel.DataAnnotations;

namespace TelegramManicureBot.Models;

/// <summary>
/// Пользователь Telegram, с которым бот переписывается.
/// Храним стабильный Telegram User Id и отображаемое имя для персонализации.
/// </summary>
public sealed class TelegramUser
{
    /// <summary>Внутренний суррогатный ключ БД.</summary>
    public int Id { get; set; }

    /// <summary>
    /// Уникальный идентификатор пользователя в Telegram (не меняется).
    /// Именно его используем для поиска при входящих апдейтах.
    /// </summary>
    public long TelegramUserId { get; set; }

    [MaxLength(64)]
    public string? Username { get; set; }

    [MaxLength(128)]
    public string? FirstName { get; set; }

    [MaxLength(128)]
    public string? LastName { get; set; }

    /// <summary>Время первого /start или первого контакта.</summary>
    public DateTime CreatedUtc { get; set; }

    /// <summary>Навигационное свойство — записи этого клиента.</summary>
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
