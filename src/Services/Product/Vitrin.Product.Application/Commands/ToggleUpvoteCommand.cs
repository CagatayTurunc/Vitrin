using MediatR;
using Vitrin.Shared.Kernel.Results;

namespace Vitrin.Product.Application.Commands;

public record ToggleUpvoteCommand(Guid ProductId, Guid UserId) : IRequest<Result<int>>;
