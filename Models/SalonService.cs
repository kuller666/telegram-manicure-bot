using System.ComponentModel.DataAnnotations;

namespace TelegramManicureBot.Models;

/// <summary>
/// Услуга салона (маникюр, гель-лак и т.д.) с ценой и длительностью.
/// Имя класса SalonService, чтобы не пересекаться с понятием «сервис приложения» в DI.
/// </summary>
public sealed class SalonService
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Краткое описание для карточки услуги.</summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>Цена в условных единицах (рубли без копеек для простоты).</summary>
    public int PriceRub { get; set; }

    /// <summary>Длительность выполнения в минутах (используется при расчёте занятости).</summary>
    public int DurationMinutes { get; set; }

    /// <summary>Можно скрыть услугу без удаления из истории.</summary>
    public bool IsActive { get; set; } = true;

    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
