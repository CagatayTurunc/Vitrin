/**
 * Marketing Utilities — Bölüm 4
 * 
 * USP, CTA optimization, conversion tracking
 */

/**
 * Madde 4.1: USP (Unique Selling Proposition) variants
 */
export const USP_VARIANTS = {
  default: {
    headline: 'Yeni fikirlere vitrin aç',
    subheadline:
      "Türkiye'nin en heyecan verici ürünlerini keşfet, destekle ve topluluğun bir sonraki favorisini birlikte öne çıkar.",
    cta: 'Ürünleri keşfet',
  },
  makerFocused: {
    headline: 'Ürününü binlerce insana ulaştır',
    subheadline:
      'Türkiye\'nin en aktif maker topluluğunda ürününü lansmanla, geri bildirim al ve büyü.',
    cta: 'Ürününü ekle',
  },
  communityFocused: {
    headline: 'Topluluk karar verir',
    subheadline:
      'Algoritma yok, sadece gerçek oylar. En iyi ürünleri sen ve topluluk belirliyor.',
    cta: 'Topluluğa katıl',
  },
  discoveryFocused: {
    headline: "Türkiye'den dünya'ya ürünler",
    subheadline:
      'Her gün yeni bir ürün keşfet, ilham al ve yerli ekosistemi destekle.',
    cta: 'Keşfetmeye başla',
  },
}

/**
 * Madde 4.1: Target audience segments
 */
export type AudienceSegment = 'maker' | 'early-adopter' | 'enthusiast' | 'investor'

export function getUSPForAudience(segment: AudienceSegment) {
  switch (segment) {
    case 'maker':
      return USP_VARIANTS.makerFocused
    case 'early-adopter':
      return USP_VARIANTS.discoveryFocused
    case 'enthusiast':
      return USP_VARIANTS.communityFocused
    case 'investor':
      return USP_VARIANTS.default
    default:
      return USP_VARIANTS.default
  }
}

/**
 * Madde 4.2: Campaign URL builder
 */
export function buildCampaignURL(
  baseURL: string,
  params: {
    source: string
    medium: string
    campaign: string
    term?: string
    content?: string
  }
): string {
  const url = new URL(baseURL)
  
  url.searchParams.set('utm_source', params.source)
  url.searchParams.set('utm_medium', params.medium)
  url.searchParams.set('utm_campaign', params.campaign)
  
  if (params.term) {
    url.searchParams.set('utm_term', params.term)
  }
  
  if (params.content) {
    url.searchParams.set('utm_content', params.content)
  }
  
  return url.toString()
}

/**
 * Madde 4.2: Common campaign URLs
 */
export const CAMPAIGN_URLS = {
  productHunt: buildCampaignURL('https://vitrin.com', {
    source: 'producthunt',
    medium: 'referral',
    campaign: 'launch',
  }),
  hackerNews: buildCampaignURL('https://vitrin.com', {
    source: 'hackernews',
    medium: 'social',
    campaign: 'launch',
  }),
  twitter: buildCampaignURL('https://vitrin.com', {
    source: 'twitter',
    medium: 'social',
    campaign: 'organic',
  }),
  linkedIn: buildCampaignURL('https://vitrin.com', {
    source: 'linkedin',
    medium: 'social',
    campaign: 'organic',
  }),
  newsletter: buildCampaignURL('https://vitrin.com', {
    source: 'newsletter',
    medium: 'email',
    campaign: 'weekly_roundup',
  }),
}

/**
 * Madde 4.3: Email subject line optimizer
 */
export function optimizeSubjectLine(subject: string): {
  score: number
  suggestions: string[]
  optimized: string
} {
  const suggestions: string[] = []
  let score = 100
  
  // Length check
  if (subject.length < 30) {
    score -= 10
    suggestions.push('Subject çok kısa (30+ karakter önerilir)')
  }
  if (subject.length > 60) {
    score -= 15
    suggestions.push('Subject çok uzun (60 karakter altı önerilir)')
  }
  
  // Emoji check
  if (!/[\u{1F300}-\u{1F9FF}]/u.test(subject)) {
    score -= 5
    suggestions.push('Emoji kullanımı open rate'i artırabilir')
  }
  
  // Personalization
  if (!subject.includes('[İsim]') && !subject.includes('sen')) {
    score -= 10
    suggestions.push('Kişiselleştirme ekleyin ([İsim] veya "sen")')
  }
  
  // Action words
  const actionWords = ['keşfet', 'gör', 'öğren', 'kazan', 'başla']
  if (!actionWords.some((word) => subject.toLowerCase().includes(word))) {
    score -= 10
    suggestions.push('Aksiyon kelimesi ekleyin (keşfet, gör, başla...)')
  }
  
  // Urgency
  if (!/(bugün|şimdi|son|acele|sınırlı)/i.test(subject)) {
    suggestions.push('Aciliyet hissi ekleyebilirsiniz')
  }
  
  // Optimize et
  let optimized = subject
  if (subject.length > 60) {
    optimized = subject.slice(0, 57) + '...'
  }
  if (!/[\u{1F300}-\u{1F9FF}]/u.test(optimized)) {
    optimized = `🚀 ${optimized}`
  }
  
  return {
    score: Math.max(0, score),
    suggestions,
    optimized,
  }
}

/**
 * Madde 4.3: A/B Test variant selector
 */
export function selectABVariant<T>(
  variants: T[],
  userId?: string
): T {
  if (variants.length === 0) {
    throw new Error('At least one variant required')
  }
  
  if (variants.length === 1) {
    return variants[0]
  }
  
  // Consistent variant for same user
  if (userId) {
    const hash = userId.split('').reduce((acc, char) => {
      return acc + char.charCodeAt(0)
    }, 0)
    return variants[hash % variants.length]
  }
  
  // Random for anonymous
  return variants[Math.floor(Math.random() * variants.length)]
}

/**
 * Madde 4.3: Conversion rate calculator
 */
export function calculateConversionRate(
  conversions: number,
  visitors: number
): number {
  if (visitors === 0) return 0
  return Math.round((conversions / visitors) * 10000) / 100
}

/**
 * Madde 4.2: Social share text generator
 */
export function generateShareText(product: {
  name: string
  tagline: string
  url: string
}): {
  twitter: string
  linkedIn: string
  facebook: string
} {
  return {
    twitter: `🚀 ${product.name} – ${product.tagline}\n\n@vitrinapp'te keşfet: ${product.url} #Vitrin #Startup`,
    linkedIn: `Yeni bir ürün keşfettim: ${product.name}\n\n${product.tagline}\n\nVitrin'de keşfedebilirsiniz: ${product.url}`,
    facebook: `${product.name} – ${product.tagline}\n\nDetaylar: ${product.url}`,
  }
}

/**
 * Madde 4.1: Value proposition matcher
 */
export function matchValueProp(userIntent: string): string {
  const intents = {
    discover: USP_VARIANTS.discoveryFocused.subheadline,
    submit: USP_VARIANTS.makerFocused.subheadline,
    vote: USP_VARIANTS.communityFocused.subheadline,
    learn: USP_VARIANTS.default.subheadline,
  }
  
  const intent = userIntent.toLowerCase()
  
  if (intent.includes('submit') || intent.includes('add') || intent.includes('launch')) {
    return intents.submit
  }
  if (intent.includes('discover') || intent.includes('find') || intent.includes('explore')) {
    return intents.discover
  }
  if (intent.includes('vote') || intent.includes('community')) {
    return intents.vote
  }
  
  return intents.learn
}
