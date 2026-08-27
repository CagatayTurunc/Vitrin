#!/usr/bin/env tsx
/**
 * Content Quality Audit — Bölüm 3
 * 
 * Madde 3.1-3.6: İçerik kalitesi, yazım, biçimlendirme kontrolü
 * 
 * Kullanım:
 *   pnpm tsx scripts/content-audit.ts
 *   pnpm tsx scripts/content-audit.ts --page=/about
 */

import { generateContentQualityReport } from '../lib/content-quality'

interface PageContent {
  url: string
  title: string
  content: string
  html?: string
}

async function fetchPageContent(url: string): Promise<PageContent | null> {
  try {
    const response = await fetch(url)
    const html = await response.text()
    
    // Extract title
    const titleMatch = html.match(/<title>([^<]+)<\/title>/i)
    const title = titleMatch ? titleMatch[1] : 'Untitled'
    
    // Extract text content (remove HTML tags)
    const textContent = html
      .replace(/<script[^>]*>[\s\S]*?<\/script>/gi, '')
      .replace(/<style[^>]*>[\s\S]*?<\/style>/gi, '')
      .replace(/<[^>]+>/g, ' ')
      .replace(/\s+/g, ' ')
      .trim()
    
    return {
      url,
      title,
      content: textContent,
      html,
    }
  } catch (error) {
    console.error(`Failed to fetch ${url}:`, error)
    return null
  }
}

async function auditPage(url: string) {
  console.log(`\n📄 Sayfa: ${url}`)
  console.log('━'.repeat(60))
  
  const page = await fetchPageContent(url)
  if (!page) {
    console.log('❌ Sayfa yüklenemedi\n')
    return
  }
  
  const report = generateContentQualityReport(page.content, page.html)
  
  // Skor
  const scoreColor = report.score >= 80 ? '🟢' : report.score >= 60 ? '🟡' : '🔴'
  console.log(`\n${scoreColor} İçerik Kalite Skoru: ${report.score}/100`)
  
  // Okunabilirlik
  console.log(`\n📖 Okunabilirlik:`)
  console.log(`  • Flesch Skoru: ${report.readability.fleschScore}/100 (${report.readability.grade})`)
  console.log(`  • Ortalama Kelime/Cümle: ${report.readability.averageWordsPerSentence}`)
  console.log(`  • Ortalama Hece/Kelime: ${report.readability.averageSyllablesPerWord}`)
  
  // Yapı
  console.log(`\n🏗️  Yapı:`)
  console.log(`  • Başlık: ${report.structure.hasHeadings ? '✅' : '❌'}`)
  console.log(`  • Liste: ${report.structure.hasList ? '✅' : '❌'}`)
  console.log(`  • Paragraf: ${report.structure.hasParagraphs ? '✅' : '❌'}`)
  
  // SEO
  console.log(`\n🔍 SEO:`)
  console.log(`  • Kelime Sayısı: ${report.seo.wordCount}`)
  
  // Sorunlar
  if (report.issues.length > 0) {
    console.log(`\n⚠️  Sorunlar:`)
    report.issues.forEach((issue) => {
      console.log(`  • ${issue}`)
    })
  }
  
  // Öneriler
  if (report.suggestions.length > 0) {
    console.log(`\n💡 Öneriler:`)
    report.suggestions.forEach((suggestion) => {
      console.log(`  • ${suggestion}`)
    })
  }
  
  if (report.issues.length === 0 && report.suggestions.length === 0) {
    console.log(`\n✅ Harika! İçerik kalitesi yüksek.`)
  }
  
  console.log()
}

async function main() {
  const args = process.argv.slice(2)
  const pageArg = args.find((arg) => arg.startsWith('--page='))
  const baseUrl = process.env.NEXT_PUBLIC_SITE_URL || 'http://localhost:3000'
  
  console.log('📋 Vitrin Content Quality Audit')
  console.log('=' .repeat(60))
  
  // Test edilecek sayfalar
  const pages = pageArg
    ? [pageArg.split('=')[1]]
    : [
        '/',
        '/about',
        '/blog',
        '/contact',
        '/discover',
        '/launches',
      ]
  
  for (const page of pages) {
    await auditPage(`${baseUrl}${page}`)
  }
  
  console.log('\n' + '='.repeat(60))
  console.log('✅ Audit tamamlandı!')
  console.log('\n💡 İpucu: Daha detaylı analiz için bireysel sayfa kontrolü yapın:')
  console.log('   pnpm tsx scripts/content-audit.ts --page=/about')
}

main().catch((error) => {
  console.error('❌ Fatal error:', error)
  process.exit(1)
})
