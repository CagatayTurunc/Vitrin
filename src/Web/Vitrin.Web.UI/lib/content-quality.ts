/**
 * Content Quality Checker — Bölüm 3
 * 
 * Madde 3.1: İçerik değer kontrolü
 * Madde 3.2: Dil bilgisi ve yazım kontrolü
 * Madde 3.3: Biçimlendirme kontrolü
 * Madde 3.4: Gerçek bilgi doğrulama
 */

export interface ContentQualityReport {
  score: number // 0-100
  readability: {
    fleschScore: number
    averageWordsPerSentence: number
    averageSyllablesPerWord: number
    grade: string
  }
  structure: {
    hasHeadings: boolean
    hasList: boolean
    hasParagraphs: boolean
    imageCount: number
    linkCount: number
  }
  seo: {
    wordCount: number
    keywordDensity: number
    titleLength: number
    descriptionLength: number
  }
  issues: string[]
  suggestions: string[]
}

/**
 * Madde 3.1: İçerik değer analizi
 */
export function analyzeContentValue(content: string): {
  hasValue: boolean
  reasons: string[]
} {
  const reasons: string[] = []
  
  // Minimum uzunluk kontrolü
  if (content.length < 300) {
    reasons.push('İçerik çok kısa (<300 karakter)')
  }
  
  // Kelime sayısı
  const wordCount = content.split(/\s+/).length
  if (wordCount < 100) {
    reasons.push(`Kelime sayısı yetersiz (${wordCount} kelime)`)
  }
  
  // Özgünlük kontrolü (basit)
  const uniqueWords = new Set(content.toLowerCase().split(/\s+/))
  const uniqueRatio = uniqueWords.size / wordCount
  if (uniqueRatio < 0.4) {
    reasons.push('Tekrarlayan kelimeler çok fazla (özgünlük düşük)')
  }
  
  // Cümle çeşitliliği
  const sentences = content.split(/[.!?]+/).filter(s => s.trim())
  if (sentences.length < 3) {
    reasons.push('Cümle sayısı yetersiz')
  }
  
  // Liste veya madde işareti
  const hasLists = /[\n-•*]/.test(content) || /<li>/i.test(content)
  if (!hasLists && wordCount > 200) {
    reasons.push('Uzun içerikte liste/madde kullanımı önerilir')
  }
  
  return {
    hasValue: reasons.length === 0,
    reasons,
  }
}

/**
 * Madde 3.2: Temel yazım kontrolleri
 */
export function checkSpelling(text: string): {
  issues: Array<{ type: string; message: string; position?: number }>
} {
  const issues: Array<{ type: string; message: string; position?: number }> = []
  
  // Türkçe karakter kontrolü
  const turkishChars = /[ğĞıİöÖüÜşŞçÇ]/
  if (!turkishChars.test(text) && text.length > 100) {
    issues.push({
      type: 'warning',
      message: 'Türkçe karakter kullanımı eksik olabilir',
    })
  }
  
  // Çift boşluk
  const doubleSpaces = text.match(/  +/g)
  if (doubleSpaces) {
    issues.push({
      type: 'spacing',
      message: `${doubleSpaces.length} adet çift boşluk bulundu`,
    })
  }
  
  // Noktalama hatası (boşluksuz nokta)
  const punctuationErrors = text.match(/\w[.,:;!?]\w/g)
  if (punctuationErrors) {
    issues.push({
      type: 'punctuation',
      message: `${punctuationErrors.length} adet noktalama hatası olabilir`,
    })
  }
  
  // Büyük harf kullanımı (cümle başları)
  const sentences = text.split(/[.!?]+\s+/)
  const lowercaseStarts = sentences.filter(
    (s) => s.trim() && /^[a-zğıöüşç]/.test(s.trim())
  )
  if (lowercaseStarts.length > 0) {
    issues.push({
      type: 'capitalization',
      message: `${lowercaseStarts.length} cümle küçük harfle başlıyor`,
    })
  }
  
  return { issues }
}

/**
 * Madde 3.3: İçerik biçimlendirme kontrolü
 */
export function checkFormatting(html: string): {
  hasProperFormatting: boolean
  structure: {
    hasHeadings: boolean
    hasList: boolean
    hasParagraphs: boolean
    hasImages: boolean
    hasLinks: boolean
  }
  suggestions: string[]
} {
  const suggestions: string[] = []
  
  const structure = {
    hasHeadings: /<h[1-6]/.test(html),
    hasList: /<[uo]l>/.test(html),
    hasParagraphs: /<p>/.test(html),
    hasImages: /<img/.test(html),
    hasLinks: /<a\s/.test(html),
  }
  
  if (!structure.hasHeadings) {
    suggestions.push('Başlık (H2, H3) kullanımı önerilir')
  }
  
  if (!structure.hasList && html.length > 500) {
    suggestions.push('Liste kullanımı içeriği daha okunabilir yapar')
  }
  
  if (!structure.hasParagraphs) {
    suggestions.push('Paragraf etiketleri (<p>) kullanın')
  }
  
  // Çok uzun paragraflar
  const paragraphs = html.match(/<p>(.*?)<\/p>/gs)
  if (paragraphs) {
    const longParagraphs = paragraphs.filter((p) => p.length > 500)
    if (longParagraphs.length > 0) {
      suggestions.push(`${longParagraphs.length} adet çok uzun paragraf var`)
    }
  }
  
  return {
    hasProperFormatting: suggestions.length === 0,
    structure,
    suggestions,
  }
}

/**
 * Madde 3.3: Okunabilirlik skoru (Flesch Reading Ease - Türkçe adapte)
 */
export function calculateReadability(text: string): {
  score: number
  level: string
  details: {
    sentences: number
    words: number
    syllables: number
    avgWordsPerSentence: number
    avgSyllablesPerWord: number
  }
} {
  const sentences = text.split(/[.!?]+/).filter((s) => s.trim()).length
  const words = text.split(/\s+/).filter((w) => w.trim()).length
  const syllables = estimateSyllables(text)
  
  const avgWordsPerSentence = sentences > 0 ? words / sentences : 0
  const avgSyllablesPerWord = words > 0 ? syllables / words : 0
  
  // Flesch Reading Ease (Türkçe için adapte edilmiş basit versiyon)
  const score = Math.max(
    0,
    Math.min(
      100,
      206.835 - 1.015 * avgWordsPerSentence - 84.6 * avgSyllablesPerWord
    )
  )
  
  let level = 'Çok Zor'
  if (score >= 80) level = 'Çok Kolay'
  else if (score >= 60) level = 'Kolay'
  else if (score >= 50) level = 'Orta'
  else if (score >= 30) level = 'Zor'
  
  return {
    score: Math.round(score),
    level,
    details: {
      sentences,
      words,
      syllables,
      avgWordsPerSentence: Math.round(avgWordsPerSentence * 10) / 10,
      avgSyllablesPerWord: Math.round(avgSyllablesPerWord * 10) / 10,
    },
  }
}

/**
 * Hece tahmin fonksiyonu (Türkçe için basit)
 */
function estimateSyllables(text: string): number {
  // Türkçe sesli harfler
  const vowels = /[aeıioöuü]/gi
  const matches = text.match(vowels)
  return matches ? matches.length : 0
}

/**
 * Madde 3.6: İçerik haritası validator
 */
export interface ContentMapItem {
  title: string
  path: string
  purpose: string // Hangi soruya cevap veriyor?
  targetAudience: string // Kime hitap ediyor?
  keywords: string[]
  status: 'draft' | 'published' | 'planned'
}

export function validateContentMap(items: ContentMapItem[]): {
  isValid: boolean
  coverage: {
    total: number
    published: number
    draft: number
    planned: number
  }
  gaps: string[]
  duplicates: Array<{ title: string; paths: string[] }>
} {
  const coverage = {
    total: items.length,
    published: items.filter((i) => i.status === 'published').length,
    draft: items.filter((i) => i.status === 'draft').length,
    planned: items.filter((i) => i.status === 'planned').length,
  }
  
  // Eksik içerik tespiti
  const gaps: string[] = []
  
  const essentialTopics = [
    'nasıl çalışır',
    'hakkımızda',
    'iletişim',
    'sıkça sorulan sorular',
    'kullanım şartları',
    'gizlilik politikası',
  ]
  
  essentialTopics.forEach((topic) => {
    const exists = items.some((item) =>
      item.title.toLowerCase().includes(topic)
    )
    if (!exists) {
      gaps.push(`"${topic}" konusunda içerik eksik`)
    }
  })
  
  // Duplicate content tespiti
  const titleGroups = new Map<string, string[]>()
  items.forEach((item) => {
    const normalized = item.title.toLowerCase().trim()
    if (!titleGroups.has(normalized)) {
      titleGroups.set(normalized, [])
    }
    titleGroups.get(normalized)!.push(item.path)
  })
  
  const duplicates = Array.from(titleGroups.entries())
    .filter(([, paths]) => paths.length > 1)
    .map(([title, paths]) => ({ title, paths }))
  
  return {
    isValid: gaps.length === 0 && duplicates.length === 0,
    coverage,
    gaps,
    duplicates,
  }
}

/**
 * Tam içerik kalite raporu
 */
export function generateContentQualityReport(
  content: string,
  html?: string
): ContentQualityReport {
  const valueAnalysis = analyzeContentValue(content)
  const spellingCheck = checkSpelling(content)
  const formattingCheck = html ? checkFormatting(html) : null
  const readability = calculateReadability(content)
  
  const words = content.split(/\s+/).length
  const issues: string[] = []
  const suggestions: string[] = []
  
  // Value issues
  if (!valueAnalysis.hasValue) {
    issues.push(...valueAnalysis.reasons)
  }
  
  // Spelling issues
  spellingCheck.issues.forEach((issue) => {
    issues.push(`${issue.type}: ${issue.message}`)
  })
  
  // Formatting suggestions
  if (formattingCheck) {
    suggestions.push(...formattingCheck.suggestions)
  }
  
  // Readability suggestions
  if (readability.score < 50) {
    suggestions.push(
      'Okunabilirlik düşük - daha kısa cümleler ve basit kelimeler kullanın'
    )
  }
  
  // Skor hesapla (0-100)
  let score = 100
  score -= issues.length * 10 // Her issue -10 puan
  score -= suggestions.length * 5 // Her suggestion -5 puan
  score = Math.max(0, Math.min(100, score))
  
  return {
    score,
    readability: {
      fleschScore: readability.score,
      averageWordsPerSentence: readability.details.avgWordsPerSentence,
      averageSyllablesPerWord: readability.details.avgSyllablesPerWord,
      grade: readability.level,
    },
    structure: formattingCheck?.structure || {
      hasHeadings: false,
      hasList: false,
      hasParagraphs: false,
      imageCount: 0,
      linkCount: 0,
    },
    seo: {
      wordCount: words,
      keywordDensity: 0, // calculateKeywordDensity'den alınabilir
      titleLength: 0, // Ayrı olarak sağlanmalı
      descriptionLength: 0, // Ayrı olarak sağlanmalı
    },
    issues,
    suggestions,
  }
}
