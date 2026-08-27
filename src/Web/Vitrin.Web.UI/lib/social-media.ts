/**
 * Social Media Utilities — Bölüm 3.7
 * 
 * Social sharing, Open Graph, Twitter Cards
 */

export interface SocialMediaConfig {
  twitter?: string
  facebook?: string
  instagram?: string
  linkedin?: string
  github?: string
  youtube?: string
}

export interface SocialShareData {
  url: string
  title: string
  description?: string
  hashtags?: string[]
  via?: string // Twitter username
}

/**
 * Madde 3.7: Social media share URL'leri oluştur
 */
export function generateShareUrls(data: SocialShareData) {
  const encodedUrl = encodeURIComponent(data.url)
  const encodedTitle = encodeURIComponent(data.title)
  const encodedDescription = data.description
    ? encodeURIComponent(data.description)
    : ''
  const hashtags = data.hashtags?.join(',') || ''
  
  return {
    twitter: `https://twitter.com/intent/tweet?url=${encodedUrl}&text=${encodedTitle}${
      data.via ? `&via=${data.via}` : ''
    }${hashtags ? `&hashtags=${hashtags}` : ''}`,
    
    facebook: `https://www.facebook.com/sharer/sharer.php?u=${encodedUrl}`,
    
    linkedin: `https://www.linkedin.com/sharing/share-offsite/?url=${encodedUrl}`,
    
    whatsapp: `https://wa.me/?text=${encodedTitle}%20${encodedUrl}`,
    
    telegram: `https://t.me/share/url?url=${encodedUrl}&text=${encodedTitle}`,
    
    reddit: `https://reddit.com/submit?url=${encodedUrl}&title=${encodedTitle}`,
    
    hackernews: `https://news.ycombinator.com/submitlink?u=${encodedUrl}&t=${encodedTitle}`,
    
    email: `mailto:?subject=${encodedTitle}&body=${encodedDescription}%0A%0A${encodedUrl}`,
  }
}

/**
 * Madde 3.7: Social meta tags validator
 */
export function validateSocialMeta(html: string): {
  isValid: boolean
  missing: string[]
  present: string[]
} {
  const requiredTags = [
    // Open Graph
    { name: 'og:title', pattern: /<meta\s+property=["']og:title["']/ },
    { name: 'og:description', pattern: /<meta\s+property=["']og:description["']/ },
    { name: 'og:image', pattern: /<meta\s+property=["']og:image["']/ },
    { name: 'og:url', pattern: /<meta\s+property=["']og:url["']/ },
    { name: 'og:type', pattern: /<meta\s+property=["']og:type["']/ },
    
    // Twitter Cards
    { name: 'twitter:card', pattern: /<meta\s+(?:name|property)=["']twitter:card["']/ },
    { name: 'twitter:title', pattern: /<meta\s+(?:name|property)=["']twitter:title["']/ },
    { name: 'twitter:description', pattern: /<meta\s+(?:name|property)=["']twitter:description["']/ },
    { name: 'twitter:image', pattern: /<meta\s+(?:name|property)=["']twitter:image["']/ },
  ]
  
  const present: string[] = []
  const missing: string[] = []
  
  requiredTags.forEach((tag) => {
    if (tag.pattern.test(html)) {
      present.push(tag.name)
    } else {
      missing.push(tag.name)
    }
  })
  
  return {
    isValid: missing.length === 0,
    missing,
    present,
  }
}

/**
 * Optimal image sizes for social media
 */
export const SOCIAL_IMAGE_SIZES = {
  'og:image': {
    width: 1200,
    height: 630,
    aspectRatio: '1.91:1',
    platforms: ['Facebook', 'LinkedIn', 'Twitter'],
  },
  'twitter:image': {
    width: 1200,
    height: 675,
    aspectRatio: '16:9',
    platforms: ['Twitter'],
  },
  instagram: {
    width: 1080,
    height: 1080,
    aspectRatio: '1:1',
    platforms: ['Instagram'],
  },
  'instagram-story': {
    width: 1080,
    height: 1920,
    aspectRatio: '9:16',
    platforms: ['Instagram Stories'],
  },
}

/**
 * Generate optimal Open Graph metadata
 */
export function generateOpenGraphMeta(data: {
  title: string
  description: string
  url: string
  image: string
  type?: 'website' | 'article' | 'product'
  locale?: string
  siteName?: string
}) {
  return {
    'og:title': data.title,
    'og:description': data.description,
    'og:url': data.url,
    'og:image': data.image,
    'og:type': data.type || 'website',
    'og:locale': data.locale || 'tr_TR',
    'og:site_name': data.siteName || 'Vitrin',
    'og:image:width': '1200',
    'og:image:height': '630',
  }
}

/**
 * Generate Twitter Card metadata
 */
export function generateTwitterCardMeta(data: {
  title: string
  description: string
  image: string
  card?: 'summary' | 'summary_large_image' | 'app' | 'player'
  site?: string
  creator?: string
}) {
  return {
    'twitter:card': data.card || 'summary_large_image',
    'twitter:title': data.title,
    'twitter:description': data.description,
    'twitter:image': data.image,
    'twitter:site': data.site || '@vitrinapp',
    'twitter:creator': data.creator || '@vitrinapp',
  }
}

/**
 * Social media content optimizer
 */
export function optimizeForSocial(content: {
  title: string
  description: string
}): {
  title: string
  description: string
  issues: string[]
} {
  const issues: string[] = []
  let { title, description } = content
  
  // Title length check
  if (title.length > 60) {
    issues.push('Title çok uzun (Twitter için 60 karakter önerilir)')
    title = title.slice(0, 57) + '...'
  }
  if (title.length < 20) {
    issues.push('Title çok kısa (En az 20 karakter önerilir)')
  }
  
  // Description length check
  if (description.length > 200) {
    issues.push('Description çok uzun (Twitter için 200 karakter önerilir)')
    description = description.slice(0, 197) + '...'
  }
  if (description.length < 100) {
    issues.push('Description çok kısa (En az 100 karakter önerilir)')
  }
  
  return {
    title,
    description,
    issues,
  }
}
