import { NextResponse } from 'next/server'
import type { NextRequest } from 'next/server'

/**
 * Next.js Middleware — Tüm istekler için çalışır
 * 
 * Madde 1.2: Site hızı için compression ve caching header'ları
 * Madde 1.1: 404/500 hatalarını loglama
 */
export function middleware(request: NextRequest) {
  const response = NextResponse.next()

  // 1. Compression için Accept-Encoding kontrolü (nginx ile birlikte çalışır)
  const acceptEncoding = request.headers.get('accept-encoding') || ''
  if (acceptEncoding.includes('gzip') || acceptEncoding.includes('br')) {
    response.headers.set('Vary', 'Accept-Encoding')
  }

  // 2. Browser caching headers — statik asset'ler için
  const { pathname } = request.nextUrl
  
  if (
    pathname.startsWith('/_next/static/') ||
    pathname.startsWith('/images/') ||
    pathname.startsWith('/fonts/')
  ) {
    // Statik dosyalar 1 yıl cache'lensin (immutable)
    response.headers.set(
      'Cache-Control',
      'public, max-age=31536000, immutable'
    )
  } else if (
    pathname.endsWith('.jpg') ||
    pathname.endsWith('.jpeg') ||
    pathname.endsWith('.png') ||
    pathname.endsWith('.webp') ||
    pathname.endsWith('.svg') ||
    pathname.endsWith('.ico')
  ) {
    // Image'ler 7 gün cache'lensin
    response.headers.set(
      'Cache-Control',
      'public, max-age=604800, stale-while-revalidate=86400'
    )
  } else if (pathname.startsWith('/api/')) {
    // API route'ları cache'lenmesin
    response.headers.set('Cache-Control', 'no-store, must-revalidate')
  } else {
    // HTML sayfalar — 1 saat cache, ama revalidation ile
    response.headers.set(
      'Cache-Control',
      'public, max-age=3600, stale-while-revalidate=86400'
    )
  }

  // 3. Security headers (bonus — güvenlik için)
  response.headers.set('X-Frame-Options', 'DENY')
  response.headers.set('X-Content-Type-Options', 'nosniff')
  response.headers.set('Referrer-Policy', 'strict-origin-when-cross-origin')
  response.headers.set(
    'Permissions-Policy',
    'camera=(), microphone=(), geolocation=()'
  )

  // 4. HSTS (HTTPS zorunlu) — production'da nginx tarafından set edilir ama yine de ekleyelim
  if (process.env.NODE_ENV === 'production') {
    response.headers.set(
      'Strict-Transport-Security',
      'max-age=31536000; includeSubDomains; preload'
    )
  }

  return response
}

// Middleware sadece bu path'lerde çalışsın (performans için)
export const config = {
  matcher: [
    /*
     * Hariç tutulanlar:
     * - _next/static (build-time assets)
     * - _next/image (image optimization)
     * - favicon.ico, robots.txt, sitemap.xml
     */
    '/((?!_next/static|_next/image|favicon.ico|robots.txt|sitemap.xml).*)',
  ],
}
