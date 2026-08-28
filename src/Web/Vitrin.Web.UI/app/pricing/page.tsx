'use client'

import { useState } from 'react'
import { useSession } from 'next-auth/react'
import { useRouter } from 'next/navigation'
import { Check, Zap, Building2, Sparkles, Crown } from 'lucide-react'

const PLANS = [
  {
    id: 'free',
    name: 'Ücretsiz',
    price: 0,
    period: 'sonsuza kadar',
    description: 'Vitrin\'i keşfetmek için mükemmel başlangıç',
    icon: Sparkles,
    color: 'from-gray-500 to-gray-600',
    border: 'border-border',
    badge: null,
    features: [
      '5 ürün paylaşımı',
      'Günlük 5 AI önerisi',
      'Temel analitik (7 gün)',
      'Topluluk erişimi',
      'Oy verme & yorum',
    ],
    missing: [
      'Sınırsız ürün',
      'Öncelikli listeleme',
      'Gelişmiş analitik',
      '🏆 PRO rozeti',
    ],
    cta: 'Mevcut Plan',
    ctaDisabled: true,
  },
  {
    id: 'pro',
    name: 'Pro Maker',
    price: 299,
    period: 'ay',
    description: 'Ciddi maker\'lar için güçlü araçlar',
    icon: Crown,
    color: 'from-blue-500 to-purple-600',
    border: 'border-blue-500/50',
    badge: '🏆 PRO',
    features: [
      'Sınırsız ürün paylaşımı',
      'Günlük 50 AI önerisi',
      '90 gün gelişmiş analitik',
      '🏆 PRO rozeti',
      'Ürün öne çıkarma',
      'Öncelikli listeleme',
      'Email bildirimleri',
    ],
    missing: [],
    cta: 'Pro\'ya Geç',
    ctaDisabled: false,
    popular: true,
  },
  {
    id: 'enterprise',
    name: 'Enterprise',
    price: 999,
    period: 'ay',
    description: 'Şirketler ve büyük ekipler için',
    icon: Building2,
    color: 'from-amber-500 to-pink-600',
    border: 'border-amber-500/50',
    badge: '💎 FEATURED',
    features: [
      'Pro\'daki her şey',
      '10 ekip üyesi',
      'Günlük 200 AI önerisi',
      '1 yıl analitik geçmişi',
      '💎 FEATURED rozeti + animasyon',
      'Öne çıkan ürün bölümü',
      'API erişimi',
      'Öncelikli destek',
      'Özel onboarding',
    ],
    missing: [],
    cta: 'Enterprise\'a Geç',
    ctaDisabled: false,
  },
]

export default function PricingPage() {
  const { data: session } = useSession()
  const router = useRouter()
  const [loading, setLoading] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  const handleUpgrade = async (planId: string) => {
    if (!session?.accessToken) {
      router.push('/login?redirect=/pricing')
      return
    }

    if (planId === 'free') return

    setLoading(planId)
    setError(null)

    try {
      const res = await fetch(
        `${process.env.NEXT_PUBLIC_API_URL}/api/subscription/checkout`,
        {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            Authorization: `Bearer ${session.accessToken}`,
          },
          body: JSON.stringify({ plan: planId }),
        }
      )

      if (!res.ok) {
        const data = await res.json().catch(() => ({})) as { detail?: string }
        throw new Error(data.detail ?? 'Ödeme başlatılamadı')
      }

      const data = await res.json() as { checkoutFormContent?: string; paymentPageUrl?: string }

      // İyzico checkout — redirect veya form inject
      if (data.paymentPageUrl) {
        window.location.href = data.paymentPageUrl
      } else if (data.checkoutFormContent) {
        // İyzico inline form
        const div = document.createElement('div')
        div.innerHTML = data.checkoutFormContent
        document.body.appendChild(div)
        const script = div.querySelector('script')
        if (script) {
          const s = document.createElement('script')
          s.src = script.src
          document.body.appendChild(s)
        }
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Bir hata oluştu')
    } finally {
      setLoading(null)
    }
  }

  return (
    <div className="min-h-screen bg-background">
      <div className="container mx-auto max-w-6xl px-4 py-16">
        {/* Header */}
        <div className="text-center mb-16">
          <div className="inline-flex items-center gap-2 px-4 py-1.5 rounded-full bg-blue-500/10 text-blue-500 text-sm font-medium mb-6">
            <Zap className="w-4 h-4" />
            Vitrin Pro ile daha fazlasına ulaş
          </div>
          <h1 className="text-4xl md:text-5xl font-extrabold tracking-tight mb-4">
            Sana uygun planı seç
          </h1>
          <p className="text-muted-foreground text-lg max-w-2xl mx-auto">
            Ücretsiz başla, büyüdükçe yükselt. İstediğin zaman iptal et.
          </p>
        </div>

        {/* Error */}
        {error && (
          <div className="mb-8 p-4 bg-red-500/10 border border-red-500/20 rounded-xl text-red-500 text-center">
            {error}
          </div>
        )}

        {/* Plans */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6 items-start">
          {PLANS.map((plan) => {
            const Icon = plan.icon
            const isLoading = loading === plan.id

            return (
              <div
                key={plan.id}
                className={`relative rounded-2xl border ${plan.border} bg-card p-6 flex flex-col gap-6 ${
                  plan.popular
                    ? 'ring-2 ring-blue-500 shadow-xl shadow-blue-500/10 scale-105'
                    : ''
                }`}
              >
                {/* Popular badge */}
                {plan.popular && (
                  <div className="absolute -top-3.5 left-1/2 -translate-x-1/2">
                    <span className="px-4 py-1 rounded-full bg-blue-500 text-white text-xs font-bold">
                      EN POPÜLER
                    </span>
                  </div>
                )}

                {/* Plan header */}
                <div>
                  <div className={`inline-flex p-2.5 rounded-xl bg-gradient-to-br ${plan.color} mb-3`}>
                    <Icon className="w-5 h-5 text-white" />
                  </div>
                  <div className="flex items-center gap-2 mb-1">
                    <h2 className="text-xl font-bold">{plan.name}</h2>
                    {plan.badge && (
                      <span className="px-2 py-0.5 rounded-md bg-gradient-to-r from-amber-500/20 to-pink-500/20 text-amber-500 text-xs font-bold border border-amber-500/30">
                        {plan.badge}
                      </span>
                    )}
                  </div>
                  <p className="text-muted-foreground text-sm">{plan.description}</p>
                </div>

                {/* Price */}
                <div className="flex items-end gap-1">
                  {plan.price === 0 ? (
                    <span className="text-4xl font-extrabold">Ücretsiz</span>
                  ) : (
                    <>
                      <span className="text-4xl font-extrabold">₺{plan.price}</span>
                      <span className="text-muted-foreground mb-1">/{plan.period}</span>
                    </>
                  )}
                </div>

                {/* CTA */}
                <button
                  onClick={() => handleUpgrade(plan.id)}
                  disabled={plan.ctaDisabled || isLoading}
                  className={`w-full py-3 rounded-xl font-semibold text-sm transition-all ${
                    plan.ctaDisabled
                      ? 'bg-muted text-muted-foreground cursor-not-allowed'
                      : plan.popular
                      ? 'bg-gradient-to-r from-blue-500 to-purple-600 text-white hover:opacity-90 active:scale-95'
                      : plan.id === 'enterprise'
                      ? 'bg-gradient-to-r from-amber-500 to-pink-600 text-white hover:opacity-90 active:scale-95'
                      : 'bg-muted text-foreground hover:bg-muted/80'
                  } ${isLoading ? 'opacity-70 cursor-wait' : ''}`}
                >
                  {isLoading ? (
                    <span className="flex items-center justify-center gap-2">
                      <svg className="animate-spin w-4 h-4" viewBox="0 0 24 24" fill="none">
                        <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                        <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8z" />
                      </svg>
                      Yükleniyor...
                    </span>
                  ) : (
                    plan.cta
                  )}
                </button>

                {/* Features */}
                <div className="flex flex-col gap-2.5">
                  {plan.features.map((f) => (
                    <div key={f} className="flex items-start gap-2.5 text-sm">
                      <Check className="w-4 h-4 text-emerald-500 mt-0.5 shrink-0" />
                      <span>{f}</span>
                    </div>
                  ))}
                  {plan.missing.map((f) => (
                    <div key={f} className="flex items-start gap-2.5 text-sm opacity-40">
                      <span className="w-4 h-4 mt-0.5 shrink-0 flex items-center justify-center">—</span>
                      <span className="line-through">{f}</span>
                    </div>
                  ))}
                </div>
              </div>
            )
          })}
        </div>

        {/* Footer note */}
        <div className="text-center mt-12 text-muted-foreground text-sm space-y-2">
          <p>Tüm planlar KDV dahildir. Ödeme işlemi İyzico güvencesiyle sağlanır.</p>
          <p>
            Sorularınız için{' '}
            <a href="/contact" className="text-blue-500 hover:underline">
              iletişime geçin
            </a>
          </p>
        </div>
      </div>
    </div>
  )
}
