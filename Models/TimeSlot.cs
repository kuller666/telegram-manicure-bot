namespace TelegramManicureBot.Models;

/// <summary>
/// Свободный интервал для записи — не хранится в БД.
/// Строится сервисом доступности на основе расписания салона и уже занятых <see cref="Appointment"/>.
/// </summary>
/// <param name="StartUtc">Начало слота в UTC.</param>
public sealed record TimeSlot(DateTime StartUtc)
{
    /// <summary>Удобная метка HH:mm в локальном времени салона (Москва).</summary>
    public string FormatLocalLabel()
    {
        var local = Common.TimeZoneHelper.ToSalonLocal(StartUtc);
        return local.ToString("HH:mm");
    }
}
