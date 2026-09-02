'use client'

import { useState, useEffect } from 'react'
import { useSession } from 'next-auth/react'
import { useRouter, useParams } from 'next/navigation'
import Link from 'next/link'
import {
  Crown,
  Building2,
  Check,
  Shield,
  CreditCard,
  ArrowRight,
  ArrowLeft,
  Sparkles,
  Star,
  Zap,
  Lock,
  RefreshCcw,
} from 'lucide-react'

const PLAN_DETAILS = {
  pro: {
    id: 'pro',
    tierValue: 1,   // SubscriptionTier.ProMaker = 1
    name: 'Pro Maker',
    price: 299,
    badge: '🏆 PRO',
    icon: Crown,
    gradient: 'from-blue-500 to-purple-600',
    glow: 'shadow-blue-500/20',
    features: [
      'Sınırsız ürün paylaşımı',
      'Günlük 50 AI önerisi',
      '90 gün gelişmiş analitik',
      '🏆 PRO rozeti profilde',
      'Ürün öne çıkarma',
      'Öncelikli listeleme',
      'Email bildirimleri',
    ],
    highlight: 'Türkiye\'nin en aktif maker\'larının tercihi',
  },
  enterprise: {
    id: 'enterprise',
    tierValue: 2,   // SubscriptionTier.Enterprise = 2
    name: 'Enterprise',
    price: 999,
    badge: '💎 ENTERPRISE',
    icon: Building2,
    gradient: 'from-amber-500 to-pink-500',
    glow: 'shadow-amber-500/20',
    features: [
      "Pro'daki her şey dahil",
      '10 ekip üyesi hesabı',
      'Günlük 200 AI önerisi',
      '1 yıl analitik geçmişi',
      '💎 FEATURED rozeti + animasyon',
      'Öne çıkan ürün bölümü',
      'API erişimi',
      'Öncelikli destek (SLA)',
      'Özel onboarding görüşmesi',
    ],
    highlight: 'Şirketler ve büyük ekipler için tasarlandı',
  },
}

const GUARANTEES = [
  { icon: RefreshCcw, text: 'İstediğin zaman iptal et' },
  { icon: Lock, text: 'SSL şifreli güvenli ödeme' },
  { icon: Shield, text: 'İyzico güvencesiyle' },
  { icon: CreditCard, text: 'Tüm kartlar kabul edilir' },
]

export default function CheckoutPage() {
  const params = useParams()
  const planId = params.plan as string
  const plan = PLAN_DETAILS[planId as keyof typeof PLAN_DETAILS]

  const { data: session, status } = useSession()
  const router = useRouter()
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [mounted, setMounted] = useState(false)

  useEffect(() => {
    setMounted(true)
    if (status === 'unauthenticated') {
      router.push(`/login?redirect=/checkout/${planId}`)
    }
  }, [status, planId, router])

  // Geçersiz plan
  if (!plan) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="text-center space-y-4">
          <p className="text-muted-foreground">Geçersiz plan seçimi.</p>
          <Link href="/pricing" className="text-primary hover:underline">
            Planlara geri dön
          </Link>
        </div>
      </div>
    )
  }

  const Icon = plan.icon

  const handleCheckout = async () => {
    if (!session?.accessToken) {
      router.push(`/login?redirect=/checkout/${planId}`)
      return
    }

    setLoading(true)
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
          body: JSON.stringify({ tier: plan.tierValue }),
        }
      )

      if (!res.ok) {
        const data = await res.json().catch(() => ({})) as { detail?: string; title?: string }
        throw new Error(data.detail ?? data.title ?? 'Ödeme başlatılamadı. Lütfen tekrar deneyin.')
      }

      const data = await res.json() as {
        checkoutFormContent?: string
        paymentPageUrl?: string
        checkoutUrl?: string
      }

      const redirectUrl = data.paymentPageUrl ?? data.checkoutUrl
      if (redirectUrl) {
        window.location.href = redirectUrl
        return
      }

      // İyzico inline form
      if (data.checkoutFormContent) {
        const container = document.getElementById('iyzico-checkout-container')
        if (container) {
          container.innerHTML = data.checkoutFormContent
          const script = container.querySelector('script')
          if (script?.src) {
            const s = document.createElement('script')
            s.src = script.src
            document.body.appendChild(s)
          }
        }
        return
      }

      throw new Error('Ödeme sayfası yüklenemedi.')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Beklenmeyen bir hata oluştu.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="min-h-screen bg-background">
      {/* Arka plan dekor */}
      <div className="fixed inset-0 pointer-events-none overflow-hidden">
        <div className={`absolute top-0 right-0 w-[500px] h-[500px] rounded-full blur-3xl opacity-10 bg-gradient-to-br ${plan.gradient}`} />
        <div className="absolute bottom-0 left-0 w-[400px] h-[400px] rounded-full blur-3xl opacity-5 bg-gradient-to-tr from-primary to-primary/50" />
      </div>

      <div className="relative max-w-5xl mx-auto px-4 py-12">
        {/* Geri butonu */}
        <Link
          href="/pricing"
          className="inline-flex items-center gap-2 text-sm text-muted-foreground hover:text-foreground transition-colors mb-8 group"
        >
          <ArrowLeft className="w-4 h-4 group-hover:-translate-x-0.5 transition-transform" />
          Planlara geri dön
        </Link>

        <div className="grid grid-cols-1 lg:grid-cols-5 gap-8 items-start">

          {/* Sol — Plan detayı */}
          <div className={`lg:col-span-3 space-y-6 transition-all duration-700 ${mounted ? 'opacity-100 translate-y-0' : 'opacity-0 translate-y-4'}`}>
            {/* Plan başlık kartı */}
            <div className={`rounded-2xl border-2 border-transparent bg-gradient-to-br from-card to-card/80 p-6 shadow-xl ${plan.glow} relative overflow-hidden`}
              style={{ backgroundOrigin: 'border-box' }}
            >
              {/* Gradient border efekti */}
              <div className={`absolute inset-0 rounded-2xl bg-gradient-to-br ${plan.gradient} opacity-10`} />

              <div className="relative">
                <div className="flex items-center gap-4 mb-4">
                  <div className={`w-14 h-14 rounded-2xl flex items-center justify-center bg-gradient-to-br ${plan.gradient} shadow-lg`}>
                    <Icon className="w-7 h-7 text-white" />
                  </div>
                  <div>
                    <div className="flex items-center gap-2 mb-1">
                      <h1 className="text-2xl font-extrabold">{plan.name}</h1>
                      <span className={`px-2.5 py-0.5 rounded-full text-xs font-bold text-white bg-gradient-to-r ${plan.gradient}`}>
                        {plan.badge}
                      </span>
                    </div>
                    <p className="text-sm text-muted-foreground">{plan.highlight}</p>
                  </div>
                </div>

                {/* Fiyat */}
                <div className="flex items-end gap-2 mb-6 pb-6 border-b border-border/50">
                  <span className="text-5xl font-extrabold tracking-tight">₺{plan.price}</span>
                  <span className="text-muted-foreground mb-2">/ay · KDV dahil</span>
                </div>

                {/* Özellikler */}
                <div className="space-y-3">
                  <p className="text-sm font-semibold text-muted-foreground uppercase tracking-wider">
                    Dahil olanlar
                  </p>
                  <ul className="space-y-2.5">
                    {plan.features.map((f, i) => (
                      <li key={i} className="flex items-center gap-3 text-sm">
                        <div className={`w-5 h-5 rounded-full flex items-center justify-center bg-gradient-to-br ${plan.gradient} shrink-0`}>
                          <Check className="w-3 h-3 text-white" />
                        </div>
                        <span>{f}</span>
                      </li>
                    ))}
                  </ul>
                </div>
              </div>
            </div>

            {/* Garanti bilgileri */}
            <div className="grid grid-cols-2 gap-3">
              {GUARANTEES.map(({ icon: GIcon, text }) => (
                <div key={text} className="flex items-center gap-2.5 p-3 rounded-xl bg-muted/50 border border-border/50">
                  <GIcon className="w-4 h-4 text-muted-foreground shrink-0" />
                  <span className="text-xs text-muted-foreground">{text}</span>
                </div>
              ))}
            </div>
          </div>

          {/* Sağ — Ödeme onay */}
          <div className={`lg:col-span-2 space-y-4 lg:sticky lg:top-8 transition-all duration-700 delay-150 ${mounted ? 'opacity-100 translate-y-0' : 'opacity-0 translate-y-4'}`}>
            <div className="rounded-2xl border border-border bg-card p-6 shadow-lg">
              <h2 className="text-lg font-bold mb-1">Sipariş Özeti</h2>
              <p className="text-sm text-muted-foreground mb-5">Ödemeye geçmeden önce kontrol et</p>

              {/* Özet satırları */}
              <div className="space-y-3 mb-5">
                <div className="flex justify-between text-sm">
                  <span className="text-muted-foreground">{plan.name}</span>
                  <span className="font-medium">₺{plan.price}/ay</span>
                </div>
                <div className="flex justify-between text-sm">
                  <span className="text-muted-foreground">KDV</span>
                  <span className="font-medium text-emerald-500">Dahil</span>
                </div>
                <div className="border-t border-border pt-3 flex justify-between font-bold">
                  <span>Toplam</span>
                  <span className={`bg-gradient-to-r ${plan.gradient} bg-clip-text text-transparent text-lg`}>
                    ₺{plan.price}/ay
                  </span>
                </div>
              </div>

              {/* Hata */}
              {error && (
                <div className="mb-4 p-3 rounded-xl bg-red-500/10 border border-red-500/20 text-sm text-red-500">
                  {error}
                </div>
              )}

              {/* Ödeme butonu */}
              <button
                onClick={handleCheckout}
                disabled={loading || status !== 'authenticated'}
                className={`w-full py-3.5 px-4 rounded-xl font-bold text-white flex items-center justify-center gap-2 transition-all duration-200
                  bg-gradient-to-r ${plan.gradient} hover:opacity-90 active:scale-[0.98] shadow-md
                  disabled:opacity-60 disabled:cursor-not-allowed`}
              >
                {loading ? (
                  <>
                    <span className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                    Yükleniyor...
                  </>
                ) : (
                  <>
                    <CreditCard className="w-4 h-4" />
                    Ödemeye Geç
                    <ArrowRight className="w-4 h-4" />
                  </>
                )}
              </button>

              {/* İyzico container — inline form için */}
              <div id="iyzico-checkout-container" className="mt-4" />

              <p className="text-center text-xs text-muted-foreground mt-4 flex items-center justify-center gap-1">
                <Lock className="w-3 h-3" />
                256-bit SSL şifreli güvenli ödeme
              </p>
            </div>

            {/* Hesap yok uyarısı */}
            {status === 'unauthenticated' && (
              <div className="p-4 rounded-xl bg-amber-500/10 border border-amber-500/20 text-sm text-amber-600 dark:text-amber-400">
                Ödeme yapmak için önce{' '}
                <Link href={`/login?redirect=/checkout/${planId}`} className="font-bold underline">
                  giriş yapman
                </Link>{' '}
                gerekiyor.
              </div>
            )}

            {/* Kullanıcı bilgisi */}
            {session?.user && (
              <div className="p-3 rounded-xl bg-muted/50 border border-border flex items-center gap-3 text-sm">
                <div className="w-8 h-8 rounded-full bg-gradient-to-br from-primary to-primary/50 flex items-center justify-center text-white font-bold text-xs shrink-0">
                  {session.user.name?.[0]?.toUpperCase() ?? '?'}
                </div>
                <div className="min-w-0">
                  <p className="font-medium truncate">{session.user.name}</p>
                  <p className="text-xs text-muted-foreground truncate">{session.user.email}</p>
                </div>
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  )
}
