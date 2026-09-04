'use client'

import { useEffect, useState } from 'react'
import { useSession } from 'next-auth/react'

export type SubscriptionTier = 'Free' | 'Pro' | 'Enterprise'
export type SubscriptionStatus =
  | 'Active'
  | 'Trialing'
  | 'PastDue'
  | 'Canceled'
  | 'Expired'
  | 'Paused'

export interface Subscription {
  tier: SubscriptionTier
  status: SubscriptionStatus
  currentPeriodEnd?: string
  cancelAtPeriodEnd?: boolean
}

export const TIER_META = {
  Free: {
    label: 'Ücretsiz',
    badge: null,
    // Avatar ring
    ringClass: '',
    ringStyle: undefined as React.CSSProperties | undefined,
    // Rozet renk
    badgeBg: '',
    badgeText: '',
    badgeBorder: '',
  },
  Pro: {
    label: 'Pro Maker',
    badge: '🏆 Pro Maker',
    ringClass: 'ring-[3px]',
    ringStyle: {
      boxShadow: '0 0 0 3px transparent',
      outline: '3px solid transparent',
      // Gradient ring via box-shadow trick
    } as React.CSSProperties,
    badgeBg: 'bg-gradient-to-r from-blue-500 to-purple-600',
    badgeText: 'text-white',
    badgeBorder: '',
  },
  Enterprise: {
    label: 'Enterprise',
    badge: '💎 Enterprise',
    ringClass: 'ring-[3px]',
    ringStyle: {} as React.CSSProperties,
    badgeBg: 'bg-gradient-to-r from-amber-400 to-pink-500',
    badgeText: 'text-white',
    badgeBorder: '',
  },
} as const

export function useSubscription() {
  const { data: session, status } = useSession()
  const [subscription, setSubscription] = useState<Subscription | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    if (status !== 'authenticated' || !session?.accessToken) {
      setLoading(false)
      return
    }

    fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/subscription/me`, {
      headers: { Authorization: `Bearer ${session.accessToken}` },
    })
      .then((r) => (r.ok ? r.json() : null))
      .then((data: Subscription | null) => {
        if (data) setSubscription(data)
      })
      .catch(() => {})
      .finally(() => setLoading(false))
  }, [session?.accessToken, status])

  const tier = subscription?.tier ?? 'Free'
  const isPro = tier === 'Pro'
  const isEnterprise = tier === 'Enterprise'
  const isPremium = isPro || isEnterprise
  const isActive =
    subscription?.status === 'Active' || subscription?.status === 'Trialing'

  return { subscription, loading, tier, isPro, isEnterprise, isPremium, isActive }
}
