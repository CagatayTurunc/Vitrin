using MediatR;
using Vitrin.Product.Domain.Entities;
using Vitrin.Shared.Kernel.Results;
using System.Net.Http;

namespace Vitrin.Product.Application.Commands;

public class ToggleUpvoteCommandHandler : IRequestHandler<ToggleUpvoteCommand, Result<int>>
{
    private readonly IProductRepository _repository;
    private readonly IHttpClientFactory _httpClientFactory;

    public ToggleUpvoteCommandHandler(IProductRepository repository, IHttpClientFactory httpClientFactory)
    {
        _repository = repository;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<Result<int>> Handle(ToggleUpvoteCommand request, CancellationToken cancellationToken)
    {
        var product = await _repository.GetByIdWithUpvotesAsync(request.ProductId, cancellationToken);
        if (product == null)
            return Result<int>.Failure("Product not found.");

        bool isNewUpvote = !product.Upvotes.Any(u => u.UserId == request.UserId);

        // Bypass loading the whole product and its upvotes collection for performance and to avoid EF tracking bugs.
        await _repository.ToggleUpvoteAsync(request.ProductId, request.UserId, cancellationToken);
        var count = await _repository.GetUpvoteCountAsync(request.ProductId, cancellationToken);

        if (isNewUpvote && product.MakerId != request.UserId)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var notificationPayload = new
                {
                    UserId = product.MakerId,
                    Message = $"Biri '{product.Name}' adlı ürününüzü oyladı!"
                };
                
                // Notification API port is 5101
                var content = new System.Net.Http.StringContent(System.Text.Json.JsonSerializer.Serialize(notificationPayload), System.Text.Encoding.UTF8, "application/json");
                var res = await client.PostAsync("http://localhost:5101/api/notifications", content, cancellationToken);
                Console.WriteLine($"[NOTIFICATION SENT] Response: {res.StatusCode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NOTIFICATION ERROR] {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine($"[NOTIFICATION SKIPPED] isNewUpvote={isNewUpvote}, MakerId={product.MakerId}, UserId={request.UserId}");
        }

        return Result<int>.Success(count);
    }
}
