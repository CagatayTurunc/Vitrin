using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Vitrin.Shared.Infrastructure.Api;

public static class VitrinApiErrorExtensions
{
    public static IServiceCollection AddVitrinApiErrors(this IServiceCollection services)
    {
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Instance ??= context.HttpContext.Request.Path;

                if (!context.ProblemDetails.Extensions.ContainsKey("traceId"))
                {
                    context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
                }
            };
        });
        services.AddExceptionHandler<VitrinGlobalExceptionHandler>();

        return services;
    }

    public static IApplicationBuilder UseVitrinApiErrors(this IApplicationBuilder app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        return app;
    }
}

public sealed class VitrinGlobalExceptionHandler(
    ILogger<VitrinGlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var isMalformedRequest = exception is BadHttpRequestException;
        var statusCode = isMalformedRequest
            ? StatusCodes.Status400BadRequest
            : StatusCodes.Status500InternalServerError;
        var title = isMalformedRequest
            ? "The request is malformed."
            : "An unexpected error occurred.";
        var detail = isMalformedRequest
            ? "The request body, route, or query value could not be parsed."
            : "The request could not be completed. Use the traceId when contacting support.";
        var code = isMalformedRequest
            ? "request.malformed"
            : "server.unexpected_error";

        if (isMalformedRequest)
        {
            logger.LogWarning(
                exception,
                "Malformed request. TraceId: {TraceId}",
                httpContext.TraceIdentifier);
        }
        else
        {
            logger.LogError(
                exception,
                "Unhandled request error. TraceId: {TraceId}",
                httpContext.TraceIdentifier);
        }

        httpContext.Response.StatusCode = statusCode;

        await Results.Problem(
                statusCode: statusCode,
                title: title,
                detail: detail,
                extensions: ApiProblemResults.Extensions(
                    code,
                    ("traceId", httpContext.TraceIdentifier)))
            .ExecuteAsync(httpContext);

        return true;
    }
}

public static class ApiProblemResults
{
    public static IResult BadRequest(string detail, string code = "request.invalid") =>
        Results.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "The request could not be processed.",
            detail: detail,
            extensions: Extensions(code));

    public static IResult NotFound(string detail, string code = "resource.not_found") =>
        Results.Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "The requested resource was not found.",
            detail: detail,
            extensions: Extensions(code));

    public static IResult Conflict(string detail, string code = "resource.conflict") =>
        Results.Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "The request conflicts with the current resource state.",
            detail: detail,
            extensions: Extensions(code));

    public static IResult TooManyRequests(
        string detail,
        string code,
        DateTimeOffset? resetAtUtc = null) =>
        Results.Problem(
            statusCode: StatusCodes.Status429TooManyRequests,
            title: "Too many requests.",
            detail: detail,
            extensions: resetAtUtc is null
                ? Extensions(code)
                : Extensions(code, ("resetAtUtc", resetAtUtc.Value)));

    public static IDictionary<string, object?> Extensions(
        string code,
        params (string Key, object? Value)[] values)
    {
        var extensions = new Dictionary<string, object?>
        {
            ["code"] = code
        };

        foreach (var (key, value) in values)
        {
            extensions[key] = value;
        }

        return extensions;
    }
}
