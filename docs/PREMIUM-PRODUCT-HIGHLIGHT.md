# ✨ Premium Product Highlighting — Tasarım Dokümanı

> Pro ve Enterprise kullanıcıların ürünlerini görsel olarak öne çıkarma

---

## 🎨 Görsel Tasarım

### 1. Ürün Kartı Badge'leri

```tsx
// Normal kullanıcı ürünü
┌─────────────────────────┐
│ [Thumbnail]             │
│ Product Name            │
│ ⬆ 45  💬 12            │
└─────────────────────────┘

// Pro Maker ürünü
┌─────────────────────────┐
│ 🏆 PRO  [Thumbnail]     │  ← Sol üst köşede badge
│ Product Name            │
│ ⬆ 45  💬 12            │
│ ─────────────────────── │  ← Altında gradient border
└─────────────────────────┘

// Enterprise ürünü
┌─────────────────────────┐
│ 💎 FEATURED [Thumbnail] │  ← Daha büyük badge
│ Product Name            │
│ ⬆ 45  💬 12            │
│ ━━━━━━━━━━━━━━━━━━━━━━━│  ← Kalın gold border
│ 🔥 TRENDING             │  ← Ekstra featured tag
└─────────────────────────┘
```

### 2. Gradient Border Renkleri

```css
/* Pro Maker — Blue gradient */
background: linear-gradient(
  135deg,
  #667eea 0%,
  #764ba2 100%
);

/* Enterprise — Gold gradient */
background: linear-gradient(
  135deg,
  #f093fb 0%,
  #f5576c 100%
);

/* Free — Normal gray */
border: 1px solid #e5e7eb;
```

### 3. Glow Effect (Hover)

```css
/* Pro hover effect */
.product-card-pro:hover {
  box-shadow: 0 20px 60px rgba(102, 126, 234, 0.4);
  transform: translateY(-4px);
}

/* Enterprise hover effect */
.product-card-enterprise:hover {
  box-shadow: 0 25px 80px rgba(245, 87, 108, 0.5);
  transform: translateY(-6px);
}
```

---

## 🏗️ Backend Implementation

### 1. Product Response'a MakerTier Ekleme

```csharp
// src/Services/Product/Vitrin.Product.Application/Queries/GetProductsQuery.cs

public record ProductCardResponse(
    Guid Id,
    string Name,
    string Slug,
    string Tagline,
    string ThumbnailUrl,
    int UpvoteCount,
    int CommentCount,
    DateTime PublishedAt,
    
    // Maker bilgileri
    Guid MakerId,
    string MakerName,
    string MakerAvatarUrl,
    SubscriptionTier MakerTier  // ← YENİ: Maker'ın subscription tier'ı
);
```

### 2. Query'de JOIN ile Tier Bilgisini Alma

```csharp
// ProductQueryHandler.cs içinde

var products = await _context.Products
    .AsNoTracking()
    .Where(p => p.Status == ProductStatus.Published)
    .OrderByDescending(p => p.PublishedAt)
    .Select(p => new ProductCardResponse(
        p.Id,
        p.Name,
        p.Slug,
        p.Tagline,
        p.ThumbnailUrl,
        p.UpvoteCount,
        p.CommentCount,
        p.PublishedAt,
        p.MakerId,
        p.Maker.FullName,
        p.Maker.AvatarUrl,
        p.Maker.Subscription.Tier  // ← Auth service'ten join
    ))
    .Take(20)
    .ToListAsync(ct);
```

**Problem:** Product service, Auth service'in Subscription tablosuna erişemez (mikroservis boundary).

**Çözüm 1: Denormalization** (Önerilen)
```csharp
// ProductItem entity'ye ekle
public class ProductItem
{
    // ... mevcut fieldlar
    
    // Denormalized maker info
    public SubscriptionTier MakerTierSnapshot { get; private set; }
    
    public void UpdateMakerTier(SubscriptionTier newTier)
    {
        MakerTierSnapshot = newTier;
    }
}
```

**Event-driven güncelleme:**
```csharp
// Auth service → Kafka event
public class SubscriptionUpgradedEvent
{
    public Guid UserId { get; init; }
    public SubscriptionTier OldTier { get; init; }
    public SubscriptionTier NewTier { get; init; }
}

// Product service → Event consumer
public class SubscriptionUpgradedEventHandler
{
    public async Task Handle(SubscriptionUpgradedEvent evt)
    {
        // Maker'ın tüm ürünlerini güncelle
        var products = await _repo.GetProductsByMakerAsync(evt.UserId);
        
        foreach (var product in products)
        {
            product.UpdateMakerTier(evt.NewTier);
        }
        
        await _repo.SaveChangesAsync();
    }
}
```

---

## 🎯 Frontend Implementation

### 1. ProductCard Component Güncelleme

```tsx
// components/product-card.tsx

interface ProductCardProps {
  product: {
    id: string;
    name: string;
    tagline: string;
    thumbnailUrl: string;
    upvoteCount: number;
    commentCount: number;
    maker: {
      name: string;
      avatarUrl: string;
      tier: 'Free' | 'ProMaker' | 'Enterprise';
    };
  };
}

export function ProductCard({ product }: ProductCardProps) {
  const tierConfig = getTierConfig(product.maker.tier);
  
  return (
    <Link
      href={`/products/${product.id}`}
      className={cn(
        "group relative block rounded-lg overflow-hidden transition-all duration-300",
        tierConfig.cardClass
      )}
    >
      {/* Badge */}
      {tierConfig.badge && (
        <div className={tierConfig.badgeClass}>
          <span className="text-xs font-bold">{tierConfig.badge}</span>
        </div>
      )}
      
      {/* Thumbnail */}
      <div className="relative aspect-video overflow-hidden bg-gray-100">
        <Image
          src={product.thumbnailUrl}
          alt={product.name}
          fill
          className="object-cover transition-transform group-hover:scale-105"
        />
      </div>
      
      {/* Content */}
      <div className="p-4">
        <h3 className="font-semibold text-lg line-clamp-1">
          {product.name}
        </h3>
        <p className="text-sm text-gray-600 line-clamp-2 mt-1">
          {product.tagline}
        </p>
        
        {/* Stats */}
        <div className="flex items-center gap-4 mt-3 text-sm text-gray-500">
          <span>⬆ {product.upvoteCount}</span>
          <span>💬 {product.commentCount}</span>
        </div>
      </div>
      
      {/* Gradient border (for premium) */}
      {tierConfig.hasBorder && (
        <div className={tierConfig.borderClass} />
      )}
    </Link>
  );
}

function getTierConfig(tier: string) {
  switch (tier) {
    case 'ProMaker':
      return {
        badge: '🏆 PRO',
        badgeClass: 'absolute top-2 left-2 z-10 bg-gradient-to-r from-blue-500 to-purple-600 text-white px-3 py-1 rounded-full text-xs font-bold shadow-lg',
        cardClass: 'border-2 border-transparent bg-gradient-to-r from-blue-500/10 to-purple-600/10 hover:shadow-2xl hover:shadow-blue-500/20 hover:-translate-y-2',
        hasBorder: true,
        borderClass: 'absolute inset-0 rounded-lg bg-gradient-to-r from-blue-500 to-purple-600 opacity-50 blur-xl -z-10'
      };
    
    case 'Enterprise':
      return {
        badge: '💎 FEATURED',
        badgeClass: 'absolute top-2 left-2 z-10 bg-gradient-to-r from-yellow-400 to-pink-500 text-white px-3 py-1 rounded-full text-xs font-bold shadow-lg animate-pulse',
        cardClass: 'border-4 border-transparent bg-gradient-to-r from-yellow-400/20 to-pink-500/20 hover:shadow-2xl hover:shadow-pink-500/30 hover:-translate-y-3',
        hasBorder: true,
        borderClass: 'absolute inset-0 rounded-lg bg-gradient-to-r from-yellow-400 to-pink-500 opacity-60 blur-2xl -z-10'
      };
    
    default: // Free
      return {
        badge: null,
        badgeClass: '',
        cardClass: 'border border-gray-200 hover:shadow-lg hover:-translate-y-1',
        hasBorder: false,
        borderClass: ''
      };
  }
}
```

### 2. Tailwind Config Güncelleme

```js
// tailwind.config.ts

module.exports = {
  theme: {
    extend: {
      animation: {
        'pulse-slow': 'pulse 3s cubic-bezier(0.4, 0, 0.6, 1) infinite',
        'glow': 'glow 2s ease-in-out infinite alternate',
      },
      keyframes: {
        glow: {
          '0%': { boxShadow: '0 0 20px rgba(102, 126, 234, 0.5)' },
          '100%': { boxShadow: '0 0 40px rgba(102, 126, 234, 0.8)' },
        }
      }
    }
  }
}
```

---

## 🎯 Anasayfa Özel Yerleşim

### Featured Section (Sadece Enterprise)

```tsx
// app/page.tsx

export default async function HomePage() {
  const featuredProducts = await getEnterpriseProducts();
  const regularProducts = await getRegularProducts();
  
  return (
    <div>
      {/* Enterprise Featured Section */}
      {featuredProducts.length > 0 && (
        <section className="mb-12">
          <h2 className="text-2xl font-bold mb-6 flex items-center gap-2">
            💎 Featured Products
          </h2>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {featuredProducts.map(product => (
              <FeaturedProductCard key={product.id} product={product} />
            ))}
          </div>
        </section>
      )}
      
      {/* Regular Products */}
      <section>
        <h2 className="text-2xl font-bold mb-6">All Products</h2>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
          {regularProducts.map(product => (
            <ProductCard key={product.id} product={product} />
          ))}
        </div>
      </section>
    </div>
  );
}
```

---

## 🔍 Arama Sonuçlarında Öncelik

### Backend — Tier-based Sorting

```csharp
// GetProductsQuery handler'da

var products = await _context.Products
    .AsNoTracking()
    .Where(p => p.Status == ProductStatus.Published)
    .OrderByDescending(p => 
        // Enterprise en üstte
        p.MakerTierSnapshot == SubscriptionTier.Enterprise ? 3 :
        // Pro ikinci sırada
        p.MakerTierSnapshot == SubscriptionTier.ProMaker ? 2 :
        // Free en altta
        1
    )
    .ThenByDescending(p => p.TrendScore) // Sonra trend score
    .ThenByDescending(p => p.PublishedAt) // Son olarak tarih
    .ToListAsync(ct);
```

---

## 📊 Analytics Tracking

Premium ürünlerin performansını ölçmek için:

```tsx
// components/product-card.tsx

<Link
  href={`/products/${product.id}`}
  onClick={() => {
    // Track premium product click
    if (product.maker.tier !== 'Free') {
      analytics.track('premium_product_clicked', {
        productId: product.id,
        makerTier: product.maker.tier,
        position: index,
        section: 'featured'
      });
    }
  }}
>
  ...
</Link>
```

**Hedef metrikler:**
- Premium ürün tıklama oranı vs normal ürün
- Conversion: Premium badge gören kullanıcılar upgrade yapıyor mu?
- Featured section engagement

---

## 🎨 Örnek Görsel Varyantlar

### Varyant A: Subtle (Minimal)
- Badge: Küçük, sade
- Border: İnce gradient
- Hover: Hafif glow

### Varyant B: Bold (Dikkat çekici) — Önerilen
- Badge: Büyük, animasyonlu
- Border: Kalın gradient + glow
- Hover: Belirgin elevation + shadow

### Varyant C: Extreme (Çok fazla)
- Badge: Büyük + pulse animation
- Border: Kalın + animated gradient
- Hover: 3D tilt effect
- Background: Animated particles

**A/B Test:** Başlangıçta Varyant B ile başla, conversion'a göre ayarla.

---

## ✅ Implementation Checklist

### Backend
- [ ] `MakerTierSnapshot` field ekle (ProductItem entity)
- [ ] Migration: `AddMakerTierSnapshotToProducts`
- [ ] Kafka event: `SubscriptionUpgradedEvent`
- [ ] Event consumer: Maker ürünlerini güncelle
- [ ] Query'de `MakerTier` field'ını response'a ekle

### Frontend
- [ ] `ProductCard` component'i güncelle
- [ ] Tier-based styling config
- [ ] Badge component'leri
- [ ] Gradient border styles
- [ ] Hover animations
- [ ] Featured section (Enterprise için)
- [ ] Analytics tracking

### Testing
- [ ] Free user ürünü → normal görünüm
- [ ] Pro user upgrade → ürünler otomatik Pro badge alıyor
- [ ] Enterprise → Featured section'da görünüyor
- [ ] Arama sonuçlarında sıralama doğru
- [ ] Mobile responsive

---

## 🚀 Quick Start

1. **Backend migration çalıştır**
2. **Mevcut tüm ürünleri Free tier'a set et** (başlangıç)
3. **Event consumer'ı aktif et**
4. **Frontend component'i güncelle**
5. **Test et:** Bir kullanıcıyı manuel Pro'ya upgrade et, ürününü kontrol et

**Beklenen sonuç:** Pro kullanıcının ürünü 🏆 badge'li ve gradient border'lı görünmeli!
