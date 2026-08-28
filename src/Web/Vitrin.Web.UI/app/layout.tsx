import { Analytics } from '@vercel/analytics/next'
import type { Metadata, Viewport } from 'next'
import './globals.css'
import NextAuthProvider from "@/components/next-auth-provider";
import { SiteHeader } from "@/components/site-header";
import { SiteFooter } from "@/components/site-footer";
import { ThemeProvider } from "@/components/theme-provider";
import { Toaster } from "@/components/ui/toaster";
import { getOrganizationSchema, getWebSiteSchema, renderJsonLd } from '@/lib/seo'

const siteUrl = process.env.NEXT_PUBLIC_SITE_URL ?? 'http://localhost:3001'

export const metadata: Metadata = {
  metadataBase: new URL(siteUrl),
  title: {
    default: 'Vitrin — Günün Ürünleri',
    template: '%s — Vitrin',
  },
  description:
    'Vitrin, en yeni ürünleri keşfedeceğin, oy vereceğin ve paylaşacağın ürün keşif platformu. Türkiye\'nin en büyük ürün keşif topluluğu.',
  keywords: [
    'ürün keşfi',
    'yeni ürünler',
    'startup',
    'girişim',
    'teknoloji',
    'product hunt',
    'türkiye',
    'yazılım',
    'uygulama',
  ],
  authors: [{ name: 'Vitrin Ekibi' }],
  creator: 'Vitrin',
  publisher: 'Vitrin',
  generator: 'Next.js',
  alternates: {
    canonical: siteUrl,
  },
  robots: {
    index: true,
    follow: true,
    googleBot: {
      index: true,
      follow: true,
      'max-video-preview': -1,
      'max-image-preview': 'large',
      'max-snippet': -1,
    },
  },
  openGraph: {
    type: 'website',
    locale: 'tr_TR',
    url: siteUrl,
    title: 'Vitrin — Günün Ürünleri',
    description: 'Türkiye\'nin en yeni ürün keşif platformu',
    siteName: 'Vitrin',
    images: [
      {
        url: `${siteUrl}/og-image.png`,
        width: 1200,
        height: 630,
        alt: 'Vitrin — Günün Ürünleri',
      },
    ],
  },
  twitter: {
    card: 'summary_large_image',
    title: 'Vitrin — Günün Ürünleri',
    description: 'Türkiye\'nin en yeni ürün keşif platformu',
    creator: '@vitrinapp',
    site: '@vitrinapp',
    images: [`${siteUrl}/og-image.png`],
  },
  icons: {
    icon: [
      {
        url: '/icon-light-32x32.png',
        media: '(prefers-color-scheme: light)',
      },
      {
        url: '/icon-dark-32x32.png',
        media: '(prefers-color-scheme: dark)',
      },
      {
        url: '/icon.svg',
        type: 'image/svg+xml',
      },
    ],
    apple: '/apple-icon.png',
  },
  // Madde 2.4: Google Analytics tracking için
  verification: {
    google: process.env.NEXT_PUBLIC_GOOGLE_SITE_VERIFICATION,
    // Diğer verification kodları
  },
}

export const viewport: Viewport = {
  width: 'device-width',
  initialScale: 1,
  colorScheme: 'light dark',
  themeColor: [
    { media: '(prefers-color-scheme: light)', color: 'white' },
    { media: '(prefers-color-scheme: dark)', color: 'black' },
  ],
}

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode
}>) {
  // Madde 2.7: Schema.org structured data
  const organizationSchema = getOrganizationSchema()
  const websiteSchema = getWebSiteSchema()

  return (
    <html
      lang="tr"
      suppressHydrationWarning
    >
      <head>
        {/* Madde 2.7: Global Schema.org */}
        <script
          type="application/ld+json"
          dangerouslySetInnerHTML={renderJsonLd(organizationSchema)}
        />
        <script
          type="application/ld+json"
          dangerouslySetInnerHTML={renderJsonLd(websiteSchema)}
        />
      </head>
      <body className="font-body antialiased min-h-screen bg-background text-foreground">
        <NextAuthProvider>
          <ThemeProvider
            attribute="class"
            defaultTheme="system"
            enableSystem
            disableTransitionOnChange
          >
            <div className="relative flex min-h-screen flex-col">
              <SiteHeader />
              <div className="flex-1">
                {children}
              </div>
              <SiteFooter />
            </div>
            <Toaster />
            <Analytics />
          </ThemeProvider>
        </NextAuthProvider>
      </body>
    </html>
  )
}
