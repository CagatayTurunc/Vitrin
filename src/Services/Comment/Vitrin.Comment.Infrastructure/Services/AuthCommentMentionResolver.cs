using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Vitrin.Comment.Application.Commands;

namespace Vitrin.Comment.Infrastructure.Services;

public sealed partial class AuthCommentMentionResolver(
    HttpClient httpClient,
    ILogger<AuthCommentMentionResolver> logger) : ICommentMentionResolver
{
    public async Task<IReadOnlyCollection<MentionRecipient>> ResolveAsync(
        string content,
        CancellationToken cancellationToken = default)
    {
        var usernames = MentionRegex()
            .Matches(content)
            .Select(match => match.Groups[1].Value.ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToArray();

        if (usernames.Length == 0) return Array.Empty<MentionRecipient>();

        try
        {
            var query = string.Join(',', usernames.Select(Uri.EscapeDataString));
            var recipients = await httpClient.GetFromJsonAsync<List<MentionRecipient>>(
                $"/api/auth/users/resolve?usernames={query}",
                cancellationToken);
            return recipients ?? new List<MentionRecipient>();
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Mentioned users could not be resolved.");
            return Array.Empty<MentionRecipient>();
        }
    }

    [GeneratedRegex(@"(?<![\w@])@([a-zA-Z0-9_]{2,30})", RegexOptions.CultureInvariant)]
    private static partial Regex MentionRegex();
}
