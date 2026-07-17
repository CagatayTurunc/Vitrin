using MediatR;
using Vitrin.Shared.Kernel.Results;

namespace Vitrin.Product.Application.Commands;

public record SubmitForReviewCommand(Guid ProductId, Guid RequestingUserId, string RequestingUsername = "") : IRequest<Result>;

public class SubmitForReviewCommandHandler : IRequestHandler<SubmitForReviewCommand, Result>
{
    private readonly IProductRepository _repository;

    public SubmitForReviewCommandHandler(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(SubmitForReviewCommand request, CancellationToken cancellationToken)
    {
        var product = await _repository.GetByIdAsync(request.ProductId, cancellationToken);
        if (product is null)
            return Result.Failure("Product not found.");

        if (!product.CanEdit(request.RequestingUserId))
            return Result.Failure("You do not have edit access to this product.");

        var result = product.SubmitForReview();
        if (result.IsFailure) return result;

        await _repository.UpdateWithRevisionAsync(
            product,
            request.RequestingUserId,
            request.RequestingUsername,
            "submitted_for_review",
            "Ürün incelemeye gönderildi.",
            cancellationToken);
        return Result.Success();
    }
}
