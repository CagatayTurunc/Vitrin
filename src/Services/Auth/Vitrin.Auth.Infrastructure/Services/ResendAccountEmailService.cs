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
            <head>
              <meta charset="UTF-8" />
              <meta name="viewport" content="width=device-width, initial-scale=1.0" />
              <title>{{safeTitle}} — Vitrin</title>
            </head>
            <body style="margin:0;padding:0;background-color:#0d1117;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;">
              <table width="100%" cellpadding="0" cellspacing="0" style="background-color:#0d1117;padding:48px 16px;">
                <tr>
                  <td align="center">
                    <table width="100%" cellpadding="0" cellspacing="0" style="max-width:560px;">

                      <!-- LOGO / HEADER -->
                      <tr>
                        <td align="center" style="padding-bottom:32px;">
                          <table cellpadding="0" cellspacing="0">
                            <tr>
                              <td style="background:#00c97a;border-radius:12px;padding:8px 10px;line-height:0;">
                                <!-- Vitrin logo mark -->
                                <svg width="22" height="22" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                                  <rect width="24" height="24" rx="6" fill="#00c97a"/>
                                  <path d="M7 8L12 16L17 8" stroke="#0d1117" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"/>
                                </svg>
                              </td>
                              <td style="padding-left:10px;">
                                <span style="font-size:20px;font-weight:800;color:#e6edf3;letter-spacing:-0.5px;">Vitrin</span>
                              </td>
                            </tr>
                          </table>
                        </td>
                      </tr>

                      <!-- CARD -->
                      <tr>
                        <td style="background:#161b22;border:1px solid #30363d;border-radius:16px;padding:40px 36px;">

                          <!-- Greeting -->
                          <p style="margin:0 0 6px;font-size:14px;color:#8b949e;">Merhaba {{safeName}},</p>

                          <!-- Title -->
                          <h1 style="margin:0 0 16px;font-size:26px;font-weight:800;color:#e6edf3;line-height:1.25;">{{safeTitle}}</h1>

                          <!-- Divider -->
                          <div style="height:1px;background:#21262d;margin:0 0 24px;"></div>

                          <!-- Intro -->
                          <p style="margin:0 0 32px;font-size:15px;line-height:1.7;color:#8b949e;">{{safeIntro}}</p>

                          <!-- CTA Button -->
                          <table cellpadding="0" cellspacing="0" style="margin-bottom:32px;">
                            <tr>
                              <td style="background:#00c97a;border-radius:10px;">
                                <a href="{{safeUrl}}"
                                   style="display:inline-block;padding:14px 28px;font-size:15px;font-weight:700;color:#0d1117;text-decoration:none;letter-spacing:0.1px;">
                                  {{safeAction}}
                                </a>
                              </td>
                            </tr>
                          </table>

                          <!-- Fallback link -->
                          <p style="margin:0 0 24px;font-size:13px;color:#8b949e;">
                            Düğme çalışmıyorsa bu bağlantıyı kopyalayıp tarayıcına yapıştır:<br/>
                            <a href="{{safeUrl}}" style="color:#00c97a;word-break:break-all;text-decoration:none;">{{safeUrl}}</a>
                          </p>

                          <!-- Divider -->
                          <div style="height:1px;background:#21262d;margin:0 0 20px;"></div>

                          <!-- Footer note -->
                          <p style="margin:0;font-size:12px;line-height:1.6;color:#6e7681;">{{safeFooter}}</p>

                        </td>
                      </tr>

                      <!-- BOTTOM FOOTER -->
                      <tr>
                        <td align="center" style="padding-top:28px;">
                          <p style="margin:0;font-size:12px;color:#484f58;">
                            Bu e-posta <span style="color:#8b949e;">vitrin.com.tr</span> tarafından gönderildi.<br/>
                            İsteği sen yapmadıysan bu e-postayı güvenle silebilirsin.
                          </p>
                        </td>
                      </tr>

                    </table>
                  </td>
                </tr>
              </table>
            </body>
            </html>
            """;
    }
}
