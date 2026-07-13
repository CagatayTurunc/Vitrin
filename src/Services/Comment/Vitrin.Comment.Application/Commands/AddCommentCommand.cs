using Vitrin.Comment.Domain.Entities;
using Vitrin.Shared.Kernel.Results;
using MediatR;

namespace Vitrin.Comment.Application.Commands;

public record AddCommentCommand(Guid ProductId, Guid UserId, string UserName, string Content, Guid? ParentCommentId = null) : IRequest<Result<Guid>>;

public interface ICommentRepository
{
    Task<CommentItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(CommentItem comment, CancellationToken cancellationToken);
    Task UpdateAsync(CommentItem comment, CancellationToken cancellationToken);
}

public class AddCommentCommandHandler : IRequestHandler<AddCommentCommand, Result<Guid>>
{
    private readonly ICommentRepository _repository;
    private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;

    public AddCommentCommandHandler(ICommentRepository repository, Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        _repository = repository;
        _configuration = configuration;
    }

    public async Task<Result<Guid>> Handle(AddCommentCommand request, CancellationToken cancellationToken)
    {
        var commentResult = CommentItem.Create(request.ProductId, request.UserId, request.UserName, request.Content, request.ParentCommentId);
        if (!commentResult.IsSuccess)
        {
            return Result<Guid>.Failure(commentResult.Error);
        }

        await _repository.AddAsync(commentResult.Value, cancellationToken);

        // Notify the product owner (Maker) and Parent Comment Owner
        try
        {
            var notificationUrl = _configuration["ServiceUrls:Notification"] ?? "http://vitrin-notification:8080";
            var productUrl = _configuration["ServiceUrls:Product"] ?? "http://vitrin-product:8080";

            using var client = new HttpClient();
            
            // 1. Notify Parent Comment Owner if it's a reply
            if (request.ParentCommentId.HasValue)
            {
                var parentComment = await _repository.GetByIdAsync(request.ParentCommentId.Value, cancellationToken);
                if (parentComment != null && parentComment.UserId != request.UserId)
                {
                    var replyPayload = new
                    {
                        UserId = parentComment.UserId,
                        Message = $"@{request.UserName} yorumunuza cevap verdi."
                    };
                    var replyContent = new System.Net.Http.StringContent(System.Text.Json.JsonSerializer.Serialize(replyPayload), System.Text.Encoding.UTF8, "application/json");
                    await client.PostAsync($"{notificationUrl}/api/notifications", replyContent, cancellationToken);
                }
            }

            // 2. Fetch product to get MakerId and notify Maker
            var productRes = await client.GetAsync($"{productUrl}/api/products/{request.ProductId}", cancellationToken);
            if (productRes.IsSuccessStatusCode)
            {
                var productJson = await productRes.Content.ReadAsStringAsync(cancellationToken);
                var productData = System.Text.Json.JsonDocument.Parse(productJson).RootElement;
                
                if (productData.TryGetProperty("makerId", out var makerIdProp))
                {
                    var makerIdStr = makerIdProp.GetString();
                    if (Guid.TryParse(makerIdStr, out Guid makerId))
                    {
                        // Don't notify if the maker commented on their own product
                        if (makerId != request.UserId)
                        {
                            var notificationPayload = new
                            {
                                UserId = makerId,
                                Message = $"@{request.UserName} ürününüze yorum yaptı"
                            };

                            var content = new System.Net.Http.StringContent(System.Text.Json.JsonSerializer.Serialize(notificationPayload), System.Text.Encoding.UTF8, "application/json");
                            await client.PostAsync($"{notificationUrl}/api/notifications", content, cancellationToken);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[COMMENT NOTIFICATION ERROR] {ex.Message}");
        }
        
        return Result<Guid>.Success(commentResult.Value.Id);
    }
}
