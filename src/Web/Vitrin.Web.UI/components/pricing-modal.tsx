'use client'

import { useEffect, useState, useCallback } from 'react'
import { useRouter } from 'next/navigation'
import { useSession } from 'next-auth/react'
import {
  X,
  Check,
  Crown,
  Building2,
  Sparkles,
  ArrowRight,
  Star,
  Zap,
} from 'lucide-react'

const PLANS = [
  {
    id: 'free',
    name: 'Ücretsiz',
    price: 0,
    icon: Sparkles,
    gradient: 'from-slate-400 to-slate-500',
    borderClass: 'border-border',
    features: ['5 ürün paylaşımı', 'Günlük 5 AI önerisi', 'Topluluk erişimi'],
    cta: 'Devam et',
    popular: false,
  },
  {
    id: 'pro',
    name: 'Pro Maker',
    price: 299,
    icon: Crown,
    gradient: 'from-blue-500 to-purple-600',
    borderClass: 'border-blue-500/60',
    features: [
      'Sınırsız ürün paylaşımı',
      'Günlük 50 AI önerisi',
      '🏆 PRO rozeti',
      'Öncelikli listeleme',
      '90 gün analitik',
    ],
    cta: "Pro'ya Geç — ₺299/ay",
    popular: true,
  },
  {
    id: 'enterprise',
    name: 'Enterprise',
    price: 999,
    icon: Building2,
    gradient: 'from-amber-500 to-pink-500',
    borderClass: 'border-amber-500/40',
    features: [
      "Pro'daki her şey",
      '10 ekip üyesi',
      '💎 FEATURED rozeti',
      'API erişimi',
      'Öncelikli destek',
    ],
    cta: "Enterprise — ₺999/ay",
    popular: false,
  },
]

type SubscriptionTier = 'Free' | 'Pro' | 'Enterprise'

// PLANS id → tier eşleşmesi
const PLAN_ID_TO_TIER: Record<string, SubscriptionTier> = {
  free: 'Free',
  pro: 'Pro',
  enterprise: 'Enterprise',
}

interface PricingModalProps {
  open: boolean
  onClose: () => void
  /** Modalı kim tetikledi: 'login' | 'register' | 'manual' */
  trigger?: 'login' | 'register' | 'manual'
  /** Kullanıcının şu anki abonelik planı — bilinmiyorsa 'Free' varsayılır */
  currentTier?: SubscriptionTier
}

export function PricingModal({ open, onClose, trigger = 'manual', currentTier = 'Free' }: PricingModalProps) {
  const { data: session } = useSession()
  const router = useRouter()
  const [visible, setVisible] = useState(false)

  // Açılış/kapanış animasyonu
  useEffect(() => {
    if (open) {
      // Small delay so CSS transition fires
      requestAnimationFrame(() => setVisible(true))
    } else {
      setVisible(false)
    }
  }, [open])

  const handleClose = useCallback(() => {
    setVisible(false)
    setTimeout(onClose, 300)
  }, [onClose])

  // ESC ile kapat
  useEffect(() => {
    if (!open) return
    const handler = (e: KeyboardEvent) => {
      if (e.key === 'Escape') handleClose()
    }
    window.addEventListener('keydown', handler)
    return () => window.removeEventListener('keydown', handler)
  }, [open, handleClose])

  // body scroll kilitle
  useEffect(() => {
    if (open) {
      document.body.style.overflow = 'hidden'
    } else {
      document.body.style.overflow = ''
    }
    return () => { document.body.style.overflow = '' }
  }, [open])

  if (!open) return null

  const handlePlanSelect = async (planId: string) => {
    const tier = PLAN_ID_TO_TIER[planId]

    // Zaten bu plandaysa kapat
    if (tier === currentTier) {
      handleClose()
      router.push('/')
      return
    }

    if (planId === 'free') {
      handleClose()
      router.push('/')
      return
    }

    if (!session?.accessToken) {
      handleClose()
      router.push(`/login?redirect=/checkout/${planId}`)
      return
    }

    // Checkout onay sayfasına yönlendir
    handleClose()
    router.push(`/checkout/${planId}`)
  }

  const headingMap = {
    login: {
      Free: { title: 'Vitrina Hoş Geldin! 🎉', sub: 'Planını seç ve topluluğa katıl.' },
      Pro: { title: 'Tekrar Hoş Geldin! 🏆', sub: 'Pro Maker planınla tüm özellikler aktif.' },
      Enterprise: { title: 'Tekrar Hoş Geldin! 💎', sub: 'Enterprise planınla tam güçte çalışıyorsun.' },
    },
    register: {
      Free: { title: 'Hesabın hazır!', sub: 'Bir plan seçerek başla. İstediğin zaman değiştirebilirsin.' },
      Pro: { title: 'Hesabın hazır!', sub: 'Pro Maker planınla tüm özellikler senin için aktif.' },
      Enterprise: { title: 'Hesabın hazır!', sub: 'Enterprise planınla tam gücünle başlıyorsun.' },
    },
    manual: {
      Free: { title: 'Planını Seç', sub: 'Her seviyeye uygun bir planımız var.' },
      Pro: { title: 'Planını Yönet', sub: 'Mevcut planın: Pro Maker. Dilersen yükseltebilirsin.' },
      Enterprise: { title: 'Planını Yönet', sub: 'Mevcut planın: Enterprise. En yüksek plandaki avantajlar senin.' },
    },
  }
  const VALID_HEADING_TIERS = ['Free', 'Pro', 'Enterprise'] as const
  const safeTier = (VALID_HEADING_TIERS as readonly string[]).includes(currentTier)
    ? (currentTier as typeof VALID_HEADING_TIERS[number])
    : 'Free'
  const heading = headingMap[trigger]?.[safeTier] ?? headingMap['manual']['Free']

  return (
    <div
      className={`fixed inset-0 z-[100] flex items-center justify-center p-4 transition-all duration-300 ${
        visible ? 'opacity-100' : 'opacity-0'
      }`}
      role="dialog"
      aria-modal="true"
      aria-label="Abonelik planları"
    >
      {/* Backdrop */}
      <div
        className="absolute inset-0 bg-black/60 backdrop-blur-sm"
        onClick={handleClose}
        aria-hidden="true"
      />

      {/* Modal panel */}
      <div
        className={`relative z-10 w-full max-w-4xl max-h-[90vh] overflow-y-auto rounded-2xl bg-background border border-border shadow-2xl transition-all duration-300 ${
          visible ? 'scale-100 translate-y-0' : 'scale-95 translate-y-4'
        }`}
      >
        {/* Kapat butonu */}
        <button
          onClick={handleClose}
          className="absolute right-4 top-4 z-20 p-2 rounded-xl text-muted-foreground hover:text-foreground hover:bg-muted transition-all duration-200"
          aria-label="Kapat"
        >
          <X className="w-5 h-5" />
        </button>

        {/* Üst dekor */}
        <div className="absolute inset-x-0 top-0 h-px bg-gradient-to-r from-transparent via-primary/50 to-transparent" />

        <div className="p-6 sm:p-8">
          {/* Başlık */}
          <div className="text-center mb-8 space-y-2">
            <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-primary/10 text-primary text-xs font-semibold mb-3">
              <Zap className="w-3 h-3" />
              Abonelik Planları
            </div>
            <h2 className="text-2xl sm:text-3xl font-extrabold tracking-tight">{heading.title}</h2>
            <p className="text-muted-foreground">{heading.sub}</p>
          </div>

          {/* Plan kartları */}
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
            {PLANS.map((plan) => {
              const Icon = plan.icon
              const isCurrentPlan = PLAN_ID_TO_TIER[plan.id] === currentTier
              const isDowngrade =
                (currentTier === 'Enterprise' && plan.id !== 'enterprise') ||
                (currentTier === 'Pro' && plan.id === 'free')

              const ctaLabel = isCurrentPlan
                ? 'Mevcut Planın ✓'
                : isDowngrade
                ? 'Düşür'
                : plan.cta

              return (
                <div
                  key={plan.id}
                  className={`relative flex flex-col rounded-2xl border-2 p-5 transition-all duration-200 hover:-translate-y-0.5 ${
                    isCurrentPlan
                      ? `border-green-500/60 bg-gradient-to-b from-green-500/5 to-emerald-500/5 shadow-lg shadow-green-500/10`
                      : plan.popular
                      ? `${plan.borderClass} bg-gradient-to-b from-blue-500/5 to-purple-500/5 shadow-lg shadow-blue-500/10`
                      : `${plan.borderClass} bg-card hover:shadow-md`
                  }`}
                >
                  {/* Mevcut plan rozeti */}
                  {isCurrentPlan && (
                    <div className="absolute -top-3 left-1/2 -translate-x-1/2">
                      <span className="inline-flex items-center gap-1 px-3 py-0.5 rounded-full bg-gradient-to-r from-green-500 to-emerald-600 text-white text-[11px] font-bold">
                        ✓ Mevcut Planın
                      </span>
                    </div>
                  )}

                  {/* Popüler rozeti — mevcut plan değilse göster */}
                  {plan.popular && !isCurrentPlan && (
                    <div className="absolute -top-3 left-1/2 -translate-x-1/2">
                      <span className="inline-flex items-center gap-1 px-3 py-0.5 rounded-full bg-gradient-to-r from-blue-500 to-purple-600 text-white text-[11px] font-bold">
                        <Star className="w-2.5 h-2.5 fill-current" />
                        En Popüler
                      </span>
                    </div>
                  )}

                  {/* Plan başlık */}
                  <div className="flex items-center gap-2.5 mb-4">
                    <div className={`w-9 h-9 rounded-xl flex items-center justify-center bg-gradient-to-br ${plan.gradient}`}>
                      <Icon className="w-4.5 h-4.5 text-white w-[18px] h-[18px]" />
                    </div>
                    <div>
                      <p className="font-bold text-sm">{plan.name}</p>
                      <p className="text-xs text-muted-foreground">
                        {plan.price === 0 ? 'Ücretsiz' : `₺${plan.price}/ay`}
                      </p>
                    </div>
                  </div>

                  {/* Özellikler */}
                  <ul className="space-y-2 mb-5 flex-1">
                    {plan.features.map((f, i) => (
                      <li key={i} className="flex items-start gap-2 text-sm text-muted-foreground">
                        <div className={`mt-0.5 w-4 h-4 rounded-full flex items-center justify-center shrink-0 bg-gradient-to-br ${plan.gradient}`}>
                          <Check className="w-2.5 h-2.5 text-white" />
                        </div>
                        {f}
                      </li>
                    ))}
                  </ul>

                  {/* CTA */}
                  <button
                    onClick={() => void handlePlanSelect(plan.id)}
                    disabled={isCurrentPlan}
                    className={`w-full py-2.5 px-4 rounded-xl text-sm font-semibold flex items-center justify-center gap-2 transition-all duration-200 disabled:cursor-default
                      ${isCurrentPlan
                        ? 'bg-green-500/10 text-green-600 border-2 border-green-500/40 dark:text-green-400'
                        : plan.popular
                        ? `bg-gradient-to-r ${plan.gradient} text-white hover:opacity-90 shadow-md disabled:opacity-60`
                        : plan.id === 'enterprise'
                        ? `bg-gradient-to-r ${plan.gradient} text-white hover:opacity-90 disabled:opacity-60`
                        : 'bg-muted text-foreground hover:bg-muted/80 border border-border disabled:opacity-60'
                      }
                    `}
                  >
                    {ctaLabel}
                    {!isCurrentPlan && <ArrowRight className="w-3.5 h-3.5" />}
                  </button>
                </div>
              )
            })}
          </div>

          {/* Alt not */}
          <p className="text-center text-xs text-muted-foreground mt-6">
            KDV dahil · İstediğin zaman iptal et · Güvenli ödeme
          </p>
        </div>
      </div>
    </div>
  )
}
