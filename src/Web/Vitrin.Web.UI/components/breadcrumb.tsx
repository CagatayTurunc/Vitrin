import Link from "next/link";
import { ChevronRight, Home } from "lucide-react";

export interface BreadcrumbItem {
  label: string;
  href?: string; // Son eleman href almaz (mevcut sayfa)
}

interface BreadcrumbProps {
  items: BreadcrumbItem[];
  /** Schema.org BreadcrumbList JSON-LD otomatik eklenir */
  className?: string;
}

const siteUrl = process.env.NEXT_PUBLIC_SITE_URL ?? "https://vitrin.it.com";

/**
 * Breadcrumb bileşeni — hem görsel hem SEO (BreadcrumbList JSON-LD).
 *
 * Kullanım:
 * <Breadcrumb items={[
 *   { label: "Kategoriler", href: "/categories" },
 *   { label: "SaaS" }  // son eleman href almaz
 * ]} />
 *
 * Neden JSON-LD?
 * Google arama sonuçlarında URL yerine "Vitrin > Kategoriler > SaaS" şeklinde
 * breadcrumb gösterimi sağlar (rich result). Tıklama oranını artırır.
 */
export function Breadcrumb({ items, className }: BreadcrumbProps) {
  // Tüm adımlar: Ana Sayfa + verilen items
  const allItems = [{ label: "Ana Sayfa", href: "/" }, ...items];

  // Schema.org BreadcrumbList
  const jsonLd = {
    "@context": "https://schema.org",
    "@type": "BreadcrumbList",
    itemListElement: allItems.map((item, index) => ({
      "@type": "ListItem",
      position: index + 1,
      name: item.label,
      item: item.href ? `${siteUrl}${item.href}` : undefined,
    })),
  };

  return (
    <>
      {/* JSON-LD structured data */}
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{
          __html: JSON.stringify(jsonLd).replace(/</g, "\\u003c"),
        }}
      />

      {/* Görsel breadcrumb */}
      <nav aria-label="Sayfa yolu" className={className}>
        <ol className="flex flex-wrap items-center gap-1 text-sm text-muted-foreground">
          {allItems.map((item, index) => {
            const isLast = index === allItems.length - 1;
            return (
              <li key={index} className="flex items-center gap-1">
                {index === 0 && (
                  <Home className="h-3.5 w-3.5 shrink-0" aria-hidden="true" />
                )}
                {item.href && !isLast ? (
                  <Link
                    href={item.href}
                    className="hover:text-foreground transition-colors font-medium"
                  >
                    {item.label}
                  </Link>
                ) : (
                  <span
                    className={isLast ? "text-foreground font-semibold" : ""}
                    aria-current={isLast ? "page" : undefined}
                  >
                    {item.label}
                  </span>
                )}
                {!isLast && (
                  <ChevronRight className="h-3.5 w-3.5 shrink-0 text-muted-foreground/50" aria-hidden="true" />
                )}
              </li>
            );
          })}
        </ol>
      </nav>
    </>
  );
}
