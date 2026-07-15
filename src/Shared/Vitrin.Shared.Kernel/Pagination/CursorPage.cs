using System.Globalization;
using System.Text;

namespace Vitrin.Shared.Kernel.Pagination;

public sealed record CursorPage<T>(
    IReadOnlyList<T> Items,
    string? NextCursor,
    bool HasMore);

public readonly record struct KeysetCursor(DateTime TimestampUtc, Guid Id);

public static class KeysetCursorCodec
{
    private const string Version = "v1";

    public static string Encode(DateTime timestampUtc, Guid id)
    {
        var utcTimestamp = timestampUtc.Kind == DateTimeKind.Utc
            ? timestampUtc
            : timestampUtc.ToUniversalTime();
        var payload = string.Create(
            CultureInfo.InvariantCulture,
            $"{Version}:{utcTimestamp.Ticks}:{id:N}");

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(payload))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public static bool TryDecode(string? value, out KeysetCursor cursor)
    {
        cursor = default;
        if (string.IsNullOrWhiteSpace(value) || value.Length > 256)
        {
            return false;
        }

        try
        {
            var base64 = value.Replace('-', '+').Replace('_', '/');
            base64 = base64.PadRight(base64.Length + ((4 - base64.Length % 4) % 4), '=');
            var payload = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
            var parts = payload.Split(':', StringSplitOptions.None);

            if (parts.Length != 3 || parts[0] != Version ||
                !long.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var ticks) ||
                ticks < DateTime.MinValue.Ticks || ticks > DateTime.MaxValue.Ticks ||
                !Guid.TryParseExact(parts[2], "N", out var id))
            {
                return false;
            }

            cursor = new KeysetCursor(new DateTime(ticks, DateTimeKind.Utc), id);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
