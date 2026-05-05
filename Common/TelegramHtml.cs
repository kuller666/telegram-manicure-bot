namespace TelegramManicureBot.Common;

/// <summary>Минимальное экранирование для HTML parse_mode.</summary>
public static class TelegramHtml
{
    public static string Escape(string s) =>
        s.Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal);
}
