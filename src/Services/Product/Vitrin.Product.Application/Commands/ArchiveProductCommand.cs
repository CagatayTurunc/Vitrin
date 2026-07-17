using MediatR;
using Vitrin.Shared.Kernel.Results;

namespace Vitrin.Product.Application.Commands;

public record ArchiveProductCommand(Guid ProductId, Guid RequestingUserId, bool IsAdmin, string RequestingUsername = "") : IRequest<Result>;

public class ArchiveProductCommandHandler : IRequestHandler<ArchiveProductCommand, Result>
{
    private readonly IProductRepository _repository;

    public ArchiveProductCommandHandler(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(ArchiveProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _repository.GetByIdAsync(request.ProductId, cancellationToken);
        if (product is null)
            return Result.Failure("Product not found.");

        // Only the maker or an admin can archive
        if (!request.IsAdmin && product.MakerId != request.RequestingUserId)
            return Result.Failure("You do not own this product.");

        var result = product.Archive();
        if (result.IsFailure) return result;

        await _repository.UpdateWithRevisionAsync(
            product,
            request.RequestingUserId,
            request.RequestingUsername,
            "archived",
            "Ürün arşivlendi.",
            cancellationToken);
        return Result.Success();
    }
}
