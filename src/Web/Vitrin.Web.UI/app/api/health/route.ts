import { NextResponse } from 'next/server'

/**
 * Health Check Endpoint — Madde 1.1 için
 * 
 * Kubernetes, Docker, monitoring tool'ları buraya istek atarak
 * uygulamanın sağlıklı çalıştığını kontrol edebilir.
 */
export async function GET() {
  try {
    // Backend API'nin sağlık kontrolü (opsiyonel)
    const apiUrl = process.env.INTERNAL_API_URL || 'http://localhost:5000'
    let apiHealthy = false
    
    try {
      const apiResponse = await fetch(`${apiUrl}/health`, {
        signal: AbortSignal.timeout(3000), // 3 saniye timeout
      })
      apiHealthy = apiResponse.ok
    } catch {
      // API'ye ulaşılamazsa da frontend healthy sayılır
      apiHealthy = false
    }

    // Uygulama bilgileri
    const healthData = {
      status: 'healthy',
      timestamp: new Date().toISOString(),
      uptime: process.uptime(),
      version: process.env.npm_package_version || '1.0.0',
      environment: process.env.NODE_ENV,
      api: {
        url: apiUrl,
        healthy: apiHealthy,
      },
      memory: {
        used: Math.round(process.memoryUsage().heapUsed / 1024 / 1024),
        total: Math.round(process.memoryUsage().heapTotal / 1024 / 1024),
        rss: Math.round(process.memoryUsage().rss / 1024 / 1024),
      },
    }

    return NextResponse.json(healthData, {
      status: 200,
      headers: {
        'Cache-Control': 'no-store, must-revalidate',
      },
    })
  } catch (error) {
    return NextResponse.json(
      {
        status: 'unhealthy',
        error: error instanceof Error ? error.message : 'Unknown error',
        timestamp: new Date().toISOString(),
      },
      {
        status: 503,
        headers: {
          'Cache-Control': 'no-store, must-revalidate',
        },
      }
    )
  }
}

// HEAD request de destekle (Kubernetes için)
export async function HEAD() {
  return new NextResponse(null, { status: 200 })
}
