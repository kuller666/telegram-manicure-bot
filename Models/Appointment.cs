using System.ComponentModel.DataAnnotations;

namespace TelegramManicureBot.Models;

/// <summary>
/// Запись клиента: связка пользователя, услуги и интервала времени.
/// Время хранится в UTC для однозначности (отображение — в московском поясе).
/// </summary>
public sealed class Appointment
{
    public int Id { get; set; }

    /// <summary>FK на сущность пользователя в нашей БД (TelegramUser.Id), не путать с Telegram User Id.</summary>
    public int AppUserId { get; set; }
    public TelegramUser TelegramUser { get; set; } = null!;

    /// <summary>FK на услугу.</summary>
    public int SalonServiceId { get; set; }
    public SalonService SalonService { get; set; } = null!;

    /// <summary>Начало записи в UTC.</summary>
    public DateTime StartUtc { get; set; }

    /// <summary>Окончание записи в UTC (Start + длительность услуги).</summary>
    public DateTime EndUtc { get; set; }

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Active;

    public DateTime CreatedUtc { get; set; }

    /// <summary>Флаг: напоминание «за ~24 часа» уже отправлено.</summary>
    public bool Reminder24hSent { get; set; }

    /// <summary>Флаг: напоминание «за ~2 часа» уже отправлено.</summary>
    public bool Reminder2hSent { get; set; }

    [MaxLength(500)]
    public string? ClientComment { get; set; }
}
