import type { Metadata } from "next";
import { notFound } from "next/navigation";
import type { ProductDetailApiModel } from "@/core/domain/product.types";
import { ProductDetailClient } from "@/components/product-detail-client";
import { serverApiFetch } from "@/lib/server-api";

type Props = { params: Promise<{ slug: string }> };
const siteUrl = process.env.NEXT_PUBLIC_SITE_URL ?? "http://localhost:3001";

async function getProduct(slug: string) {
  return serverApiFetch<ProductDetailApiModel>(`/products/${encodeURIComponent(slug)}`, {
    revalidate: 60,
    tags: [`product-${slug}`],
  });
}

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { slug } = await params;
  const product = await getProduct(slug);
  if (!product) return { title: "Ürün bulunamadı — Vitrin", robots: { index: false, follow: false } };

  const title = `${product.name} — ${product.tagline}`;
  const description = compactDescription(product.description || product.tagline);
  const canonical = `${siteUrl}/product/${product.slug}`;
  const image = product.thumbnailUrl || `${siteUrl}/icon.svg`;

  return {
    title,
    description,
    alternates: { canonical },
    openGraph: {
      type: "website",
      locale: "tr_TR",
      siteName: "Vitrin",
      title,
      description,
      url: canonical,
      images: [{ url: image, alt: `${product.name} ürün görseli` }],
    },
    twitter: {
      card: "summary_large_image",
      title,
      description,
      images: [image],
    },
  };
}

export default async function ProductPage({ params }: Props) {
  const { slug } = await params;
  const product = await getProduct(slug);
  if (!product) notFound();

  const canonical = `${siteUrl}/product/${product.slug}`;
  const jsonLd = {
    "@context": "https://schema.org",
    "@type": "SoftwareApplication",
    name: product.name,
    description: compactDescription(product.description || product.tagline, 500),
    url: canonical,
    image: [product.thumbnailUrl, ...(product.galleryUrls ?? [])].filter(Boolean),
    applicationCategory: product.categories?.map(category => category.name).join(", ") || "BusinessApplication",
    operatingSystem: "Web",
    author: { "@type": "Person", identifier: product.makerId },
    interactionStatistic: [
      { "@type": "InteractionCounter", interactionType: "https://schema.org/LikeAction", userInteractionCount: product.upvotes ?? 0 },
      { "@type": "InteractionCounter", interactionType: "https://schema.org/CommentAction", userInteractionCount: product.commentCount ?? 0 },
      { "@type": "InteractionCounter", interactionType: "https://schema.org/ViewAction", userInteractionCount: product.viewCount ?? 0 },
    ],
    datePublished: product.activeLaunch?.publishedAtUtc ?? product.publishedAt,
    inLanguage: "tr-TR",
  };

  return <>
    <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: JSON.stringify(jsonLd).replace(/</g, "\\u003c") }} />
    <ProductDetailClient slug={slug} initialProduct={product} />
  </>;
}

function compactDescription(value: string, maxLength = 180) {
  const compact = value.replace(/\s+/g, " ").trim();
  return compact.length <= maxLength ? compact : `${compact.slice(0, maxLength - 1).trimEnd()}…`;
}
