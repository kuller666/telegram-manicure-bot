using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Telegram.Bot.Types;
using TelegramManicureBot.Common;
using TelegramManicureBot.Services.Telegram;

namespace TelegramManicureBot.Controllers;

/// <summary>
/// Принимает POST от Telegram при режиме webhook.
/// URL нужно указать в SetWebhook (HTTPS). Для локальной отладки используйте ngrok/cloudflare tunnel.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class TelegramWebhookController(
    ITelegramUpdateHandler handler,
    IOptions<TelegramBotOptions> botOptions,
    ILogger<TelegramWebhookController> logger) : ControllerBase
{
    /// <summary>Путь по умолчанию: /api/TelegramWebhook</summary>
    /// <remarks>
    /// Тело — JSON Update в формате Telegram (snake_case). В <see cref="Program"/> для MVC заданы JsonSerializerOptions.
    /// </remarks>
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] Update? update, CancellationToken ct)
    {
        var cfg = botOptions.Value;

        // Если задан секрет в BotFather / при установке webhook — Telegram присылает его в заголовке.
        if (!string.IsNullOrWhiteSpace(cfg.WebhookSecretToken))
        {
            var header = Request.Headers["X-Telegram-Bot-Api-Secret-Token"].FirstOrDefault();
            if (!string.Equals(header, cfg.WebhookSecretToken, StringComparison.Ordinal))
                return Unauthorized();
        }

        if (update is null)
        {
            logger.LogWarning("Webhook: пустое или некорректное тело запроса.");
            return BadRequest();
        }

        await handler.HandleAsync(update, ct);
        return Ok();
    }
}
