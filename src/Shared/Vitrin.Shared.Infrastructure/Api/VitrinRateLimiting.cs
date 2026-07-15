using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Vitrin.Shared.Infrastructure.Auth;

namespace Vitrin.Shared.Infrastructure.Api;

public static class VitrinRateLimitPolicies
{
    public const string Login = "auth-login";
    public const string Registration = "auth-registration";
    public const string ExternalLogin = "auth-external-login";
    public const string AiAnalysis = "ai-analysis";
}

public static class VitrinRateLimitingExtensions
{
    public static IServiceCollection AddVitrinRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                TimeSpan? retryAfter = null;
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfterValue))
                {
                    retryAfter = retryAfterValue;
                    context.HttpContext.Response.Headers.RetryAfter =
                        Math.Ceiling(retryAfterValue.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                }

                await Results.Problem(
                        statusCode: StatusCodes.Status429TooManyRequests,
                        title: "Too many requests.",
                        detail: "The request limit was exceeded. Please wait before trying again.",
                        extensions: ApiProblemResults.Extensions(
                            "rate_limit.exceeded",
                            ("retryAfterSeconds", retryAfter is null
                                ? null
                                : (int)Math.Ceiling(retryAfter.Value.TotalSeconds))))
                    .ExecuteAsync(context.HttpContext);
            };

            options.AddPolicy(VitrinRateLimitPolicies.Login, context =>
                FixedWindow(ClientIp(context), 5, TimeSpan.FromMinutes(1)));

            options.AddPolicy(VitrinRateLimitPolicies.Registration, context =>
                FixedWindow(ClientIp(context), 3, TimeSpan.FromMinutes(10)));

            options.AddPolicy(VitrinRateLimitPolicies.ExternalLogin, context =>
                FixedWindow(ClientIp(context), 10, TimeSpan.FromMinutes(1)));

            options.AddPolicy(VitrinRateLimitPolicies.AiAnalysis, context =>
                FixedWindow(
                    context.User.GetUserId()?.ToString() ?? ClientIp(context),
                    5,
                    TimeSpan.FromMinutes(1)));
        });

        return services;
    }

    private static RateLimitPartition<string> FixedWindow(
        string partitionKey,
        int permitLimit,
        TimeSpan window) =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = permitLimit,
                QueueLimit = 0,
                Window = window
            });

    private static string ClientIp(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
