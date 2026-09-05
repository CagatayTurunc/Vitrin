'use client'

import { Crown, Building2, Sparkles } from 'lucide-react'
import type { SubscriptionTier } from '@/hooks/use-subscription'

// ─── Tier konfigürasyonu ─────────────────────────────────────────────────────

export const TIER_CONFIG = {
  Free: {
    label: 'Ücretsiz',
    icon: Sparkles,
    // Avatar ring
    avatarRingClass: '',
    avatarRingInlineStyle: {} as React.CSSProperties,
    // Rozet
    badgeClass: 'bg-muted text-muted-foreground border border-border',
    // Glow
    glowClass: '',
  },
  Pro: {
    label: '🏆 Pro Maker',
    icon: Crown,
    avatarRingClass: 'ring-[3.5px] ring-offset-2 ring-offset-background',
    avatarRingInlineStyle: {
      // CSS custom property trick — tailwind ring color can't do gradient,
      // so we use outline trick with a pseudo-element via box-shadow
      boxShadow: '0 0 0 3px #7c3aed, 0 0 0 5px #3b82f6',
    } as React.CSSProperties,
    badgeClass:
      'bg-gradient-to-r from-blue-500 to-purple-600 text-white shadow-sm shadow-blue-500/30',
    glowClass: 'shadow-[0_0_20px_4px_rgba(124,58,237,0.25)]',
  },
  Enterprise: {
    label: '💎 Enterprise',
    icon: Building2,
    avatarRingClass: 'ring-[3.5px] ring-offset-2 ring-offset-background',
    avatarRingInlineStyle: {
      boxShadow: '0 0 0 3px #ec4899, 0 0 0 5px #f59e0b',
    } as React.CSSProperties,
    badgeClass:
      'bg-gradient-to-r from-amber-400 to-pink-500 text-white shadow-sm shadow-amber-400/30',
    glowClass: 'shadow-[0_0_20px_4px_rgba(245,158,11,0.25)]',
  },
} as const

// ─── Plan Rozeti ─────────────────────────────────────────────────────────────

interface SubscriptionBadgeProps {
  tier: SubscriptionTier
  /** 'sm' | 'md' (default) | 'lg' */
  size?: 'sm' | 'md' | 'lg'
  className?: string
}

export function SubscriptionBadge({
  tier,
  size = 'md',
  className = '',
}: SubscriptionBadgeProps) {
  if (!tier || tier === 'Free') return null

  const config = TIER_CONFIG[tier] ?? TIER_CONFIG['Free']
  const Icon = config.icon

  const sizeClasses = {
    sm: 'text-[10px] px-2 py-0.5 gap-1',
    md: 'text-xs px-2.5 py-1 gap-1.5',
    lg: 'text-sm px-3 py-1.5 gap-1.5',
  }

  const iconSizes = {
    sm: 'w-2.5 h-2.5',
    md: 'w-3 h-3',
    lg: 'w-3.5 h-3.5',
  }

  return (
    <span
      className={`inline-flex items-center rounded-full font-semibold ${sizeClasses[size]} ${config.badgeClass} ${className}`}
    >
      <Icon className={iconSizes[size]} />
      {config.label}
    </span>
  )
}

// ─── Avatar çerçeve yardımcısı ───────────────────────────────────────────────
// Gerçek avatar wrapper'ına uygulanacak sınıf + inline style döner

export function getAvatarRingProps(tier: SubscriptionTier) {
  const config = TIER_CONFIG[tier] ?? TIER_CONFIG['Free']
  return {
    ringClass: config.avatarRingClass,
    ringStyle: config.avatarRingInlineStyle,
    glowClass: config.glowClass,
  }
}

// ─── Plan Banner (kendi profilinde gösterilecek özel tebrik kutusu) ──────────

interface PlanBannerProps {
  tier: SubscriptionTier
  isActive: boolean
}

export function PlanBanner({ tier, isActive }: PlanBannerProps) {
  if (tier === 'Free' || !isActive) return null

  const isPro = tier === 'Pro'

  return (
    <div
      className={`relative overflow-hidden rounded-2xl border p-4 ${
        isPro
          ? 'border-blue-500/30 bg-gradient-to-r from-blue-500/10 to-purple-600/10'
          : 'border-amber-400/30 bg-gradient-to-r from-amber-400/10 to-pink-500/10'
      }`}
    >
      {/* Dekoratif arka plan parıltısı */}
      <div
        className={`absolute -right-6 -top-6 h-20 w-20 rounded-full blur-2xl opacity-40 ${
          isPro ? 'bg-purple-500' : 'bg-amber-400'
        }`}
      />

      <div className="relative flex items-center gap-3">
        <div
          className={`flex h-10 w-10 shrink-0 items-center justify-center rounded-xl text-xl ${
            isPro ? 'bg-blue-500/20' : 'bg-amber-400/20'
          }`}
        >
          {isPro ? '🏆' : '💎'}
        </div>
        <div>
          <p className={`font-bold text-sm ${isPro ? 'text-blue-500' : 'text-amber-500'}`}>
            {isPro ? 'Pro Maker Üyesisin' : 'Enterprise Üyesisin'}
          </p>
          <p className="text-xs text-muted-foreground mt-0.5">
            {isPro
              ? 'Sınırsız ürün, 50 AI/gün ve öncelikli listeleme aktif.'
              : 'Tüm Pro özellikleri + 10 ekip üyesi, API erişimi ve öncelikli destek aktif.'}
          </p>
        </div>
      </div>
    </div>
  )
}
