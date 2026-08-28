/**
 * SEO Utilities — Bölüm 2 için
 * 
 * Meta tag, canonical URL, Open Graph, Schema.org helper fonksiyonları
 */

import type { Metadata } from 'next'

interface SEOConfig {
  title: string
  description: string
  path?: string
  image?: string
  noIndex?: boolean
  keywords?: string[]
  author?: string
  publishedTime?: string
  modifiedTime?: string
  type?: 'website' | 'article'
}

const siteUrl = process.env.NEXT_PUBLIC_SITE_URL ?? 'http://localhost:3001'
const siteName = 'Vitrin'
const defaultDescription =
  'Vitrin, en yeni ürünleri keşfedeceğin, oy vereceğin ve paylaşacağın ürün keşif platformu.'

/**
 * Madde 2.3: Canonical URL oluştur
 */
export function getCanonicalUrl(path: string): string {
  const cleanPath = path.startsWith('/') ? path : `/${path}`
  return `${siteUrl}${cleanPath}`
}

/**
 * Madde 2.6: Standart metadata oluştur
 * Tüm sayfalarda tutarlı SEO için
 */
export function generateSEO(config: SEOConfig): Metadata {
  const {
    title,
    description,
    path = '',
    image = `${siteUrl}/og-image.png`,
    noIndex = false,
    keywords = [],
    author,
    publishedTime,
    modifiedTime,
    type = 'website',
  } = config

  const canonical = getCanonicalUrl(path)
  const fullTitle = path === '' ? title : `${title} — ${siteName}`

  return {
    title: fullTitle,
    description,
    keywords: keywords.length > 0 ? keywords.join(', ') : undefined,
    authors: author ? [{ name: author }] : undefined,
    creator: siteName,
    publisher: siteName,
    alternates: {
      canonical,
    },
    // Madde 2.1: Robots meta tag
    robots: {
      index: !noIndex,
      follow: true,
      googleBot: {
        index: !noIndex,
        follow: true,
        'max-video-preview': -1,
        'max-image-preview': 'large',
        'max-snippet': -1,
      },
    },
    openGraph: {
      type,
      locale: 'tr_TR',
      url: canonical,
      title: fullTitle,
      description,
      siteName,
      images: [
        {
          url: image,
          width: 1200,
          height: 630,
          alt: title,
        },
      ],
      ...(publishedTime && { publishedTime }),
      ...(modifiedTime && { modifiedTime }),
    },
    twitter: {
      card: 'summary_large_image',
      title: fullTitle,
      description,
      images: [image],
      creator: '@vitrinapp',
      site: '@vitrinapp',
    },
  }
}

/**
 * Madde 2.2: URL'leri normalize et (www vs non-www)
 */
export function normalizeUrl(url: string): string {
  // www'siz versiyonu tercih et (canonical)
  return url.replace(/^(https?:\/\/)www\./, '$1')
}

/**
 * Madde 2.7: Organization Schema.org
 */
export function getOrganizationSchema() {
  return {
    '@context': 'https://schema.org',
    '@type': 'Organization',
    name: siteName,
    url: siteUrl,
    logo: `${siteUrl}/logo.png`,
    sameAs: [
      // Social media profilleri buraya
      'https://twitter.com/vitrinapp',
      'https://github.com/vitrinapp',
    ],
    contactPoint: {
      '@type': 'ContactPoint',
      email: 'info@vitrin.com',
      contactType: 'customer support',
      areaServed: 'TR',
      availableLanguage: ['Turkish'],
    },
  }
}

/**
 * Madde 2.7: Website Schema.org
 */
export function getWebSiteSchema() {
  return {
    '@context': 'https://schema.org',
    '@type': 'WebSite',
    name: siteName,
    url: siteUrl,
    description: defaultDescription,
    inLanguage: 'tr-TR',
    potentialAction: {
      '@type': 'SearchAction',
      target: {
        '@type': 'EntryPoint',
        urlTemplate: `${siteUrl}/search?q={search_term_string}`,
      },
      'query-input': 'required name=search_term_string',
    },
  }
}

/**
 * Madde 2.7: Article/BlogPosting Schema.org
 */
export function getArticleSchema(article: {
  title: string
  description: string
  url: string
  image?: string
  author?: string
  publishedTime: string
  modifiedTime?: string
}) {
  return {
    '@context': 'https://schema.org',
    '@type': 'Article',
    headline: article.title,
    description: article.description,
    url: article.url,
    image: article.image || `${siteUrl}/og-image.png`,
    author: {
      '@type': 'Person',
      name: article.author || siteName,
    },
    publisher: {
      '@type': 'Organization',
      name: siteName,
      logo: {
        '@type': 'ImageObject',
        url: `${siteUrl}/logo.png`,
      },
    },
    datePublished: article.publishedTime,
    dateModified: article.modifiedTime || article.publishedTime,
    inLanguage: 'tr-TR',
  }
}

/**
 * Madde 2.6: Meta description'ı optimize et
 * - 150-160 karakter arası
 * - Anahtar kelime içermeli
 * - Call-to-action olmalı
 */
export function optimizeDescription(text: string, maxLength = 160): string {
  const cleaned = text.replace(/\s+/g, ' ').trim()

  if (cleaned.length <= maxLength) {
    return cleaned
  }

  // Son cümleyi tamamla
  const truncated = cleaned.slice(0, maxLength - 1)
  const lastSpace = truncated.lastIndexOf(' ')

  return lastSpace > maxLength - 20
    ? `${truncated.slice(0, lastSpace)}…`
    : `${truncated}…`
}

/**
 * Madde 2.5: Anahtar kelime yoğunluğunu hesapla
 */
export function calculateKeywordDensity(
  content: string,
  keyword: string
): number {
  const words = content.toLowerCase().split(/\s+/)
  const keywordWords = keyword.toLowerCase().split(/\s+/)
  const keywordLength = keywordWords.length

  let count = 0
  for (let i = 0; i <= words.length - keywordLength; i++) {
    const phrase = words.slice(i, i + keywordLength).join(' ')
    if (phrase === keywordWords.join(' ')) {
      count++
    }
  }

  return (count / words.length) * 100
}

/**
 * Madde 2.2: Duplicate content checker
 */
export function generateContentHash(content: string): string {
  // Basit hash fonksiyonu (production'da crypto kullanın)
  let hash = 0
  for (let i = 0; i < content.length; i++) {
    const char = content.charCodeAt(i)
    hash = (hash << 5) - hash + char
    hash = hash & hash
  }
  return hash.toString(36)
}

/**
 * JSON-LD script tag'i için güvenli render
 */
export function renderJsonLd(data: object) {
  return {
    __html: JSON.stringify(data).replace(/</g, '\\u003c'),
  }
}
