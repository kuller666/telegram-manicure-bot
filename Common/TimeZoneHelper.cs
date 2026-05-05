namespace TelegramManicureBot.Common;

/// <summary>
/// Часовой пояс для отображения времени клиенту (Москва).
/// На Windows и Linux идентификаторы различаются — выбираем доступный.
/// </summary>
public static class TimeZoneHelper
{
    private static readonly Lazy<TimeZoneInfo> MoscowLazy = new(ResolveMoscow);

    public static TimeZoneInfo Moscow => MoscowLazy.Value;

    private static TimeZoneInfo ResolveMoscow()
    {
        // Сначала пробуем IANA (Linux/macOS), затем Windows-имя.
        foreach (var id in new[] { "Europe/Moscow", "Russian Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
                // пробуем следующий вариант
            }
            catch (InvalidTimeZoneException)
            {
                // пробуем следующий вариант
            }
        }

        return TimeZoneInfo.Utc;
    }

    /// <summary>Перевод UTC в локальное время салона для красивого вывода.</summary>
    public static DateTime ToSalonLocal(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Moscow);
}
