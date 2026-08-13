'use client';

import Link from 'next/link';
import { ChevronDown, Rocket, BookOpen, MessageSquare, Trophy, Calendar, Mail, Newspaper, Star, TrendingUp, Layers, Zap, ShieldCheck } from 'lucide-react';

const navItems = [
  {
    title: 'En İyiler',
    href: '/launches',
    items: [
      {
        title: 'Günün En İyileri',
        description: 'Topluluğun bugün keşfettiği favori ürünler',
        icon: <Star className="h-4 w-4 text-amber-500" />,
        href: '/launches'
      },
      {
        title: 'Trend Olan Kategoriler',
        description: 'Yapay Zeka, SaaS, Üretkenlik ve daha fazlası',
        icon: <TrendingUp className="h-4 w-4 text-rose-500" />,
        href: '/categories'
      },
      {
        title: 'Yeni Eklenenler',
        description: 'Vitrin\'e yeni giriş yapan en taze araçlar',
        icon: <Zap className="h-4 w-4 text-yellow-400" />,
        href: '/discover?sort=newest'
      },
      {
        title: 'Koleksiyonlar',
        description: 'Farklı ihtiyaçlara göre derlenmiş ürün listeleri',
        icon: <Layers className="h-4 w-4 text-cyan-500" />,
        href: '/collections'
      }
    ]
  },
  {
    title: 'Lansmanlar',
    href: '/launches',
    items: [
      {
        title: 'Yaklaşan Lansmanlar',
        description: 'Topluluk tarafından en çok beklenenler',
        icon: <Rocket className="h-4 w-4 text-orange-500" />,
        href: '/launches/upcoming'
      },
      {
        title: 'Günün Lansman Sıralaması',
        description: 'İstanbul saati ile bugün yayınlanan ürünler',
        icon: <Calendar className="h-4 w-4 text-blue-500" />,
        href: '/launches/today'
      },
      {
        title: 'Sıralama Nasıl Çalışır?',
        description: 'Puan, dönem, eşitlik ve adil oy kuralları',
        icon: <BookOpen className="h-4 w-4 text-indigo-500" />,
        href: '/ranking'
      }
    ]
  },
  {
    title: 'Haberler',
    href: '/blog',
    items: [
      {
        title: 'Bülten',
        description: 'Vitrin\'in en iyileri, her gün mailinde',
        icon: <Mail className="h-4 w-4 text-emerald-500" />,
        href: '/newsletter'
      },
      {
        title: 'Hikayeler',
        description: 'Geliştiricilerden teknoloji haberleri ve ipuçları',
        icon: <Newspaper className="h-4 w-4 text-pink-500" />,
        href: '/blog'
      }
    ]
  },
  {
    title: 'Forumlar',
    href: '/activity',
    items: [
      {
        title: 'Topluluk Akışı',
        description: 'Lansmanları, yorumları ve yeni bağlantıları canlı izle',
        icon: <MessageSquare className="h-4 w-4 text-indigo-500" />,
        href: '/activity'
      },
      {
        title: 'Liderlik Tablosu',
        description: 'En aktif topluluk üyeleri',
        icon: <Trophy className="h-4 w-4 text-yellow-500" />,
        href: '/leaderboard'
      },
      {
        title: 'Etkinlikler',
        description: 'Online ve fiziksel buluşmalar',
        icon: <Calendar className="h-4 w-4 text-teal-500" />,
        href: '/events'
      },
      {
        title: 'Topluluk Kuralları',
        description: 'Katılım, içerik, oy ve yaptırım ilkeleri',
        icon: <ShieldCheck className="h-4 w-4 text-emerald-500" />,
        href: '/community-rules'
      }
    ]
  },
  {
    title: 'Reklamver',
    href: '/advertise',
    simple: true
  }
];

const handleSmoothScroll = (elementId: string) => {
  const element = document.getElementById(elementId);
  if (element) {
    element.scrollIntoView({ 
      behavior: 'smooth',
      block: 'start'
    });
  }
};

export function MainNav() {
  return (
    <nav className="hidden lg:flex items-center space-x-0 pl-1 shrink-0">
      {/* Navigation items */}
      {navItems.map((navItem, index) => (
        navItem.simple ? (
          // Simple link without dropdown
          <Link
            key={index}
            href={navItem.href}
            className="whitespace-nowrap px-1.5 py-2 text-sm font-medium text-muted-foreground hover:text-foreground transition-colors rounded-md hover:bg-muted/50"
          >
            {navItem.title}
          </Link>
        ) : (
          // Dropdown menu item
          <div key={index} className="relative group">
            <Link href={navItem.href} className="flex items-center gap-0.5 whitespace-nowrap px-1.5 py-2 text-sm font-medium text-muted-foreground hover:text-foreground transition-colors rounded-md hover:bg-muted/50">
              {navItem.title}
              <ChevronDown className="h-3 w-3 opacity-50 group-hover:rotate-180 transition-transform duration-200" />
            </Link>
            
            <div className="absolute left-0 top-full pt-2 opacity-0 translate-y-2 pointer-events-none group-hover:opacity-100 group-hover:translate-y-0 group-hover:pointer-events-auto transition-all duration-200 z-50">
              <div className="w-80 rounded-2xl border border-border bg-card/95 backdrop-blur-xl p-2 shadow-lg ring-1 ring-black/5 dark:ring-white/10">
                {navItem.items?.map((subItem, subIndex) => (
                  <Link
                    key={subIndex}
                    href={subItem.href}
                    className="flex items-start gap-3 rounded-xl p-3 hover:bg-muted transition-colors"
                  >
                    <div className="mt-0.5 flex h-8 w-8 shrink-0 items-center justify-center rounded-lg bg-background shadow-sm border border-border/50">
                      {subItem.icon}
                    </div>
                    <div className="flex flex-col gap-1">
                      <span className="text-sm font-semibold text-foreground">
                        {subItem.title}
                      </span>
                      <span className="text-xs text-muted-foreground leading-snug">
                        {subItem.description}
                      </span>
                    </div>
                  </Link>
                ))}
              </div>
            </div>
          </div>
        )
      ))}
    </nav>
  );
}
