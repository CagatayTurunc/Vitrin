using Vitrin.Product.Domain.Entities;

namespace Vitrin.Product.Infrastructure.Data;

internal static class ProductCategorySeedData
{
    private static readonly Guid Engineering = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid Productivity = Guid.Parse("10000000-0000-0000-0000-000000000002");
    private static readonly Guid Marketing = Guid.Parse("10000000-0000-0000-0000-000000000003");
    private static readonly Guid Design = Guid.Parse("10000000-0000-0000-0000-000000000004");
    private static readonly Guid Finance = Guid.Parse("10000000-0000-0000-0000-000000000005");
    private static readonly Guid Community = Guid.Parse("10000000-0000-0000-0000-000000000006");
    private static readonly Guid Ai = Guid.Parse("10000000-0000-0000-0000-000000000007");

    public static readonly ProductCategory[] All =
    {
        Create(Engineering, "Mühendislik ve Geliştirme", "muhendislik-gelistirme", "Yazılım geliştirme, altyapı, API, test ve DevOps ürünleri.", null, 10),
        Create(Productivity, "Üretkenlik", "uretkenlik", "Bireysel ve ekip üretkenliğini artıran araçlar.", null, 20),
        Create(Marketing, "Pazarlama ve Satış", "pazarlama-satis", "Müşteri kazanımı, satış ve büyüme ürünleri.", null, 30),
        Create(Design, "Tasarım ve Yaratıcılık", "tasarim-yaraticilik", "Tasarım, içerik ve yaratıcı üretim araçları.", null, 40),
        Create(Finance, "Finans ve İşletme", "finans-isletme", "Finans, muhasebe ve işletme operasyonu ürünleri.", null, 50),
        Create(Community, "Sosyal ve Topluluk", "sosyal-topluluk", "Topluluk, iletişim ve profesyonel ağ ürünleri.", null, 60),
        Create(Ai, "Yapay Zekâ", "yapay-zeka", "Yapay zekâ tabanlı ürünler, modeller ve ajanlar.", null, 70),

        Create(Guid.Parse("20000000-0000-0000-0000-000000000001"), "Geliştirici Araçları", "gelistirici-araclari", "Kodlama ve geliştirici deneyimi araçları.", Engineering, 10),
        Create(Guid.Parse("20000000-0000-0000-0000-000000000002"), "API Araçları", "api-araclari", "API geliştirme, test ve entegrasyon araçları.", Engineering, 20),
        Create(Guid.Parse("20000000-0000-0000-0000-000000000003"), "DevOps", "devops", "Dağıtım, gözlemlenebilirlik ve altyapı araçları.", Engineering, 30),
        Create(Guid.Parse("20000000-0000-0000-0000-000000000004"), "Ekip İş Birliği", "ekip-is-birligi", "Takımlar için iletişim ve iş birliği ürünleri.", Productivity, 10),
        Create(Guid.Parse("20000000-0000-0000-0000-000000000005"), "Proje Yönetimi", "proje-yonetimi", "Planlama, görev ve proje yönetimi ürünleri.", Productivity, 20),
        Create(Guid.Parse("20000000-0000-0000-0000-000000000006"), "CRM", "crm", "Müşteri ilişkileri ve satış süreçleri araçları.", Marketing, 10),
        Create(Guid.Parse("20000000-0000-0000-0000-000000000007"), "Pazarlama Otomasyonu", "pazarlama-otomasyonu", "Kampanya ve büyüme otomasyonu araçları.", Marketing, 20),
        Create(Guid.Parse("20000000-0000-0000-0000-000000000008"), "Grafik Tasarım", "grafik-tasarim", "Grafik ve görsel tasarım ürünleri.", Design, 10),
        Create(Guid.Parse("20000000-0000-0000-0000-000000000009"), "Video ve Ses", "video-ses", "Video, ses ve medya üretim araçları.", Design, 20),
        Create(Guid.Parse("20000000-0000-0000-0000-000000000010"), "Ön Muhasebe", "on-muhasebe", "Fatura, gider ve ön muhasebe ürünleri.", Finance, 10),
        Create(Guid.Parse("20000000-0000-0000-0000-000000000011"), "Fintech", "fintech", "Ödeme, bankacılık ve finansal teknoloji ürünleri.", Finance, 20),
        Create(Guid.Parse("20000000-0000-0000-0000-000000000012"), "Topluluk Yönetimi", "topluluk-yonetimi", "Topluluk kurma ve yönetme araçları.", Community, 10),
        Create(Guid.Parse("20000000-0000-0000-0000-000000000013"), "AI Ajanları", "ai-ajanlari", "Görevleri otonom veya yarı otonom yürüten ajanlar.", Ai, 10),
        Create(Guid.Parse("20000000-0000-0000-0000-000000000014"), "Üretken AI", "uretken-ai", "Metin, görsel, video ve kod üreten yapay zekâ ürünleri.", Ai, 20)
    };

    private static ProductCategory Create(Guid id, string name, string slug, string description, Guid? parentId, int sortOrder)
        => ProductCategory.Create(name, slug, description, parentId, sortOrder, id);
}
