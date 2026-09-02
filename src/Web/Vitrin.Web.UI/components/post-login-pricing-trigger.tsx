'use client'

import { useEffect, useState } from 'react'
import { useSearchParams, useRouter, usePathname } from 'next/navigation'
import { useSession } from 'next-auth/react'
import { PricingModal } from './pricing-modal'

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

  useEffect(() => {
    // Oturum yüklendikten sonra kontrol et
    if (status !== 'authenticated') return
    if (searchParams.get('showPricing') !== '1') return

    // Free tier kullanıcılara göster (Pro/Enterprise zaten ödedi)
    // Subscription bilgisi yoksa göster — fetch maliyetli, basit tut
    const timer = setTimeout(() => setOpen(true), 600)
    return () => clearTimeout(timer)
  }, [status, searchParams])

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
    />
  )
}
