using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TelegramManicureBot.Common;
using TelegramManicureBot.Data;
using TelegramManicureBot.Models;

namespace TelegramManicureBot.Services.Booking;

/// <inheritdoc />
public sealed class AvailabilityService(
    AppDbContext db,
    IOptions<SalonScheduleOptions> scheduleOptions) : IAvailabilityService
{
    private readonly SalonScheduleOptions _opt = scheduleOptions.Value;

    /// <inheritdoc />
    public async Task<IReadOnlyList<TimeSlot>> GetAvailableSlotsAsync(
        DateOnly salonLocalDate,
        int serviceDurationMinutes,
        CancellationToken ct = default)
    {
        var tz = TimeZoneHelper.Moscow;

        // Границы календарного дня в салоне → в UTC
        var localDayStart = new DateTime(salonLocalDate.Year, salonLocalDate.Month, salonLocalDate.Day, _opt.DayStartHour, 0, 0, DateTimeKind.Unspecified);
        var localDayEnd = new DateTime(salonLocalDate.Year, salonLocalDate.Month, salonLocalDate.Day, _opt.DayEndHour, 0, 0, DateTimeKind.Unspecified);

        var dayStartUtc = TimeZoneInfo.ConvertTimeToUtc(localDayStart, tz);
        var dayEndUtc = TimeZoneInfo.ConvertTimeToUtc(localDayEnd, tz);

        // Все активные записи, которые пересекаются с этим календарным днём
        var busy = await db.Appointments
            .AsNoTracking()
            .Where(a => a.Status == AppointmentStatus.Active)
            .Where(a => a.EndUtc > dayStartUtc && a.StartUtc < dayEndUtc)
            .Select(a => new { a.StartUtc, a.EndUtc })
            .ToListAsync(ct);

        var slotStep = TimeSpan.FromMinutes(Math.Max(5, _opt.SlotMinutes));
        var result = new List<TimeSlot>();

        // Перебираем потенциальные старты слотов
        for (var cursor = dayStartUtc; cursor + TimeSpan.FromMinutes(serviceDurationMinutes) <= dayEndUtc; cursor += slotStep)
        {
            var candidateEnd = cursor.AddMinutes(serviceDurationMinutes);

            var overlaps = busy.Any(b =>
                cursor < b.EndUtc && candidateEnd > b.StartUtc);

            if (!overlaps)
                result.Add(new TimeSlot(cursor));
        }

        return result;
    }
}
