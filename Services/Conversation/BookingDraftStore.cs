using System.Collections.Concurrent;

namespace TelegramManicureBot.Services.Conversation;

/// <summary>
/// Этапы мастера записи через диалог (выбор услуги → даты → времени).
/// Храним черновик в памяти процесса (достаточно для MVP; при нескольких инстансах нужен Redis/БД).
/// </summary>
public enum BookingWizardStep
{
    Idle,
    WaitingServicePick,
    WaitingDatePick,
    WaitingTimePick
}

/// <summary>Снимок незавершённой записи для конкретного Telegram-пользователя.</summary>
public sealed class BookingDraft
{
    public BookingWizardStep Step { get; set; } = BookingWizardStep.Idle;
    public int? ServiceId { get; set; }
    public DateOnly? Date { get; set; }
}

/// <summary>Потокобезопасное хранилище черновиков по ключу Telegram User Id.</summary>
public sealed class BookingDraftStore
{
    private readonly ConcurrentDictionary<long, BookingDraft> _drafts = new();

    public BookingDraft GetOrCreate(long telegramUserId) =>
        _drafts.GetOrAdd(telegramUserId, _ => new BookingDraft());

    public void Reset(long telegramUserId) =>
        _drafts[telegramUserId] = new BookingDraft();
}
