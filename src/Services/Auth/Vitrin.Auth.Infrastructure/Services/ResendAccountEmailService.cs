using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Vitrin.Auth.Application.Interfaces;
using Vitrin.Auth.Domain.Entities;

namespace Vitrin.Auth.Infrastructure.Services;

public sealed class ResendAccountEmailService(
    HttpClient httpClient,
    IConfiguration configuration,
    IHostEnvironment environment,
    ILogger<ResendAccountEmailService> logger) : IAccountEmailService
{
    public Task<bool> SendEmailConfirmationAsync(User user, string token, CancellationToken cancellationToken)
    {
        var url = BuildAppUrl("/confirm-email", token);
        return SendAsync(
            user.Email,
            "Vitrin e-posta adresini doğrula",
            EmailLayout(
                user.FullName,
                "E-posta adresini doğrula",
                "Vitrin hesabını kullanmaya başlamak için e-posta adresini doğrula.",
                "E-postamı doğrula",
                url,
                "Bu bağlantı 24 saat geçerlidir."),
            $"Vitrin hesabını doğrulamak için bağlantıyı aç: {url}",
            url,
            cancellationToken);
    }

    public Task<bool> SendPasswordResetAsync(User user, string token, CancellationToken cancellationToken)
    {
        var url = BuildAppUrl("/reset-password", token);
        return SendAsync(
            user.Email,
            "Vitrin şifreni yenile",
            EmailLayout(
                user.FullName,
                "Şifreni yenile",
                "Vitrin hesabın için bir şifre yenileme isteği aldık.",
                "Yeni şifre belirle",
                url,
                "Bu bağlantı 1 saat geçerlidir. İsteği sen yapmadıysan bu e-postayı yok sayabilirsin."),
            $"Vitrin şifreni yenilemek için bağlantıyı aç: {url}",
            url,
            cancellationToken);
    }

    public Task<bool> SendMakerApprovedAsync(User user, CancellationToken cancellationToken)
    {
        var url = $"{GetAppBaseUrl()}/submit";
        return SendAsync(
            user.Email,
            "Maker başvurun onaylandı",
            EmailLayout(
                user.FullName,
                "Artık Vitrin Maker'sın",
                "Başvurun onaylandı. İlk ürününü topluluğa gönderebilirsin.",
                "Ürünümü gönder",
                url,
                "Ürünün incelemeye gönderildiğinde durumunu Ürünlerim sayfasından takip edebilirsin."),
            $"Maker başvurun onaylandı. Ürün göndermek için: {url}",
            url,
            cancellationToken);
    }

    private async Task<bool> SendAsync(
        string recipient,
        string subject,
        string html,
        string plainText,
        string developmentUrl,
        CancellationToken cancellationToken)
    {
        var apiKey = configuration["Email:Resend:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            if (environment.IsDevelopment())
            {
                logger.LogInformation(
                    "Resend is not configured. Development email link for {Recipient}: {Url}",
                    recipient,
                    developmentUrl);
                return true;
            }

            logger.LogError("Resend API key is not configured; email to {Recipient} was not sent.", recipient);
            return false;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "emails");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Headers.TryAddWithoutValidation("Idempotency-Key", Guid.NewGuid().ToString("N"));
        request.Content = JsonContent.Create(new
        {
            from = configuration["Email:From"] ?? "Vitrin <onboarding@resend.dev>",
            to = new[] { recipient },
            subject,
            html,
            text = plainText
        });

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode) return true;

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        logger.LogError(
            "Resend email request failed with status {StatusCode}: {ResponseBody}",
            (int)response.StatusCode,
            responseBody);
        return false;
    }

    private string BuildAppUrl(string path, string token) =>
        $"{GetAppBaseUrl()}{path}?token={Uri.EscapeDataString(token)}";

    private string GetAppBaseUrl() =>
        (configuration["Email:AppBaseUrl"] ?? "http://localhost:3001").TrimEnd('/');

    private static string EmailLayout(
        string name,
        string title,
        string intro,
        string action,
        string actionUrl,
        string footer)
    {
        var safeName = WebUtility.HtmlEncode(name);
        var safeTitle = WebUtility.HtmlEncode(title);
        var safeIntro = WebUtility.HtmlEncode(intro);
        var safeAction = WebUtility.HtmlEncode(action);
        var safeUrl = WebUtility.HtmlEncode(actionUrl);
        var safeFooter = WebUtility.HtmlEncode(footer);

        return $$"""
            <!doctype html>
            <html lang="tr">
              <body style="margin:0;background:#f4f7f5;font-family:Arial,sans-serif;color:#10231b">
                <div style="max-width:600px;margin:0 auto;padding:40px 20px">
                  <div style="background:#ffffff;border:1px solid #dce7e1;border-radius:20px;padding:36px">
                    <div style="font-size:20px;font-weight:800;color:#007a52;margin-bottom:28px">Vitrin</div>
                    <p style="margin:0 0 10px">Merhaba {{safeName}},</p>
                    <h1 style="font-size:28px;line-height:1.2;margin:0 0 14px">{{safeTitle}}</h1>
                    <p style="font-size:16px;line-height:1.6;color:#50645b;margin:0 0 28px">{{safeIntro}}</p>
                    <a href="{{safeUrl}}" style="display:inline-block;background:#007a52;color:#fff;text-decoration:none;font-weight:700;padding:14px 22px;border-radius:12px">{{safeAction}}</a>
                    <p style="font-size:13px;line-height:1.5;color:#7b8d84;margin:28px 0 0">{{safeFooter}}</p>
                  </div>
                </div>
              </body>
            </html>
            """;
    }
}
