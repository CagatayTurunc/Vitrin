#!/usr/bin/env tsx
/**
 * Link Checker Script — Madde 1.1
 * 
 * Kullanım:
 *   pnpm tsx scripts/check-links.ts
 *   pnpm tsx scripts/check-links.ts --url=https://vitrin.com
 */

import { checkAllSiteLinks, checkCriticalPages } from '../lib/link-checker'

async function main() {
  const args = process.argv.slice(2)
  const urlArg = args.find((arg) => arg.startsWith('--url='))
  const baseUrl = urlArg ? urlArg.split('=')[1] : 'http://localhost:3000'

  console.log(`🔍 Link kontrolü başlatılıyor: ${baseUrl}\n`)

  // Önce kritik sayfaları kontrol et (hızlı)
  console.log('📋 Kritik sayfalar kontrol ediliyor...')
  const criticalReport = await checkCriticalPages(baseUrl)
  
  console.log(`✅ Toplam: ${criticalReport.total}`)
  console.log(`❌ Hatalı: ${criticalReport.broken.length}`)
  console.log(`🔄 Yönlendirme: ${criticalReport.redirects.length}`)
  console.log(`🔴 5xx: ${criticalReport.serverErrors.length}`)
  console.log(`🟡 4xx: ${criticalReport.clientErrors.length}\n`)

  if (criticalReport.broken.length > 0) {
    console.log('⚠️  Hatalı linkler:')
    criticalReport.broken.forEach((link) => {
      console.log(`  - ${link.url} → ${link.status} ${link.error || ''}`)
    })
    console.log()
  }

  if (criticalReport.redirects.length > 0) {
    console.log('🔄 Yönlendirmeler:')
    criticalReport.redirects.forEach((link) => {
      console.log(`  - ${link.url} → ${link.status}`)
    })
    console.log()
  }

  // Sitemap varsa tüm sayfaları kontrol et
  if (args.includes('--full')) {
    console.log('📄 Sitemap kontrol ediliyor (bu biraz zaman alabilir)...')
    const fullReport = await checkAllSiteLinks(baseUrl)
    
    console.log(`✅ Toplam: ${fullReport.total}`)
    console.log(`❌ Hatalı: ${fullReport.broken.length}`)
    console.log(`🔄 Yönlendirme: ${fullReport.redirects.length}\n`)

    if (fullReport.broken.length > 0) {
      console.log('⚠️  Hatalı linkler:')
      fullReport.broken.forEach((link) => {
        console.log(`  - ${link.url} → ${link.status}`)
      })
    }
  }

  // Exit code (CI/CD için)
  const exitCode = criticalReport.broken.length > 0 ? 1 : 0
  process.exit(exitCode)
}

main().catch((error) => {
  console.error('❌ Hata:', error)
  process.exit(1)
})
