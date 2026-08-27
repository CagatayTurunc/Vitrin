#!/usr/bin/env tsx
/**
 * SEO Audit Script — Bölüm 2
 * 
 * Madde 2.2: Duplicate content kontrolü
 * Madde 2.3: URL yapısı kontrolü
 * Madde 2.6: Meta tag kontrolü
 * 
 * Kullanım:
 *   pnpm tsx scripts/seo-audit.ts
 *   pnpm tsx scripts/seo-audit.ts --url=https://vitrin.com
 */

interface PageAudit {
  url: string
  title?: string
  description?: string
  canonical?: string
  hasH1: boolean
  metaIssues: string[]
  urlIssues: string[]
}

async function fetchPage(url: string): Promise<string> {
  const response = await fetch(url)
  return response.text()
}

function extractMetaTag(html: string, name: string): string | undefined {
  const metaRegex = new RegExp(
    `<meta\\s+(?:name|property)=["']${name}["']\\s+content=["']([^"']+)["']`,
    'i'
  )
  const match = html.match(metaRegex)
  return match?.[1]
}

function extractTitle(html: string): string | undefined {
  const match = html.match(/<title>([^<]+)<\/title>/i)
  return match?.[1]
}

function extractCanonical(html: string): string | undefined {
  const match = html.match(/<link\s+rel=["']canonical["']\s+href=["']([^"']+)["']/i)
  return match?.[1]
}

function hasH1Tag(html: string): boolean {
  return /<h1[^>]*>/.test(html)
}

function auditUrl(url: string): string[] {
  const issues: string[] = []

  // Madde 2.3: URL best practices
  if (url.length > 100) {
    issues.push('URL çok uzun (100+ karakter)')
  }

  if (url.includes('?') && url.split('?')[1].split('&').length > 3) {
    issues.push('Çok fazla query parameter')
  }

  if (/[^a-z0-9\-\/:.]/i.test(url)) {
    issues.push('URL özel karakter içeriyor')
  }

  if (url.includes('_')) {
    issues.push('URL underscore içeriyor (tire kullanın)')
  }

  if (!url.match(/^https?:\/\//)) {
    issues.push('Protocol eksik')
  }

  return issues
}

function auditMetaTags(page: PageAudit): string[] {
  const issues: string[] = []

  // Madde 2.6: Meta tag kontrolü
  if (!page.title) {
    issues.push('❌ Title tag eksik')
  } else if (page.title.length < 30) {
    issues.push('⚠️  Title çok kısa (<30 karakter)')
  } else if (page.title.length > 60) {
    issues.push('⚠️  Title çok uzun (>60 karakter)')
  }

  if (!page.description) {
    issues.push('❌ Meta description eksik')
  } else if (page.description.length < 120) {
    issues.push('⚠️  Description çok kısa (<120 karakter)')
  } else if (page.description.length > 160) {
    issues.push('⚠️  Description çok uzun (>160 karakter)')
  }

  if (!page.canonical) {
    issues.push('⚠️  Canonical tag eksik')
  }

  if (!page.hasH1) {
    issues.push('❌ H1 tag eksik')
  }

  return issues
}

async function auditPage(url: string): Promise<PageAudit> {
  console.log(`\n🔍 Kontrol ediliyor: ${url}`)

  try {
    const html = await fetchPage(url)

    const page: PageAudit = {
      url,
      title: extractTitle(html),
      description: extractMetaTag(html, 'description'),
      canonical: extractCanonical(html),
      hasH1: hasH1Tag(html),
      metaIssues: [],
      urlIssues: auditUrl(url),
    }

    page.metaIssues = auditMetaTags(page)

    // Sonuçları yazdır
    console.log(`  📄 Title: ${page.title || 'YOK'}`)
    console.log(`  📝 Description: ${page.description?.slice(0, 80) || 'YOK'}...`)
    console.log(`  🔗 Canonical: ${page.canonical || 'YOK'}`)
    console.log(`  📌 H1: ${page.hasH1 ? 'VAR' : 'YOK'}`)

    if (page.metaIssues.length > 0 || page.urlIssues.length > 0) {
      console.log(`\n  ⚠️  Sorunlar:`)
      ;[...page.metaIssues, ...page.urlIssues].forEach((issue) => {
        console.log(`    • ${issue}`)
      })
    } else {
      console.log(`  ✅ Sorun yok!`)
    }

    return page
  } catch (error) {
    console.error(`  ❌ Hata: ${error instanceof Error ? error.message : 'Unknown'}`)
    return {
      url,
      hasH1: false,
      metaIssues: ['Sayfa yüklenemedi'],
      urlIssues: [],
    }
  }
}

async function main() {
  const args = process.argv.slice(2)
  const urlArg = args.find((arg) => arg.startsWith('--url='))
  const baseUrl = urlArg ? urlArg.split('=')[1] : 'http://localhost:3000'

  console.log('🚀 Vitrin SEO Audit')
  console.log('==================\n')

  // Madde 2.2: Kritik sayfaları kontrol et
  const criticalPages = [
    '',
    '/discover',
    '/categories',
    '/launches',
    '/about',
    '/contact',
  ]

  const results: PageAudit[] = []

  for (const page of criticalPages) {
    const result = await auditPage(`${baseUrl}${page}`)
    results.push(result)
  }

  // Özet rapor
  console.log('\n\n📊 ÖZET RAPOR')
  console.log('=============\n')

  const totalIssues = results.reduce(
    (sum, r) => sum + r.metaIssues.length + r.urlIssues.length,
    0
  )

  console.log(`Toplam sayfa: ${results.length}`)
  console.log(`Toplam sorun: ${totalIssues}`)
  console.log(
    `Başarı oranı: ${Math.round(((results.length - results.filter((r) => r.metaIssues.length > 0).length) / results.length) * 100)}%`
  )

  // Duplicate title kontrolü (Madde 2.2)
  const titles = results.map((r) => r.title).filter(Boolean)
  const duplicateTitles = titles.filter(
    (title, index) => titles.indexOf(title) !== index
  )

  if (duplicateTitles.length > 0) {
    console.log('\n⚠️  KOPYA TITLE TAGI TESPİT EDİLDİ:')
    duplicateTitles.forEach((title) => {
      console.log(`  • "${title}"`)
    })
  }

  // Duplicate description kontrolü (Madde 2.2)
  const descriptions = results.map((r) => r.description).filter(Boolean)
  const duplicateDescriptions = descriptions.filter(
    (desc, index) => descriptions.indexOf(desc) !== index
  )

  if (duplicateDescriptions.length > 0) {
    console.log('\n⚠️  KOPYA DESCRIPTION TESPİT EDİLDİ:')
    duplicateDescriptions.forEach((desc) => {
      console.log(`  • "${desc?.slice(0, 60)}..."`)
    })
  }

  // Exit code
  process.exit(totalIssues > 0 ? 1 : 0)
}

main().catch((error) => {
  console.error('❌ Fatal error:', error)
  process.exit(1)
})
