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
            "🚀 Vitrin hesabını aktifleştir",
            EmailLayout(
                user.FullName,
                "Hoş geldin! Hesabını aktifleştir",
                "Vitrin topluluğuna katılmak için e-posta adresini doğrulaman gerekiyor. Tek tıkla aktifleştir!",
                "✅ Hesabımı Aktifleştir",
                url,
                "Bu doğrulama bağlantısı 24 saat geçerlidir. Güvenlik için hemen aktifleştirmenizi öneriyoruz."),
            $"Vitrin hesabını aktifleştirmek için bu linke tıkla: {url} Bu bağlantı 24 saat geçerlidir.",
            url,
            cancellationToken);
    }

    public Task<bool> SendPasswordResetAsync(User user, string token, CancellationToken cancellationToken)
    {
        var url = BuildAppUrl("/reset-password", token);
        return SendAsync(
            user.Email,
            "🔑 Vitrin şifreni yenile",
            EmailLayout(
                user.FullName,
                "Şifre yenileme isteği",
                "Vitrin hesabın için şifre yenileme isteği aldık. Güvenli bir şekilde yeni şifre belirleyebilirsin.",
                "🔒 Yeni Şifre Belirle",
                url,
                "Bu şifre yenileme bağlantısı 1 saat geçerlidir. İsteği sen yapmadıysan bu e-postayı güvenle silebilirsin."),
            $"Vitrin şifreni yenilemek için bu linke tıkla: {url} Bu bağlantı 1 saat geçerlidir. İsteği sen yapmadıysan bu maili sil.",
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

    public Task<bool> SendSubscriptionUpgradedAsync(User user, string tier, DateTime periodEnd, CancellationToken cancellationToken)
    {
        var dashboardUrl = $"{GetAppBaseUrl()}/dashboard";
        var tierLabel = tier == "ProMaker" ? "Pro Maker 🏆" : "Enterprise 💎";
        var tierPrice = tier == "ProMaker" ? "₺299/ay" : "₺999/ay";
        var periodEndStr = periodEnd.ToString("d MMMM yyyy", new System.Globalization.CultureInfo("tr-TR"));

        return SendAsync(
            user.Email,
            $"🎉 {tierLabel} aboneliğin aktif!",
            EmailLayout(
                user.FullName ?? user.Username,
                $"{tierLabel} aboneliğin başladı!",
                $"Harika haber! {tierLabel} planına geçişin başarıyla tamamlandı. " +
                $"Bir sonraki yenileme tarihin: {periodEndStr}. " +
                $"Ödeme tutarı: {tierPrice}. Artık tüm premium özelliklere erişebilirsin.",
                "Dashboard'a Git",
                dashboardUrl,
                "Aboneliğini Settings sayfasından yönetebilir, istediğin zaman iptal edebilirsin. " +
                "Sorularınız için destek@vitrin.com.tr adresine yazabilirsiniz."),
            $"{tierLabel} aboneliğin aktif. Yenileme: {periodEndStr}. Dashboard: {dashboardUrl}",
            dashboardUrl,
            cancellationToken);
    }

    public Task<bool> SendSubscriptionCanceledAsync(User user, string tier, DateTime periodEnd, CancellationToken cancellationToken)
    {
        var pricingUrl = $"{GetAppBaseUrl()}/pricing";
        var tierLabel = tier == "ProMaker" ? "Pro Maker 🏆" : "Enterprise 💎";
        var periodEndStr = periodEnd.ToString("d MMMM yyyy", new System.Globalization.CultureInfo("tr-TR"));

        return SendAsync(
            user.Email,
            "Abonelik iptali alındı",
            EmailLayout(
                user.FullName ?? user.Username,
                "Abonelik iptalin onaylandı",
                $"{tierLabel} aboneliğin iptal edildi. " +
                $"Premium özelliklerine {periodEndStr} tarihine kadar erişmeye devam edebilirsin. " +
                $"Bu tarihten sonra ücretsiz plana geçeceksin. " +
                $"Fikrin değişirse istediğin zaman tekrar abone olabilirsin.",
                "Tekrar Abone Ol",
                pricingUrl,
                $"Aboneliğin sona erdiğinde ürün gönderme sınırın 5 adete düşecek. " +
                $"Verilerini kaybetmeyeceksin."),
            $"Aboneliğin {periodEndStr} tarihine kadar aktif. Yeniden abone olmak için: {pricingUrl}",
            pricingUrl,
            cancellationToken);
    }

    public Task<bool> SendPaymentFailedAsync(User user, string tier, int retryCount, DateTime? nextRetryAt, CancellationToken cancellationToken)
    {
        var settingsUrl = $"{GetAppBaseUrl()}/settings";
        var tierLabel = tier == "ProMaker" ? "Pro Maker 🏆" : "Enterprise 💎";

        string retryInfo;
        if (nextRetryAt.HasValue)
        {
            var retryDateStr = nextRetryAt.Value.ToString("d MMMM yyyy", new System.Globalization.CultureInfo("tr-TR"));
            retryInfo = $"Ödeme {retryDateStr} tarihinde otomatik olarak tekrar deneyecek (deneme {retryCount}/3). " +
                        "Bu tarihe kadar kart bilgilerini güncelleyebilirsin.";
        }
        else
        {
            retryInfo = "Tüm yenileme denemeleri başarısız oldu. Aboneliğin ücretsiz plana geçirildi. " +
                        "Yeniden abone olmak için fiyatlandırma sayfasını ziyaret edebilirsin.";
        }

        return SendAsync(
            user.Email,
            "⚠️ Ödeme başarısız — aboneliğin risk altında",
            EmailLayout(
                user.FullName ?? user.Username,
                "Ödemen alınamadı",
                $"{tierLabel} aboneliğin için ödeme alınamadı. " + retryInfo,
                "Ödeme Bilgilerini Güncelle",
                settingsUrl,
                "Yardım için destek@vitrin.com.tr adresine yazabilirsiniz."),
            $"Aboneliğin için ödeme alınamadı. {retryInfo} Güncelle: {settingsUrl}",
            settingsUrl,
            cancellationToken);
    }

    public Task<bool> SendSubscriptionRenewedAsync(User user, string tier, DateTime newPeriodEnd, decimal paidAmount, CancellationToken cancellationToken)
    {
        var dashboardUrl = $"{GetAppBaseUrl()}/dashboard";
        var tierLabel = tier == "ProMaker" ? "Pro Maker 🏆" : "Enterprise 💎";
        var periodEndStr = newPeriodEnd.ToString("d MMMM yyyy", new System.Globalization.CultureInfo("tr-TR"));
        var amountStr = paidAmount.ToString("N2", new System.Globalization.CultureInfo("tr-TR"));

        return SendAsync(
            user.Email,
            $"✅ {tierLabel} aboneliğin yenilendi",
            EmailLayout(
                user.FullName ?? user.Username,
                "Aboneliğin başarıyla yenilendi!",
                $"{tierLabel} aboneliğin ₺{amountStr} tutarında başarıyla yenilendi. " +
                $"Bir sonraki yenileme tarihin: {periodEndStr}. " +
                "Premium özelliklerine kesintisiz erişmeye devam ediyorsun.",
                "Dashboard'a Git",
                dashboardUrl,
                "Aboneliğini Settings sayfasından yönetebilir, istediğin zaman iptal edebilirsin."),
            $"{tierLabel} aboneliğin yenilendi. Tutar: ₺{amountStr}. Sonraki yenileme: {periodEndStr}. Dashboard: {dashboardUrl}",
            dashboardUrl,
            cancellationToken);
    }

    public Task<bool> SendSubscriptionExpiredAsync(User user, string tier, CancellationToken cancellationToken)
    {
        var pricingUrl = $"{GetAppBaseUrl()}/pricing";
        var tierLabel = tier == "ProMaker" ? "Pro Maker 🏆" : "Enterprise 💎";

        return SendAsync(
            user.Email,
            "Aboneliğin sona erdi",
            EmailLayout(
                user.FullName ?? user.Username,
                "Aboneliğin sona erdi",
                $"{tierLabel} aboneliğin sona erdi ve ücretsiz plana geçirildi. " +
                "Ürünlerin ve verilerinin kaybolmayacak; ancak premium özelliklerine erişimin kısıtlandı. " +
                "İstediğin zaman yeniden abone olarak tam erişimi geri kazanabilirsin.",
                "Yeniden Abone Ol",
                pricingUrl,
                "Sorularınız için destek@vitrin.com.tr adresine yazabilirsiniz."),
            $"{tierLabel} aboneliğin sona erdi, ücretsiz plana geçildi. Yeniden abone ol: {pricingUrl}",
            pricingUrl,
            cancellationToken);
    }

    public Task<bool> SendSubscriptionRenewalReminderAsync(User user, string tier, DateTime periodEnd, CancellationToken cancellationToken)
    {
        var settingsUrl = $"{GetAppBaseUrl()}/settings";
        var tierLabel = tier == "ProMaker" ? "Pro Maker 🏆" : "Enterprise 💎";
        var tierPrice = tier == "ProMaker" ? "₺299" : "₺999";
        var periodEndStr = periodEnd.ToString("d MMMM yyyy", new System.Globalization.CultureInfo("tr-TR"));

        return SendAsync(
            user.Email,
            $"⏰ {tierLabel} aboneliğin 3 gün içinde yenileniyor",
            EmailLayout(
                user.FullName ?? user.Username,
                "Aboneliğin yakında yenileniyor",
                $"{tierLabel} aboneliğin {periodEndStr} tarihinde {tierPrice} olarak otomatik yenilenecek. " +
                "Her şey yolundaysa herhangi bir işlem yapmanıza gerek yok. " +
                "Aboneliğinizi iptal etmek istiyorsanız yenileme tarihinden önce ayarlar sayfasından iptal edebilirsiniz.",
                "Abonelik Ayarları",
                settingsUrl,
                "Sorularınız için destek@vitrin.com.tr adresine yazabilirsiniz."),
            $"{tierLabel} aboneliğin {periodEndStr} tarihinde {tierPrice} olarak yenileniyor. Ayarlar: {settingsUrl}",
            settingsUrl,
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
