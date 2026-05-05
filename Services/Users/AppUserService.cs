using Microsoft.EntityFrameworkCore;
using TelegramManicureBot.Data;
using TelegramManicureBot.Models;

namespace TelegramManicureBot.Services.Users;

/// <inheritdoc />
public sealed class AppUserService(AppDbContext db) : IAppUserService
{
    /// <inheritdoc />
    public async Task<(TelegramUser User, bool Created)> GetOrCreateAsync(
        long telegramUserId,
        string? username,
        string? firstName,
        string? lastName,
        CancellationToken ct = default)
    {
        var existing = await db.TelegramUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.TelegramUserId == telegramUserId, ct);

        if (existing is not null)
            return (existing, false);

        var row = new TelegramUser
        {
            TelegramUserId = telegramUserId,
            Username = username,
            FirstName = firstName,
            LastName = lastName,
            CreatedUtc = DateTime.UtcNow
        };

        db.TelegramUsers.Add(row);
        await db.SaveChangesAsync(ct);

        return (row, true);
    }
}
