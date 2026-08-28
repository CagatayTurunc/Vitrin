import { redirect } from 'next/navigation'
import { getServerSession } from 'next-auth'
import { authOptions } from '@/lib/auth-options'
import { CheckCircle, XCircle, ArrowRight } from 'lucide-react'
import Link from 'next/link'

const API_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000'

interface CallbackPageProps {
  searchParams: Promise<{ token?: string; status?: string }>
}

export default async function PricingCallbackPage({ searchParams }: CallbackPageProps) {
  const params = await searchParams
  const { token, status } = params

  const session = await getServerSession(authOptions)

  // İyzico'dan dönen token ile callback endpoint'i çağır
  if (token && session?.accessToken) {
    try {
      const res = await fetch(
        `${API_URL}/api/subscription/callback?token=${token}`,
        {
          headers: {
            Authorization: `Bearer ${session.accessToken}`,
          },
          cache: 'no-store',
        }
      )

      if (res.ok) {
        // Başarılı ödeme
        return (
          <div className="min-h-screen bg-background flex items-center justify-center px-4">
            <div className="max-w-md w-full text-center space-y-6">
              <div className="flex justify-center">
                <div className="w-20 h-20 rounded-full bg-emerald-500/10 flex items-center justify-center">
                  <CheckCircle className="w-10 h-10 text-emerald-500" />
                </div>
              </div>
              <div>
                <h1 className="text-3xl font-extrabold mb-2">Abonelik Aktif! 🎉</h1>
                <p className="text-muted-foreground">
                  Ödemeniz başarıyla tamamlandı. Pro özelliklerine hemen erişebilirsiniz.
                </p>
              </div>
              <div className="flex flex-col gap-3">
                <Link
                  href="/dashboard"
                  className="flex items-center justify-center gap-2 w-full py-3 rounded-xl bg-gradient-to-r from-blue-500 to-purple-600 text-white font-semibold hover:opacity-90 transition-opacity"
                >
                  Dashboard&apos;a Git
                  <ArrowRight className="w-4 h-4" />
                </Link>
                <Link
                  href="/submit"
                  className="w-full py-3 rounded-xl border border-border text-sm font-medium hover:bg-muted transition-colors"
                >
                  Ürün Paylaş
                </Link>
              </div>
            </div>
          </div>
        )
      }
    } catch {
      // Hata durumuna düş
    }
  }

  // Başarısız veya iptal durumu
  if (status === 'failure' || status === 'cancel') {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center px-4">
        <div className="max-w-md w-full text-center space-y-6">
          <div className="flex justify-center">
            <div className="w-20 h-20 rounded-full bg-red-500/10 flex items-center justify-center">
              <XCircle className="w-10 h-10 text-red-500" />
            </div>
          </div>
          <div>
            <h1 className="text-3xl font-extrabold mb-2">Ödeme Başarısız</h1>
            <p className="text-muted-foreground">
              {status === 'cancel'
                ? 'Ödeme işlemini iptal ettiniz.'
                : 'Ödeme sırasında bir hata oluştu. Lütfen tekrar deneyin.'}
            </p>
          </div>
          <div className="flex flex-col gap-3">
            <Link
              href="/pricing"
              className="flex items-center justify-center gap-2 w-full py-3 rounded-xl bg-gradient-to-r from-blue-500 to-purple-600 text-white font-semibold hover:opacity-90 transition-opacity"
            >
              Tekrar Dene
            </Link>
            <Link
              href="/"
              className="w-full py-3 rounded-xl border border-border text-sm font-medium hover:bg-muted transition-colors"
            >
              Ana Sayfaya Dön
            </Link>
          </div>
        </div>
      </div>
    )
  }

  // Token yoksa pricing sayfasına yönlendir
  redirect('/pricing')
}
