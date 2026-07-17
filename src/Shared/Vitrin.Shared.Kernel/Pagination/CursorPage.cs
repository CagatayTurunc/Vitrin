using System.Globalization;
using System.Text;

namespace Vitrin.Shared.Kernel.Pagination;

public sealed record CursorPage<T>(
    IReadOnlyList<T> Items,
    string? NextCursor,
    bool HasMore);

public readonly record struct KeysetCursor(DateTime TimestampUtc, Guid Id);

public readonly record struct SortedKeysetCursor(
    string Sort,
    double Value,
    DateTime TimestampUtc,
    Guid Id,
    DateTime AnchorUtc,
    string Scope);

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

public static class SortedKeysetCursorCodec
{
    private const string Version = "v2";

    public static string Encode(
        string sort,
        double value,
        DateTime timestampUtc,
        Guid id,
        DateTime anchorUtc,
        string scope = "default")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sort);
        ArgumentException.ThrowIfNullOrWhiteSpace(scope);
        if (sort.Contains(':') || scope.Contains(':') || scope.Length > 64)
            throw new ArgumentException("Cursor sort and scope cannot contain separators.");
        if (!double.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value));

        var timestamp = timestampUtc.Kind == DateTimeKind.Utc
            ? timestampUtc
            : timestampUtc.ToUniversalTime();
        var anchor = anchorUtc.Kind == DateTimeKind.Utc
            ? anchorUtc
            : anchorUtc.ToUniversalTime();
        var payload = string.Create(
            CultureInfo.InvariantCulture,
            $"{Version}:{sort}:{value:R}:{timestamp.Ticks}:{id:N}:{anchor.Ticks}:{scope}");

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(payload))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public static bool TryDecode(string? value, out SortedKeysetCursor cursor)
    {
        cursor = default;
        if (string.IsNullOrWhiteSpace(value) || value.Length > 512) return false;

        try
        {
            var base64 = value.Replace('-', '+').Replace('_', '/');
            base64 = base64.PadRight(base64.Length + ((4 - base64.Length % 4) % 4), '=');
            var payload = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
            var parts = payload.Split(':', StringSplitOptions.None);

            if (parts.Length != 7 || parts[0] != Version || string.IsNullOrWhiteSpace(parts[1]) ||
                !double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var sortValue) ||
                !double.IsFinite(sortValue) ||
                !long.TryParse(parts[3], NumberStyles.None, CultureInfo.InvariantCulture, out var timestampTicks) ||
                timestampTicks < DateTime.MinValue.Ticks || timestampTicks > DateTime.MaxValue.Ticks ||
                !Guid.TryParseExact(parts[4], "N", out var id) ||
                !long.TryParse(parts[5], NumberStyles.None, CultureInfo.InvariantCulture, out var anchorTicks) ||
                anchorTicks < DateTime.MinValue.Ticks || anchorTicks > DateTime.MaxValue.Ticks ||
                string.IsNullOrWhiteSpace(parts[6]) || parts[6].Length > 64)
            {
                return false;
            }

            cursor = new SortedKeysetCursor(
                parts[1],
                sortValue,
                new DateTime(timestampTicks, DateTimeKind.Utc),
                id,
                new DateTime(anchorTicks, DateTimeKind.Utc),
                parts[6]);
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
