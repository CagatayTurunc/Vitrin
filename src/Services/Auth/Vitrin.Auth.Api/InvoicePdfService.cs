using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Vitrin.Auth.Api;

/// <summary>
/// Ödeme kaydından fatura PDF'i oluşturur.
/// QuestPDF Community lisansı — ticari kullanım için ücretsiz.
/// </summary>
public static class InvoicePdfService
{
    static InvoicePdfService()
    {
        // QuestPDF Community lisansını kaydet
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Generate(InvoiceData data)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(10).FontColor("#1a1a2e"));

                page.Header().Element(ComposeHeader);
                page.Content().Element(c => ComposeContent(c, data));
                page.Footer().Element(ComposeFooter);
            });
        }).GeneratePdf();
    }

    private static void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            // Sol: Logo + Şirket adı
            row.RelativeItem().Column(col =>
            {
                col.Item().Text("Vitrin")
                    .Bold().FontSize(24).FontColor("#00c97a");
                col.Item().Text("vitrin.it.com")
                    .FontSize(10).FontColor("#6b7280");
            });

            // Sağ: FATURA başlığı
            row.ConstantItem(160).AlignRight().Column(col =>
            {
                col.Item().Text("FATURA")
                    .Bold().FontSize(20).FontColor("#1a1a2e");
                col.Item().Height(4);
                col.Item().Text(t =>
                {
                    t.Span("Fatura No: ").SemiBold();
                    t.Span($"INV-{DateTime.UtcNow.Year}-{DateTime.UtcNow:MMdd}");
                });
            });
        });

        container.PaddingTop(8).LineHorizontal(1).LineColor("#e5e7eb");
    }

    private static void ComposeContent(IContainer container, InvoiceData data)
    {
        container.PaddingVertical(24).Column(col =>
        {
            // Fatura Bilgileri + Müşteri Bilgileri — yan yana
            col.Item().Row(row =>
            {
                // Fatura detayları
                row.RelativeItem().Column(info =>
                {
                    info.Item().Text("Fatura Bilgileri")
                        .SemiBold().FontSize(11).FontColor("#374151");
                    info.Item().Height(6);
                    info.Item().Text(t =>
                    {
                        t.Span("Ödeme Tarihi: ").SemiBold();
                        t.Span(data.PaymentDate.ToString("dd MMMM yyyy",
                            new System.Globalization.CultureInfo("tr-TR")));
                    });
                    info.Item().Text(t =>
                    {
                        t.Span("İşlem No: ").SemiBold();
                        t.Span(data.IyzicoPaymentId ?? "-");
                    });
                    info.Item().Text(t =>
                    {
                        t.Span("Durum: ").SemiBold();
                        t.Span("Ödendi").FontColor("#16a34a");
                    });
                });

                row.ConstantItem(20);

                // Müşteri bilgileri
                row.RelativeItem().Column(customer =>
                {
                    customer.Item().Text("Faturalanan")
                        .SemiBold().FontSize(11).FontColor("#374151");
                    customer.Item().Height(6);
                    customer.Item().Text(data.UserFullName).SemiBold();
                    customer.Item().Text(data.UserEmail).FontColor("#6b7280");
                });
            });

            col.Item().Height(28);

            // Ürün tablosu
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.RelativeColumn(5); // Açıklama
                    cols.RelativeColumn(2); // Dönem
                    cols.RelativeColumn(1); // Adet
                    cols.RelativeColumn(2); // Tutar
                });

                // Tablo başlığı
                table.Header(header =>
                {
                    static IContainer HeaderCell(IContainer c) =>
                        c.Background("#f3f4f6").Padding(8).BorderBottom(1).BorderColor("#d1d5db");

                    header.Cell().Element(HeaderCell).Text("Ürün / Hizmet").SemiBold();
                    header.Cell().Element(HeaderCell).Text("Dönem").SemiBold();
                    header.Cell().Element(HeaderCell).AlignCenter().Text("Adet").SemiBold();
                    header.Cell().Element(HeaderCell).AlignRight().Text("Tutar").SemiBold();
                });

                static IContainer BodyCell(IContainer c) =>
                    c.BorderBottom(1).BorderColor("#f3f4f6").Padding(8);

                // Satır
                table.Cell().Element(BodyCell).Text($"Vitrin {data.TierLabel} Aboneliği");
                table.Cell().Element(BodyCell).Text(data.BillingPeriod).FontColor("#6b7280");
                table.Cell().Element(BodyCell).AlignCenter().Text("1");
                table.Cell().Element(BodyCell).AlignRight()
                    .Text($"₺{data.OriginalAmount:N2}").SemiBold();

                // İndirim satırı (varsa)
                if (data.DiscountAmount > 0)
                {
                    table.Cell().Element(BodyCell).Text($"İndirim ({data.CouponCode})").FontColor("#16a34a");
                    table.Cell().Element(BodyCell).Text("-").FontColor("#6b7280");
                    table.Cell().Element(BodyCell).AlignCenter().Text("-");
                    table.Cell().Element(BodyCell).AlignRight()
                        .Text($"-₺{data.DiscountAmount:N2}").FontColor("#16a34a");
                }
            });

            col.Item().Height(16);

            // Toplam
            col.Item().AlignRight().Width(240).Column(totals =>
            {
                if (data.DiscountAmount > 0)
                {
                    totals.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Ara Toplam:").FontColor("#6b7280");
                        r.ConstantItem(100).AlignRight()
                            .Text($"₺{data.OriginalAmount:N2}").FontColor("#6b7280");
                    });

                    totals.Item().Row(r =>
                    {
                        r.RelativeItem().Text("İndirim:").FontColor("#16a34a");
                        r.ConstantItem(100).AlignRight()
                            .Text($"-₺{data.DiscountAmount:N2}").FontColor("#16a34a");
                    });

                    totals.Item().Height(4).LineHorizontal(1).LineColor("#e5e7eb");
                }

                totals.Item().Background("#f9fafb").Padding(8).Row(r =>
                {
                    r.RelativeItem().Text("TOPLAM:").Bold().FontSize(13);
                    r.ConstantItem(100).AlignRight()
                        .Text($"₺{data.PaidAmount:N2}").Bold().FontSize(13).FontColor("#00c97a");
                });

                totals.Item().PaddingTop(4).Text("KDV dahil (KDV oranı: %18)")
                    .FontSize(8).FontColor("#9ca3af");
            });

            col.Item().Height(32);

            // Notlar
            col.Item().Background("#f9fafb").Border(1).BorderColor("#e5e7eb")
                .Padding(12).Column(notes =>
            {
                notes.Item().Text("Notlar").SemiBold().FontColor("#374151");
                notes.Item().Height(4);
                notes.Item().Text(
                    "• Bu fatura elektronik olarak oluşturulmuştur.\n" +
                    "• Abonelik dönemleri UTC zaman dilimine göre hesaplanır.\n" +
                    "• Sorularınız için destek@vitrin.com.tr adresine yazabilirsiniz."
                ).FontColor("#6b7280").LineHeight(1.6f);
            });
        });
    }

    private static void ComposeFooter(IContainer container)
    {
        container.BorderTop(1).BorderColor("#e5e7eb").PaddingTop(8).Row(row =>
        {
            row.RelativeItem().Text("Vitrin — vitrin.it.com")
                .FontSize(8).FontColor("#9ca3af");
            row.ConstantItem(100).AlignRight().Text(t =>
            {
                t.Span("Sayfa ").FontSize(8).FontColor("#9ca3af");
                t.CurrentPageNumber().FontSize(8).FontColor("#9ca3af");
                t.Span(" / ").FontSize(8).FontColor("#9ca3af");
                t.TotalPages().FontSize(8).FontColor("#9ca3af");
            });
        });
    }
}

public sealed record InvoiceData(
    string UserFullName,
    string UserEmail,
    string TierLabel,
    string BillingPeriod,
    decimal OriginalAmount,
    decimal DiscountAmount,
    decimal PaidAmount,
    string? CouponCode,
    DateTime PaymentDate,
    string? IyzicoPaymentId);
