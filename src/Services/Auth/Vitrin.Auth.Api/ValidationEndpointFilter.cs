using FluentValidation;
using Vitrin.Shared.Infrastructure.Api;

namespace Vitrin.Auth.Api;

public sealed class ValidationEndpointFilter<TRequest>(IValidator<TRequest> validator) : IEndpointFilter
    where TRequest : class
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var request = context.Arguments.OfType<TRequest>().FirstOrDefault();
        if (request is null)
        {
            return await next(context);
        }

        var validationResult = await validator.ValidateAsync(request, context.HttpContext.RequestAborted);
        if (validationResult.IsValid)
        {
            return await next(context);
        }

        return Results.ValidationProblem(
            validationResult.ToDictionary(),
            title: "One or more validation errors occurred.",
            extensions: ApiProblemResults.Extensions("request.validation_failed"));
    }
}
