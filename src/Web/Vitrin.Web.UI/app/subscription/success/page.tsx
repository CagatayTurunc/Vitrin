'use client'

import { Suspense, useEffect, useState } from 'react'
import { useSearchParams } from 'next/navigation'
import { useSession } from 'next-auth/react'
import Link from 'next/link'
import { Crown, Building2, ArrowRight, CheckCircle } from 'lucide-react'

const TIER_CONFIG: Record<string, { label: string; icon: typeof Crown; gradient: string; badge: string; features: string[] }> = {
  ProMaker: {
    label: 'Pro Maker',
    icon: Crown,
    gradient: 'from-blue-500 to-purple-600',
    badge: '🏆 PRO',
    features: ['Sınırsız ürün paylaşımı', 'Günlük 50 AI önerisi', '🏆 PRO rozeti profilde', 'Öncelikli listeleme', '90 gün gelişmiş analitik'],
  },
  Enterprise: {
    label: 'Enterprise',
    icon: Building2,
    gradient: 'from-amber-500 to-pink-500',
    badge: '💎 ENTERPRISE',
    features: ["Pro'daki her şey", '10 ekip üyesi', '💎 FEATURED rozeti', 'API erişimi', 'Öncelikli destek'],
  },
}

function SuccessContent() {
  const searchParams = useSearchParams()
  const { update } = useSession()
  const [mounted, setMounted] = useState(false)

  const tier = searchParams.get('tier') ?? 'ProMaker'
  const config = TIER_CONFIG[tier] ?? TIER_CONFIG.ProMaker
  const Icon = config.icon

  useEffect(() => {
    setMounted(true)
    update()
  }, [update])

  return (
    <div className={`relative max-w-md w-full text-center space-y-6 transition-all duration-700 ${mounted ? 'opacity-100 scale-100' : 'opacity-0 scale-95'}`}>
      <div className="flex justify-center">
        <div className={`relative w-24 h-24 rounded-3xl flex items-center justify-center bg-gradient-to-br ${config.gradient} shadow-2xl`}>
          <Icon className="w-12 h-12 text-white" />
          <div className="absolute -bottom-2 -right-2 w-8 h-8 rounded-full bg-emerald-500 border-2 border-background flex items-center justify-center">
            <CheckCircle className="w-5 h-5 text-white fill-current" />
          </div>
        </div>
      </div>

      <div className="space-y-2">
        <div className={`inline-flex items-center gap-2 px-4 py-1.5 rounded-full text-sm font-bold text-white bg-gradient-to-r ${config.gradient}`}>
          {config.badge}
        </div>
        <h1 className="text-3xl font-extrabold mt-3">
          Hoş geldin, {config.label}! 🎉
        </h1>
        <p className="text-muted-foreground text-lg">
          Aboneliğin başarıyla aktifleştirildi.
        </p>
      </div>

      <div className="rounded-2xl border border-border bg-card p-5 text-left space-y-3">
        <p className="text-sm font-semibold text-muted-foreground uppercase tracking-wider">Artık erişebilirsin</p>
        <ul className="space-y-2">
          {config.features.map(f => (
            <li key={f} className="flex items-center gap-2 text-sm">
              <CheckCircle className="w-4 h-4 text-emerald-500 shrink-0" />
              {f}
            </li>
          ))}
        </ul>
      </div>

      <div className="space-y-3">
        <Link
          href="/"
          className={`flex items-center justify-center gap-2 w-full py-3.5 px-6 rounded-xl font-bold text-white bg-gradient-to-r ${config.gradient} hover:opacity-90 transition-opacity shadow-lg`}
        >
          Ürünleri Keşfet
          <ArrowRight className="w-4 h-4" />
        </Link>
        <Link
          href="/dashboard"
          className="flex items-center justify-center gap-2 w-full py-3 px-6 rounded-xl font-semibold border border-border bg-muted hover:bg-muted/80 transition-colors text-sm"
        >
          Dashboard'a Git
        </Link>
      </div>

      <p className="text-xs text-muted-foreground">
        Fatura ve abonelik detayları için Dashboard → Abonelik bölümünü ziyaret et.
      </p>
    </div>
  )
}

export default function SubscriptionSuccessPage() {
  return (
    <div className="min-h-screen bg-background flex items-center justify-center p-4">
      <div className="fixed inset-0 pointer-events-none">
        <div className="absolute top-1/4 left-1/2 -translate-x-1/2 w-[600px] h-[400px] rounded-full blur-3xl opacity-10 bg-gradient-to-br from-blue-500 to-purple-600" />
      </div>
      <Suspense fallback={<div className="text-muted-foreground">Yükleniyor...</div>}>
        <SuccessContent />
      </Suspense>
    </div>
  )
}
