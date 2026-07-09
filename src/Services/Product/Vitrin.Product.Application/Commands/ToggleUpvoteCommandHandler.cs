using MediatR;
using Vitrin.Product.Domain.Entities;
using Vitrin.Shared.Kernel.Results;

namespace Vitrin.Product.Application.Commands;

public class ToggleUpvoteCommandHandler : IRequestHandler<ToggleUpvoteCommand, Result<int>>
{
    private readonly IProductRepository _repository;

    public ToggleUpvoteCommandHandler(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<int>> Handle(ToggleUpvoteCommand request, CancellationToken cancellationToken)
    {
        var product = await _repository.GetByIdWithUpvotesAsync(request.ProductId, cancellationToken);

        if (product == null)
            return Result<int>.Failure("Product not found.");

        product.ToggleUpvote(request.UserId);

        await _repository.UpdateAsync(product, cancellationToken);

        return Result<int>.Success(product.Upvotes.Count);
    }
}
