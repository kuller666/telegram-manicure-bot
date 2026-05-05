using Microsoft.EntityFrameworkCore;
using TelegramManicureBot.Models;

namespace TelegramManicureBot.Data;

/// <summary>
/// Контекст EF Core: пользователи Telegram, услуги, записи.
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<TelegramUser> TelegramUsers => Set<TelegramUser>();
    public DbSet<SalonService> SalonServices => Set<SalonService>();
    public DbSet<Appointment> Appointments => Set<Appointment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // TelegramUser: уникальный индекс по стабильному id из Telegram
        modelBuilder.Entity<TelegramUser>(e =>
        {
            e.HasIndex(x => x.TelegramUserId).IsUnique();
        });

        modelBuilder.Entity<SalonService>(e =>
        {
            e.ToTable("Services");
        });

        modelBuilder.Entity<Appointment>(e =>
        {
            e.HasOne(a => a.TelegramUser)
                .WithMany(u => u.Appointments)
                .HasForeignKey(a => a.AppUserId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(a => a.SalonService)
                .WithMany(s => s.Appointments)
                .HasForeignKey(a => a.SalonServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            // Ускоряем выборки по времени (напоминания, занятость)
            e.HasIndex(a => a.StartUtc);
            e.HasIndex(a => new { a.Status, a.StartUtc });
        });
    }
}
