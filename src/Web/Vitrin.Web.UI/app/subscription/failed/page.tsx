'use client'

import { useSearchParams } from 'next/navigation'
import { useState, useEffect } from 'react'
import Link from 'next/link'
import { XCircle, ArrowLeft, RefreshCcw, Mail } from 'lucide-react'

const ERROR_MESSAGES: Record<string, string> = {
  missing_token: 'Ödeme doğrulama tokeni eksik.',
  invalid_conversation_id: 'Ödeme oturumu geçersiz.',
  payment_failed: 'Ödeme işlemi başarısız oldu.',
}

export default function SubscriptionFailedPage() {
  const searchParams = useSearchParams()
  const [mounted, setMounted] = useState(false)

  useEffect(() => setMounted(true), [])

  const errorCode = searchParams.get('error') ?? 'payment_failed'
  const errorMessage = ERROR_MESSAGES[errorCode] ?? decodeURIComponent(errorCode)

  return (
    <div className="min-h-screen bg-background flex items-center justify-center p-4">
      <div className="fixed inset-0 pointer-events-none">
        <div className="absolute top-1/4 left-1/2 -translate-x-1/2 w-[400px] h-[300px] rounded-full blur-3xl opacity-8 bg-red-500/20" />
      </div>

      <div className={`relative max-w-md w-full text-center space-y-6 transition-all duration-700 ${mounted ? 'opacity-100 scale-100' : 'opacity-0 scale-95'}`}>

        {/* Hata ikonu */}
        <div className="flex justify-center">
          <div className="w-20 h-20 rounded-3xl flex items-center justify-center bg-red-500/10 border-2 border-red-500/30">
            <XCircle className="w-10 h-10 text-red-500" />
          </div>
        </div>

        {/* Başlık */}
        <div className="space-y-2">
          <h1 className="text-2xl font-extrabold">Ödeme Başarısız</h1>
          <p className="text-muted-foreground">{errorMessage}</p>
        </div>

        {/* Olası nedenler */}
        <div className="rounded-2xl border border-border bg-card p-5 text-left space-y-2">
          <p className="text-sm font-semibold text-muted-foreground">Olası nedenler:</p>
          <ul className="space-y-1.5 text-sm text-muted-foreground">
            <li>• Kart limiti aşıldı</li>
            <li>• Yetersiz bakiye</li>
            <li>• 3D Secure doğrulaması başarısız</li>
            <li>• Kart bilgileri hatalı</li>
          </ul>
        </div>

        {/* CTA butonları */}
        <div className="space-y-3">
          <Link
            href="/checkout/pro"
            className="flex items-center justify-center gap-2 w-full py-3.5 px-6 rounded-xl font-bold text-white bg-gradient-to-r from-blue-500 to-purple-600 hover:opacity-90 transition-opacity shadow-lg"
          >
            <RefreshCcw className="w-4 h-4" />
            Tekrar Dene
          </Link>
          <Link
            href="/pricing"
            className="flex items-center justify-center gap-2 w-full py-3 px-6 rounded-xl font-semibold border border-border bg-muted hover:bg-muted/80 transition-colors text-sm"
          >
            <ArrowLeft className="w-4 h-4" />
            Planlara Dön
          </Link>
        </div>

        <p className="text-xs text-muted-foreground flex items-center justify-center gap-1.5">
          <Mail className="w-3.5 h-3.5" />
          Sorun devam ederse{' '}
          <a href="mailto:destek@vitrin.it.com" className="text-primary hover:underline">
            destek@vitrin.it.com
          </a>
        </p>
      </div>
    </div>
  )
}
