/**
 * Madde 1.1: Otomatik Link Kontrolü
 * 
 * Production'da tüm internal linklerin çalıştığını test eder.
 * 4xx, 5xx ve 302 redirect'leri tespit eder.
 */

export interface LinkCheckResult {
  url: string
  status: number
  ok: boolean
  redirected: boolean
  error?: string
}

export interface BrokenLinksReport {
  total: number
  broken: LinkCheckResult[]
  redirects: LinkCheckResult[]
  serverErrors: LinkCheckResult[]
  clientErrors: LinkCheckResult[]
  timestamp: string
}

/**
 * Tek bir URL'i kontrol et
 */
export async function checkLink(url: string): Promise<LinkCheckResult> {
  try {
    const response = await fetch(url, {
      method: 'HEAD', // Sadece header'ları al, body'yi indirme
      redirect: 'manual', // Redirect'leri manuel kontrol et
      signal: AbortSignal.timeout(10000), // 10 saniye timeout
    })

    return {
      url,
      status: response.status,
      ok: response.ok,
      redirected: response.redirected || [301, 302, 307, 308].includes(response.status),
    }
  } catch (error) {
    return {
      url,
      status: 0,
      ok: false,
      redirected: false,
      error: error instanceof Error ? error.message : 'Unknown error',
    }
  }
}

/**
 * Birden fazla URL'i paralel kontrol et
 */
export async function checkLinks(urls: string[]): Promise<BrokenLinksReport> {
  const results = await Promise.all(urls.map(checkLink))

  const broken = results.filter((r) => !r.ok && r.status !== 0)
  const redirects = results.filter((r) => r.redirected)
  const serverErrors = results.filter((r) => r.status >= 500 && r.status < 600)
  const clientErrors = results.filter((r) => r.status >= 400 && r.status < 500)

  return {
    total: results.length,
    broken,
    redirects,
    serverErrors,
    clientErrors,
    timestamp: new Date().toISOString(),
  }
}

/**
 * Sitenin tüm sayfalarını sitemap'ten oku ve kontrol et
 */
export async function checkAllSiteLinks(baseUrl: string): Promise<BrokenLinksReport> {
  // Sitemap'i fetch et
  const sitemapUrl = `${baseUrl}/sitemap.xml`
  const response = await fetch(sitemapUrl)
  const xml = await response.text()

  // XML'den URL'leri parse et (basit regex ile)
  const urlMatches = xml.matchAll(/<loc>(.*?)<\/loc>/g)
  const urls = Array.from(urlMatches, (m) => m[1])

  // Tüm URL'leri kontrol et
  return checkLinks(urls)
}

/**
 * Önemli sayfaları kontrol et (hızlı health check için)
 */
export async function checkCriticalPages(baseUrl: string): Promise<BrokenLinksReport> {
  const criticalPages = [
    '/',
    '/discover',
    '/login',
    '/register',
    '/submit',
    '/search',
    '/about',
    '/contact',
    '/api/health',
  ]

  const urls = criticalPages.map((path) => `${baseUrl}${path}`)
  return checkLinks(urls)
}
