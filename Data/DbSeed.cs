using Microsoft.EntityFrameworkCore;
using TelegramManicureBot.Models;

namespace TelegramManicureBot.Data;

/// <summary>
/// Первичное заполнение справочника услуг, если таблица пустая.
/// Вызывается при старте приложения.
/// </summary>
public static class DbSeed
{
    public static async Task EnsureSeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        // Для быстрого старта создаём схему без файлов миграций.
        // Для продакшена рекомендуется перейти на dotnet ef migrations + MigrateAsync().
        await db.Database.EnsureCreatedAsync(ct);

        if (await db.SalonServices.AnyAsync(ct))
            return;

        db.SalonServices.AddRange(
            new SalonService
            {
                Name = "Классический маникюр",
                Description = "Обработка ногтей и кутикулы без покрытия.",
                PriceRub = 1200,
                DurationMinutes = 60,
                IsActive = true
            },
            new SalonService
            {
                Name = "Маникюр + гель-лак",
                Description = "Маникюр и стойкое покрытие.",
                PriceRub = 2200,
                DurationMinutes = 90,
                IsActive = true
            },
            new SalonService
            {
                Name = "Дизайн",
                Description = "Дополнительный дизайн (обсуждается при визите).",
                PriceRub = 300,
                DurationMinutes = 30,
                IsActive = true
            },
            new SalonService
            {
                Name = "Снятие покрытия",
                Description = "Безопасное снятие гель-лака.",
                PriceRub = 500,
                DurationMinutes = 30,
                IsActive = true
            }
        );

        await db.SaveChangesAsync(ct);
    }
}
