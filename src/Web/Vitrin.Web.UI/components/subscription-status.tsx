'use client'

import { useEffect, useState } from 'react'
import { useSession } from 'next-auth/react'
import Link from 'next/link'
import { Crown, Building2, Sparkles, AlertTriangle, ArrowRight, X } from 'lucide-react'

interface Subscription {
  tier: 'Free' | 'Pro' | 'Enterprise'
  status: 'Active' | 'Trialing' | 'PastDue' | 'Canceled' | 'Expired' | 'Paused'
  currentPeriodEnd?: string
  cancelAtPeriodEnd?: boolean
}

const TIER_CONFIG = {
  Free: {
    label: 'Ücretsiz',
    icon: Sparkles,
    color: 'text-muted-foreground',
    bg: 'bg-muted',
  },
  Pro: {
    label: '🏆 Pro Maker',
    icon: Crown,
    color: 'text-blue-500',
    bg: 'bg-blue-500/10',
  },
  Enterprise: {
    label: '💎 Enterprise',
    icon: Building2,
    color: 'text-amber-500',
    bg: 'bg-amber-500/10',
  },
}

export function SubscriptionStatus() {
  const { data: session } = useSession()
  const [subscription, setSubscription] = useState<Subscription | null>(null)
  const [loading, setLoading] = useState(true)
  const [dismissedBanner, setDismissedBanner] = useState(false)
  const [canceling, setCanceling] = useState(false)

  useEffect(() => {
    if (!session?.accessToken) {
      setLoading(false)
      return
    }

    fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/subscription/me`, {
      headers: { Authorization: `Bearer ${session.accessToken}` },
    })
      .then((r) => r.ok ? r.json() : null)
      .then((data) => {
        if (data) setSubscription(data as Subscription)
      })
      .catch(() => {})
      .finally(() => setLoading(false))
  }, [session?.accessToken])

  const handleCancel = async () => {
    if (!session?.accessToken || !confirm('Aboneliğinizi iptal etmek istediğinizden emin misiniz?')) return

    setCanceling(true)
    try {
      const res = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/subscription/cancel`, {
        method: 'POST',
        headers: { Authorization: `Bearer ${session.accessToken}` },
      })
      if (res.ok) {
        setSubscription((prev) => prev ? { ...prev, cancelAtPeriodEnd: true } : prev)
      }
    } catch {
      //
    } finally {
      setCanceling(false)
    }
  }

  if (loading || !subscription) return null

  const config = TIER_CONFIG[subscription.tier]
  const Icon = config.icon
  const isActive = subscription.status === 'Active' || subscription.status === 'Trialing'
  const isPastDue = subscription.status === 'PastDue'
  const isFree = subscription.tier === 'Free'

  const periodEnd = subscription.currentPeriodEnd
    ? new Date(subscription.currentPeriodEnd).toLocaleDateString('tr-TR', {
        day: 'numeric', month: 'long', year: 'numeric',
      })
    : null

  return (
    <div className="space-y-3">
      {/* Plan kartı */}
      <div className={`flex items-center justify-between rounded-xl border border-border p-4 ${config.bg}`}>
        <div className="flex items-center gap-3">
          <div className={`p-2 rounded-lg ${config.bg}`}>
            <Icon className={`w-5 h-5 ${config.color}`} />
          </div>
          <div>
            <p className={`font-semibold text-sm ${config.color}`}>{config.label}</p>
            <p className="text-xs text-muted-foreground">
              {isFree
                ? 'Temel özellikler aktif'
                : isActive
                ? subscription.cancelAtPeriodEnd
                  ? `${periodEnd} tarihinde sona erer`
                  : `${periodEnd} tarihinde yenilenir`
                : isPastDue
                ? 'Ödeme bekliyor'
                : 'Abonelik pasif'}
            </p>
          </div>
        </div>

        {isFree ? (
          <Link
            href="/pricing"
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-gradient-to-r from-blue-500 to-purple-600 text-white text-xs font-semibold hover:opacity-90 transition-opacity"
          >
            Yükselt
            <ArrowRight className="w-3 h-3" />
          </Link>
        ) : isActive && !subscription.cancelAtPeriodEnd ? (
          <button
            onClick={handleCancel}
            disabled={canceling}
            className="text-xs text-muted-foreground hover:text-red-500 transition-colors"
          >
            {canceling ? 'İptal ediliyor...' : 'Aboneliği iptal et'}
          </button>
        ) : subscription.cancelAtPeriodEnd ? (
          <Link
            href="/pricing"
            className="text-xs text-blue-500 hover:underline"
          >
            Yeniden aktifleştir
          </Link>
        ) : null}
      </div>

      {/* Past due uyarısı */}
      {isPastDue && (
        <div className="flex items-start gap-3 p-3 rounded-xl bg-amber-500/10 border border-amber-500/20">
          <AlertTriangle className="w-4 h-4 text-amber-500 mt-0.5 shrink-0" />
          <div className="flex-1">
            <p className="text-sm font-medium text-amber-500">Ödeme başarısız</p>
            <p className="text-xs text-muted-foreground">Aboneliğiniz için ödeme alınamadı.</p>
          </div>
          <Link href="/pricing" className="text-xs text-amber-500 hover:underline shrink-0">
            Güncelle
          </Link>
        </div>
      )}

      {/* Free tier upgrade banner */}
      {isFree && !dismissedBanner && (
        <div className="relative flex items-start gap-3 p-3 rounded-xl bg-gradient-to-r from-blue-500/10 to-purple-500/10 border border-blue-500/20">
          <Crown className="w-4 h-4 text-blue-500 mt-0.5 shrink-0" />
          <div className="flex-1">
            <p className="text-sm font-medium">Pro ile sınırları kaldır</p>
            <p className="text-xs text-muted-foreground">
              Sınırsız ürün, 50 AI/gün ve PRO rozeti için sadece ₺299/ay
            </p>
          </div>
          <button
            onClick={() => setDismissedBanner(true)}
            className="text-muted-foreground hover:text-foreground transition-colors shrink-0"
          >
            <X className="w-3.5 h-3.5" />
          </button>
        </div>
      )}
    </div>
  )
}
