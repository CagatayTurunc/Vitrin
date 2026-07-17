using MediatR;
using Vitrin.Shared.Kernel.Results;

namespace Vitrin.Product.Application.Commands;

public record RetractToDraftCommand(Guid ProductId, Guid RequestingUserId, string RequestingUsername = "") : IRequest<Result>;

public class RetractToDraftCommandHandler : IRequestHandler<RetractToDraftCommand, Result>
{
    private readonly IProductRepository _repository;

    public RetractToDraftCommandHandler(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(RetractToDraftCommand request, CancellationToken cancellationToken)
    {
        var product = await _repository.GetByIdAsync(request.ProductId, cancellationToken);
        if (product is null)
            return Result.Failure("Product not found.");

        if (!product.CanEdit(request.RequestingUserId))
            return Result.Failure("You do not have edit access to this product.");

        var result = product.RetractToDraft();
        if (result.IsFailure) return result;

        await _repository.UpdateWithRevisionAsync(
            product,
            request.RequestingUserId,
            request.RequestingUsername,
            "retracted_to_draft",
            "Ürün taslağa geri çekildi.",
            cancellationToken);
        return Result.Success();
    }
}
