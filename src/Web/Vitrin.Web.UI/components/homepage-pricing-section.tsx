'use client'

import { useState } from 'react'
import Link from 'next/link'
import { useRouter } from 'next/navigation'
import { useSession } from 'next-auth/react'
import {
  Check,
  Crown,
  Building2,
  Sparkles,
  ArrowRight,
  Zap,
  Star,
} from 'lucide-react'

const PLANS = [
  {
    id: 'free',
    name: 'Ücretsiz',
    price: 0,
    period: 'sonsuza kadar',
    description: 'Keşfe başlamak için ideal',
    icon: Sparkles,
    gradient: 'from-slate-500 to-slate-600',
    glow: '',
    borderClass: 'border-border',
    badgeClass: '',
    features: [
      '5 ürün paylaşımı',
      'Günlük 5 AI önerisi',
      'Temel analitik (7 gün)',
      'Topluluk erişimi',
      'Oy verme & yorum',
    ],
    cta: 'Ücretsiz Başla',
    href: '/login',
    popular: false,
  },
  {
    id: 'pro',
    name: 'Pro Maker',
    price: 299,
    period: 'ay',
    description: 'Ciddi maker\'lar için güçlü araçlar',
    icon: Crown,
    gradient: 'from-blue-500 to-purple-600',
    glow: 'shadow-[0_0_40px_rgba(99,102,241,0.25)]',
    borderClass: 'border-blue-500/60',
    badgeClass: 'bg-gradient-to-r from-blue-500 to-purple-600',
    features: [
      'Sınırsız ürün paylaşımı',
      'Günlük 50 AI önerisi',
      '90 gün gelişmiş analitik',
      '🏆 PRO rozeti',
      'Ürün öne çıkarma',
      'Öncelikli listeleme',
      'Email bildirimleri',
    ],
    cta: 'Pro\'ya Geç',
    href: '/pricing',
    popular: true,
  },
  {
    id: 'enterprise',
    name: 'Enterprise',
    price: 999,
    period: 'ay',
    description: 'Şirketler ve büyük ekipler için',
    icon: Building2,
    gradient: 'from-amber-500 to-pink-500',
    glow: 'shadow-[0_0_40px_rgba(245,158,11,0.15)]',
    borderClass: 'border-amber-500/40',
    badgeClass: 'bg-gradient-to-r from-amber-500 to-pink-500',
    features: [
      'Pro\'daki her şey',
      '10 ekip üyesi',
      'Günlük 200 AI önerisi',
      '1 yıl analitik geçmişi',
      '💎 FEATURED rozeti',
      'Öne çıkan ürün bölümü',
      'API erişimi',
      'Öncelikli destek',
    ],
    cta: 'Enterprise\'a Geç',
    href: '/pricing',
    popular: false,
  },
]

export function HomepagePricingSection() {
  const { data: session } = useSession()
  const router = useRouter()
  const [hoveredPlan, setHoveredPlan] = useState<string | null>(null)

  const handleCta = (plan: (typeof PLANS)[0]) => {
    if (plan.id === 'free') {
      if (!session) router.push('/login')
      return
    }
    router.push(session ? plan.href : `/login?redirect=${plan.href}`)
  }

  return (
    <section className="relative py-20 px-4 overflow-hidden" id="planlar">
      {/* Arka plan dekorasyon */}
      <div className="absolute inset-0 pointer-events-none">
        <div className="absolute top-0 left-1/2 -translate-x-1/2 w-[800px] h-[400px] bg-gradient-to-b from-primary/5 to-transparent rounded-full blur-3xl" />
        <div className="absolute bottom-0 left-1/4 w-[400px] h-[300px] bg-gradient-to-t from-blue-500/5 to-transparent rounded-full blur-3xl" />
        <div className="absolute bottom-0 right-1/4 w-[400px] h-[300px] bg-gradient-to-t from-purple-500/5 to-transparent rounded-full blur-3xl" />
      </div>

      <div className="relative mx-auto max-w-6xl">
        {/* Başlık */}
        <div className="text-center mb-14 space-y-4">
          <div className="inline-flex items-center gap-2 px-4 py-1.5 rounded-full bg-primary/10 border border-primary/20 text-primary text-sm font-medium">
            <Zap className="w-3.5 h-3.5" />
            <span>Abonelik Planları</span>
          </div>

          <h2 className="text-3xl sm:text-4xl lg:text-5xl font-extrabold tracking-tight">
            Topluluğun{' '}
            <span className="bg-gradient-to-r from-blue-500 to-purple-600 bg-clip-text text-transparent">
              gücüne katıl
            </span>
          </h2>

          <p className="text-lg text-muted-foreground max-w-xl mx-auto">
            Ürününü öne çıkar, toplulukla büyü. Her seviyeye uygun bir planımız var.
          </p>
        </div>

        {/* Plan kartları */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6 lg:gap-8 items-start">
          {PLANS.map((plan) => {
            const Icon = plan.icon
            const isHovered = hoveredPlan === plan.id

            return (
              <div
                key={plan.id}
                onMouseEnter={() => setHoveredPlan(plan.id)}
                onMouseLeave={() => setHoveredPlan(null)}
                className={`
                  relative flex flex-col rounded-2xl border p-6 lg:p-7 transition-all duration-300
                  ${plan.popular ? `bg-card ${plan.glow} scale-[1.03] border-2 ${plan.borderClass}` : `bg-card/60 ${plan.borderClass} hover:bg-card`}
                  ${isHovered && !plan.popular ? 'shadow-lg -translate-y-1' : ''}
                `}
              >
                {/* Popüler rozeti */}
                {plan.popular && (
                  <div className="absolute -top-3.5 left-1/2 -translate-x-1/2">
                    <div className={`inline-flex items-center gap-1.5 px-4 py-1 rounded-full text-white text-xs font-bold ${plan.badgeClass}`}>
                      <Star className="w-3 h-3 fill-current" />
                      En Popüler
                    </div>
                  </div>
                )}

                {/* Plan başlığı */}
                <div className="mb-6">
                  <div className="flex items-center gap-3 mb-3">
                    <div className={`w-10 h-10 rounded-xl flex items-center justify-center bg-gradient-to-br ${plan.gradient}`}>
                      <Icon className="w-5 h-5 text-white" />
                    </div>
                    <div>
                      <h3 className="font-bold text-foreground">{plan.name}</h3>
                      {plan.badgeClass && (
                        <span className={`inline-block text-[10px] font-bold px-2 py-0.5 rounded-full text-white ${plan.badgeClass}`}>
                          {plan.id === 'pro' ? '🏆 PRO' : '💎 ENTERPRISE'}
                        </span>
                      )}
                    </div>
                  </div>

                  <p className="text-sm text-muted-foreground">{plan.description}</p>
                </div>

                {/* Fiyat */}
                <div className="mb-6">
                  {plan.price === 0 ? (
                    <div className="flex items-end gap-1">
                      <span className="text-4xl font-extrabold text-foreground">Ücretsiz</span>
                    </div>
                  ) : (
                    <div className="flex items-end gap-1">
                      <span className="text-4xl font-extrabold text-foreground">₺{plan.price}</span>
                      <span className="text-muted-foreground text-sm pb-1">/{plan.period}</span>
                    </div>
                  )}
                </div>

                {/* CTA butonu */}
                <button
                  onClick={() => handleCta(plan)}
                  className={`
                    w-full py-3 px-4 rounded-xl font-semibold text-sm flex items-center justify-center gap-2 transition-all duration-200 mb-6
                    ${plan.popular
                      ? `bg-gradient-to-r ${plan.gradient} text-white hover:opacity-90 shadow-md`
                      : plan.id === 'enterprise'
                      ? `bg-gradient-to-r ${plan.gradient} text-white hover:opacity-90`
                      : 'bg-muted text-foreground hover:bg-muted/80 border border-border'
                    }
                  `}
                >
                  {plan.cta}
                  <ArrowRight className="w-4 h-4" />
                </button>

                {/* Özellikler listesi */}
                <div className="space-y-2.5 flex-1">
                  {plan.features.map((feature, i) => (
                    <div key={i} className="flex items-start gap-2.5">
                      <div className={`mt-0.5 w-4 h-4 rounded-full flex items-center justify-center shrink-0 bg-gradient-to-br ${plan.gradient}`}>
                        <Check className="w-2.5 h-2.5 text-white" />
                      </div>
                      <span className="text-sm text-muted-foreground leading-snug">{feature}</span>
                    </div>
                  ))}
                </div>
              </div>
            )
          })}
        </div>

        {/* Alt bilgi */}
        <div className="mt-10 text-center space-y-3">
          <p className="text-sm text-muted-foreground">
            Tüm planlar KDV dahildir · İstediğin zaman iptal et · Güvenli ödeme ile iyzico
          </p>
          <Link
            href="/pricing"
            className="inline-flex items-center gap-1.5 text-sm text-primary hover:underline font-medium"
          >
            Detaylı plan karşılaştırmasını gör
            <ArrowRight className="w-3.5 h-3.5" />
          </Link>
        </div>
      </div>
    </section>
  )
}
