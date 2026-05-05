namespace TelegramManicureBot.Models;

/// <summary>
/// Статус записи. Отменённые слоты снова становятся доступными.
/// </summary>
public enum AppointmentStatus
{
    /// <summary>Активная запись.</summary>
    Active = 0,

    /// <summary>Клиент или администратор отменил.</summary>
    Cancelled = 1,

    /// <summary>Выполнено (можно расширять под отчёты).</summary>
    Completed = 2
}
