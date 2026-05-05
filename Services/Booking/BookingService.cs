using Microsoft.EntityFrameworkCore;
using TelegramManicureBot.Data;
using TelegramManicureBot.Models;

namespace TelegramManicureBot.Services.Booking;

/// <inheritdoc />
public sealed class BookingService(AppDbContext db) : IBookingService
{
    /// <inheritdoc />
    public async Task<Appointment?> CreateAsync(
        int appUserId,
        int salonServiceId,
        DateTime startUtc,
        CancellationToken ct = default)
    {
        var service = await db.SalonServices.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == salonServiceId && s.IsActive, ct);
        if (service is null)
            return null;

        var start = DateTime.SpecifyKind(startUtc, DateTimeKind.Utc);
        var end = start.AddMinutes(service.DurationMinutes);

        // Повторная проверка коллизий на момент сохранения
        var collision = await db.Appointments.AnyAsync(a =>
            a.Status == AppointmentStatus.Active &&
            start < a.EndUtc && end > a.StartUtc, ct);

        if (collision)
            return null;

        var appointment = new Appointment
        {
            AppUserId = appUserId,
            SalonServiceId = salonServiceId,
            StartUtc = start,
            EndUtc = end,
            Status = AppointmentStatus.Active,
            CreatedUtc = DateTime.UtcNow
        };

        db.Appointments.Add(appointment);
        await db.SaveChangesAsync(ct);

        return appointment;
    }

    /// <inheritdoc />
    public async Task<bool> CancelOwnAppointmentAsync(long telegramUserId, int appointmentId, CancellationToken ct = default)
    {
        var user = await db.TelegramUsers.FirstOrDefaultAsync(u => u.TelegramUserId == telegramUserId, ct);
        if (user is null)
            return false;

        var appt = await db.Appointments.FirstOrDefaultAsync(a =>
            a.Id == appointmentId && a.AppUserId == user.Id, ct);

        if (appt is null || appt.Status != AppointmentStatus.Active)
            return false;

        appt.Status = AppointmentStatus.Cancelled;
        await db.SaveChangesAsync(ct);
        return true;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Appointment>> GetUpcomingForTelegramUserAsync(long telegramUserId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return await db.Appointments
            .AsNoTracking()
            .Include(a => a.SalonService)
            .Where(a => a.TelegramUser.TelegramUserId == telegramUserId)
            .Where(a => a.Status == AppointmentStatus.Active && a.StartUtc >= now)
            .OrderBy(a => a.StartUtc)
            .ToListAsync(ct);
    }
}
