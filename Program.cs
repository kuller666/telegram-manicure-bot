using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using TelegramManicureBot.Common;
using TelegramManicureBot.Data;
using TelegramManicureBot.Services.Booking;
using TelegramManicureBot.Services.Conversation;
using TelegramManicureBot.Services.Notifications;
using TelegramManicureBot.Services.Reminders;
using TelegramManicureBot.Services.Telegram;
using TelegramManicureBot.Services.Users;

var builder = WebApplication.CreateBuilder(args);

// Telegram шлёт JSON с ключами в snake_case — политика нужна для корректной привязки [FromBody] Update.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    });

builder.Services.Configure<TelegramBotOptions>(
    builder.Configuration.GetSection(TelegramBotOptions.SectionName));
builder.Services.Configure<SalonScheduleOptions>(
    builder.Configuration.GetSection(SalonScheduleOptions.SectionName));

// SQLite + EF Core (файл manicure_bot.db в рабочей директории при запуске).
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("Default")));

// Telegram.Bot: один клиент на процесс.
builder.Services.AddSingleton<ITelegramBotClient>(_ =>
{
    var token = builder.Configuration
        .GetSection(TelegramBotOptions.SectionName)
        .Get<TelegramBotOptions>()?.BotToken;

    if (string.IsNullOrWhiteSpace(token))
        throw new InvalidOperationException(
            "Укажите Telegram:BotToken в appsettings.json, переменных окружения или User Secrets.");

    return new TelegramBotClient(token);
});

// Доменные сервисы.
builder.Services.AddScoped<IAppUserService, AppUserService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IAvailabilityService, AvailabilityService>();
builder.Services.AddScoped<IMasterNotificationService, MasterNotificationService>();
builder.Services.AddSingleton<BookingDraftStore>();

builder.Services.AddScoped<ITelegramUpdateHandler, TelegramUpdateHandler>();

// Фоновые напоминания клиентам.
builder.Services.AddHostedService<AppointmentReminderHostedService>();

builder.Services.AddOpenApi();

var app = builder.Build();

// Применяем миграции и начальное заполнение услуг.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbSeed.EnsureSeedAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

await app.RunAsync();
