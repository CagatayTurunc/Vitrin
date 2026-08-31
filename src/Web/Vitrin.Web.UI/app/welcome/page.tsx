'use client'

import { useEffect, useState, Suspense } from 'react'
import { useSession } from 'next-auth/react'
import { useRouter, useSearchParams } from 'next/navigation'
import Link from 'next/link'
import {
  Crown, Building2, Zap, BarChart3, Users, Rocket,
  ArrowRight, Check, Star, ChevronRight
} from 'lucide-react'

const PRO_FEATURES = [
  {
    icon: Rocket,
    title: 'Sınırsız Ürün Paylaşımı',
    description: 'İstediğin kadar ürün ekle, toplulukla paylaş.',
    action: 'Ürün Ekle',
    href: '/submit',
  },
  {
    icon: Zap,
    title: 'Günlük 50 AI Önerisi',
    description: 'AI destekli içerik iyileştirme ve tagline önerileri.',
    action: 'Ürün Düzenle',
    href: '/my-products',
  },
  {
    icon: BarChart3,
    title: '90 Gün Analitik Geçmişi',
    description: 'Ürünlerinin performansını detaylı görüntüle.',
    action: 'Dashboard',
    href: '/dashboard',
  },
  {
    icon: Star,
    title: '🏆 PRO Rozeti',
    description: 'Ürünlerin 🏆 PRO rozeti ile öne çıkar.',
    action: 'Profilini Gör',
    href: '/profile',
  },
]

const ENTERPRISE_FEATURES = [
  ...PRO_FEATURES,
  {
    icon: Users,
    title: '10 Ekip Üyesi',
    description: 'Ekibini davet et, birlikte yönetin.',
    action: 'Ekip Yönet',
    href: '/my-products',
  },
  {
    icon: Building2,
    title: '💎 FEATURED Rozeti',
    description: 'Öne çıkan ürünler bölümünde görün, daha fazla keşfedil.',
    action: 'Ürünlerini Gör',
    href: '/my-products',
  },
]

export default function WelcomePage() {
  return (
    <Suspense fallback={<div className="min-h-screen bg-background flex items-center justify-center" />}>
      <WelcomeContent />
    </Suspense>
  )
}

function WelcomeContent() {
  const { data: session } = useSession()
  const router = useRouter()
  const searchParams = useSearchParams()
  const tier = searchParams.get('tier') ?? 'ProMaker'
  const [step, setStep] = useState(0)

  const isPro = tier === 'ProMaker'
  const isEnterprise = tier === 'Enterprise'
  const features = isEnterprise ? ENTERPRISE_FEATURES : PRO_FEATURES
  const tierLabel = isEnterprise ? 'Enterprise 💎' : 'Pro Maker 🏆'
  const tierColor = isEnterprise
    ? 'from-amber-500 to-pink-600'
    : 'from-blue-500 to-purple-600'

  useEffect(() => {
    // Auto-advance steps
    if (step === 0) {
      const timer = setTimeout(() => setStep(1), 600)
      return () => clearTimeout(timer)
    }
  }, [step])

  if (!isPro && !isEnterprise) {
    router.push('/dashboard')
    return null
  }

  return (
    <div className="min-h-screen bg-background flex items-center justify-center px-4 py-16">
      <div className="max-w-2xl w-full">

        {/* Header */}
        <div
          className={`text-center mb-10 transition-all duration-700 ${
            step >= 1 ? 'opacity-100 translate-y-0' : 'opacity-0 translate-y-4'
          }`}
        >
          <div className={`inline-flex items-center justify-center w-20 h-20 rounded-2xl bg-gradient-to-br ${tierColor} mb-6 shadow-xl`}>
            {isEnterprise
              ? <Building2 className="w-10 h-10 text-white" />
              : <Crown className="w-10 h-10 text-white" />}
          </div>

          <h1 className="text-4xl font-extrabold tracking-tight mb-3">
            Hoş geldin, {session?.user?.name?.split(' ')[0] ?? 'Maker'}! 🎉
          </h1>
          <p className="text-xl text-muted-foreground">
            <span className={`font-bold bg-gradient-to-r ${tierColor} bg-clip-text text-transparent`}>
              {tierLabel}
            </span>{' '}
            planına geçişin başarılı.
          </p>
        </div>

        {/* Features */}
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 mb-8">
          {features.map((feature, i) => (
            <div
              key={feature.title}
              className={`rounded-2xl border border-border bg-card p-5 transition-all duration-500 ${
                step >= 1 ? 'opacity-100 translate-y-0' : 'opacity-0 translate-y-4'
              }`}
              style={{ transitionDelay: `${i * 80}ms` }}
            >
              <div className={`inline-flex p-2 rounded-xl bg-gradient-to-br ${tierColor} mb-3`}>
                <feature.icon className="w-5 h-5 text-white" />
              </div>
              <h3 className="font-bold text-sm mb-1">{feature.title}</h3>
              <p className="text-xs text-muted-foreground mb-3">{feature.description}</p>
              <Link
                href={feature.href}
                className="inline-flex items-center gap-1 text-xs font-semibold text-blue-500 hover:underline"
              >
                {feature.action}
                <ChevronRight className="w-3 h-3" />
              </Link>
            </div>
          ))}
        </div>

        {/* Checklist */}
        <div
          className={`rounded-2xl border border-border bg-card p-6 mb-6 transition-all duration-700 delay-300 ${
            step >= 1 ? 'opacity-100 translate-y-0' : 'opacity-0 translate-y-4'
          }`}
        >
          <h2 className="font-bold text-sm mb-4 text-muted-foreground uppercase tracking-wider">
            İlk adımlar
          </h2>
          <div className="space-y-3">
            {[
              { label: 'İlk ürününü ekle veya mevcut ürününü güncelle', href: '/submit' },
              { label: 'Dashboard\'da analitikleri incele', href: '/dashboard' },
              { label: 'Profilini ve rozetini görüntüle', href: '/profile' },
              ...(isEnterprise ? [{ label: 'Ekip üyelerini davet et', href: '/my-products' }] : []),
            ].map((item) => (
              <Link
                key={item.label}
                href={item.href}
                className="flex items-center gap-3 group"
              >
                <div className={`w-5 h-5 rounded-full border-2 border-border flex items-center justify-center group-hover:border-blue-500 transition-colors`}>
                  <Check className="w-3 h-3 text-muted-foreground/0 group-hover:text-blue-500 transition-colors" />
                </div>
                <span className="text-sm text-muted-foreground group-hover:text-foreground transition-colors">
                  {item.label}
                </span>
                <ArrowRight className="w-3.5 h-3.5 text-muted-foreground opacity-0 group-hover:opacity-100 transition-opacity ml-auto" />
              </Link>
            ))}
          </div>
        </div>

        {/* CTA */}
        <div
          className={`flex flex-col sm:flex-row gap-3 transition-all duration-700 delay-500 ${
            step >= 1 ? 'opacity-100 translate-y-0' : 'opacity-0 translate-y-4'
          }`}
        >
          <Link
            href="/submit"
            className={`flex-1 flex items-center justify-center gap-2 py-3 rounded-xl bg-gradient-to-r ${tierColor} text-white font-semibold hover:opacity-90 transition-opacity`}
          >
            <Rocket className="w-4 h-4" />
            İlk Ürünü Ekle
          </Link>
          <Link
            href="/dashboard"
            className="flex-1 flex items-center justify-center gap-2 py-3 rounded-xl border border-border font-semibold hover:bg-muted transition-colors text-sm"
          >
            Dashboard&apos;a Git
            <ArrowRight className="w-4 h-4" />
          </Link>
        </div>

        <p className="text-center text-xs text-muted-foreground mt-6">
          Aboneliğini{' '}
          <Link href="/settings" className="text-blue-500 hover:underline">
            Ayarlar
          </Link>{' '}
          sayfasından yönetebilirsin.
        </p>
      </div>
    </div>
  )
}
