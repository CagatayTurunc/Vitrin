'use client'

import { useEffect, useState } from 'react'
import { useSearchParams, useRouter, usePathname } from 'next/navigation'
import { useSession } from 'next-auth/react'
import { PricingModal } from './pricing-modal'

type SubscriptionTier = 'Free' | 'Pro' | 'Enterprise'

interface SubscriptionInfo {
  tier: SubscriptionTier
  status: string
}

/**
 * Login sonrası ?showPricing=1 param'ı varsa pricing modal'ını açar.
 * Layout'a ya da herhangi bir istemci bileşenine mount edilir.
 */
export function PostLoginPricingTrigger() {
  const searchParams = useSearchParams()
  const router = useRouter()
  const pathname = usePathname()
  const { data: session, status } = useSession()
  const [open, setOpen] = useState(false)
  const [currentTier, setCurrentTier] = useState<SubscriptionTier>('Free')

  useEffect(() => {
    // Oturum yüklendikten sonra kontrol et
    if (status !== 'authenticated') return
    if (searchParams.get('showPricing') !== '1') return
    if (!session?.accessToken) return

    // Kullanıcının mevcut planını çek — herkese modal göster ama aktif planı vurgula
    const VALID_TIERS: SubscriptionTier[] = ['Free', 'Pro', 'Enterprise']
    fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/subscription/me`, {
      headers: { Authorization: `Bearer ${session.accessToken}` },
    })
      .then((r) => (r.ok ? r.json() : null))
      .then((data: SubscriptionInfo | null) => {
        const tier = data?.tier
        if (tier && VALID_TIERS.includes(tier)) setCurrentTier(tier)
      })
      .catch(() => {
        // Hata durumunda Free varsay
      })
      .finally(() => {
        const timer = setTimeout(() => setOpen(true), 600)
        // cleanup için timer'ı kaydet — finally içinde doğrudan dönüş yok
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        ;(window as any).__pricingTriggerTimer = timer
      })

    return () => {
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      clearTimeout((window as any).__pricingTriggerTimer)
    }
  }, [status, searchParams, session?.accessToken])

  const handleClose = () => {
    setOpen(false)
    // URL'den param'ı temizle (back navigation bozulmasın)
    const params = new URLSearchParams(searchParams.toString())
    params.delete('showPricing')
    const newUrl = params.size > 0 ? `${pathname}?${params.toString()}` : pathname
    router.replace(newUrl, { scroll: false })
  }

  if (!session) return null

  return (
    <PricingModal
      open={open}
      onClose={handleClose}
      trigger="login"
      currentTier={currentTier}
    />
  )
}
