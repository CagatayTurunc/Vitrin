'use client'

import { useState } from 'react'
import { Crown, X, Zap, ArrowRight, TrendingUp } from 'lucide-react'
import { PricingModal } from './pricing-modal'

interface UpgradePromoBannerProps {
  /** Farklı varyantlar için farklı tasarım */
  variant?: 'hero' | 'inline' | 'sticky'
}

export function UpgradePromoBanner({ variant = 'inline' }: UpgradePromoBannerProps) {
  const [open, setOpen] = useState(false)
  const [dismissed, setDismissed] = useState(false)

  if (dismissed) return null

  // ── HERO variant — büyük, dikkat çekici kart ──────────────────────────────
  if (variant === 'hero') {
    return (
      <>
        <div
          onClick={() => setOpen(true)}
          className="group relative overflow-hidden rounded-2xl border border-blue-500/30 bg-gradient-to-br from-blue-500/10 via-purple-500/5 to-pink-500/10 p-6 cursor-pointer transition-all duration-300 hover:border-blue-500/60 hover:shadow-lg hover:shadow-blue-500/10 hover:-translate-y-0.5"
          role="button"
          tabIndex={0}
          onKeyDown={(e) => e.key === 'Enter' && setOpen(true)}
          aria-label="Planları görüntüle"
        >
          {/* Arka plan glow */}
          <div className="absolute -right-8 -top-8 w-32 h-32 rounded-full bg-gradient-to-br from-blue-500/20 to-purple-500/20 blur-2xl group-hover:scale-150 transition-transform duration-500" />

          <div className="relative flex items-center justify-between gap-4">
            <div className="flex items-center gap-4">
              <div className="flex-shrink-0 w-12 h-12 rounded-2xl bg-gradient-to-br from-blue-500 to-purple-600 flex items-center justify-center shadow-lg shadow-blue-500/30">
                <Crown className="w-6 h-6 text-white" />
              </div>
              <div>
                <p className="font-bold text-foreground">Görünür ol, öne çık!</p>
                <p className="text-sm text-muted-foreground mt-0.5">
                  PRO veya Enterprise ile ürününü topluluğun önüne taşı
                </p>
              </div>
            </div>

            <div className="flex-shrink-0 flex items-center gap-2 px-4 py-2 rounded-xl bg-gradient-to-r from-blue-500 to-purple-600 text-white text-sm font-semibold group-hover:opacity-90 transition-opacity shadow-md">
              Planları Gör
              <ArrowRight className="w-4 h-4 group-hover:translate-x-0.5 transition-transform" />
            </div>
          </div>

          {/* Feature pills */}
          <div className="relative mt-4 flex flex-wrap gap-2">
            {['🏆 PRO Rozet', '⚡ Öncelikli Liste', '📊 Gelişmiş Analitik', '💎 Featured'].map((f) => (
              <span
                key={f}
                className="inline-flex items-center px-2.5 py-1 rounded-full bg-white/10 dark:bg-white/5 border border-white/10 text-xs font-medium text-foreground/70"
              >
                {f}
              </span>
            ))}
          </div>
        </div>

        <PricingModal open={open} onClose={() => setOpen(false)} trigger="manual" />
      </>
    )
  }

  // ── STICKY variant — sayfanın altından yüzen bar ───────────────────────────
  if (variant === 'sticky') {
    return (
      <>
        <div className="fixed bottom-4 left-1/2 -translate-x-1/2 z-50 w-full max-w-lg px-4">
          <div className="relative flex items-center justify-between gap-3 rounded-2xl border border-blue-500/40 bg-background/95 backdrop-blur-md px-4 py-3 shadow-2xl shadow-blue-500/10">
            {/* Dismiss */}
            <button
              onClick={() => setDismissed(true)}
              className="absolute -right-2 -top-2 w-5 h-5 rounded-full bg-muted border border-border flex items-center justify-center text-muted-foreground hover:text-foreground transition-colors"
              aria-label="Kapat"
            >
              <X className="w-3 h-3" />
            </button>

            <div className="flex items-center gap-3">
              <div className="w-8 h-8 rounded-xl bg-gradient-to-br from-blue-500 to-purple-600 flex items-center justify-center">
                <Zap className="w-4 h-4 text-white" />
              </div>
              <div>
                <p className="text-sm font-semibold">Görünür ol, öne çık!</p>
                <p className="text-xs text-muted-foreground">PRO ile ürününü topluluğa taşı</p>
              </div>
            </div>

            <button
              onClick={() => setOpen(true)}
              className="flex-shrink-0 flex items-center gap-1.5 px-3 py-1.5 rounded-xl bg-gradient-to-r from-blue-500 to-purple-600 text-white text-xs font-bold hover:opacity-90 transition-opacity"
            >
              Planları Gör
              <ArrowRight className="w-3 h-3" />
            </button>
          </div>
        </div>

        <PricingModal open={open} onClose={() => setOpen(false)} trigger="manual" />
      </>
    )
  }

  // ── INLINE variant (default) — satır içi küçük banner ──────────────────────
  return (
    <>
      <div
        onClick={() => setOpen(true)}
        role="button"
        tabIndex={0}
        onKeyDown={(e) => e.key === 'Enter' && setOpen(true)}
        aria-label="Abonelik planlarını gör"
        className="group flex items-center justify-between gap-3 rounded-xl border border-blue-500/20 bg-gradient-to-r from-blue-500/8 to-purple-500/5 px-4 py-3 cursor-pointer transition-all duration-200 hover:border-blue-500/40 hover:shadow-md hover:shadow-blue-500/5 hover:-translate-y-0.5"
      >
        <div className="flex items-center gap-3">
          <div className="w-8 h-8 rounded-lg bg-gradient-to-br from-blue-500 to-purple-600 flex items-center justify-center shadow-sm">
            <TrendingUp className="w-4 h-4 text-white" />
          </div>
          <div>
            <p className="text-sm font-semibold text-foreground">Abone ol, görünür ol</p>
            <p className="text-xs text-muted-foreground">PRO ile ürününü öne çıkar</p>
          </div>
        </div>

        <div className="flex items-center gap-1.5 text-xs font-bold text-blue-500 group-hover:gap-2 transition-all">
          Planları Gör
          <ArrowRight className="w-3.5 h-3.5 group-hover:translate-x-0.5 transition-transform" />
        </div>
      </div>

      <PricingModal open={open} onClose={() => setOpen(false)} trigger="manual" />
    </>
  )
}
