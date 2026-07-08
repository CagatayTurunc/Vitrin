using MediatR;
using Vitrin.Shared.Kernel.Results;

namespace Vitrin.Voting.Application.Commands;

public record AddVoteCommand(
    Guid UserId,
    Guid ProductId) : IRequest<Result>;
