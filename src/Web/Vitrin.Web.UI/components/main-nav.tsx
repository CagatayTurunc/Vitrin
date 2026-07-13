'use client';

import Link from 'next/link';
import { ChevronDown, Rocket, BookOpen, MessageSquare, Trophy, Calendar, Mail, Newspaper, Star, TrendingUp, Layers, Zap } from 'lucide-react';
import { cn } from '@/lib/utils';

const navItems = [
  {
    title: 'En İyiler',
    href: '#',
    items: [
      {
        title: 'Günün En İyileri',
        description: 'Topluluğun bugün keşfettiği favori ürünler',
        icon: <Star className="h-4 w-4 text-amber-500" />,
        href: '#'
      },
      {
        title: 'Trend Olan Kategoriler',
        description: 'Yapay Zeka, SaaS, Üretkenlik ve daha fazlası',
        icon: <TrendingUp className="h-4 w-4 text-rose-500" />,
        href: '#'
      },
      {
        title: 'Yeni Eklenenler',
        description: 'Vitrin\'e yeni giriş yapan en taze araçlar',
        icon: <Zap className="h-4 w-4 text-yellow-400" />,
        href: '#'
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
    href: '#',
    items: [
      {
        title: 'Yaklaşan Lansmanlar',
        description: 'Topluluk tarafından en çok beklenenler',
        icon: <Rocket className="h-4 w-4 text-orange-500" />,
        href: '#'
      },
      {
        title: 'Lansman Rehberi',
        description: 'Başarılı bir lansman için ipuçları',
        icon: <BookOpen className="h-4 w-4 text-blue-500" />,
        href: '#'
      }
    ]
  },
  {
    title: 'Haberler',
    href: '#',
    items: [
      {
        title: 'Bülten',
        description: 'Vitrin\'in en iyileri, her gün mailinde',
        icon: <Mail className="h-4 w-4 text-emerald-500" />,
        href: '#'
      },
      {
        title: 'Hikayeler',
        description: 'Geliştiricilerden teknoloji haberleri ve ipuçları',
        icon: <Newspaper className="h-4 w-4 text-pink-500" />,
        href: '#'
      }
    ]
  },
  {
    title: 'Topluluk',
    href: '#',
    items: [
      {
        title: 'Tartışmalar',
        description: 'Soru sor, destek bul ve ağ kur',
        icon: <MessageSquare className="h-4 w-4 text-indigo-500" />,
        href: '#'
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
        href: '#'
      }
    ]
  }
];

export function MainNav() {
  return (
    <nav className="hidden lg:flex items-center space-x-1 pl-6">
      {navItems.map((navItem, index) => (
        <div key={index} className="relative group">
          <button className="flex items-center gap-1.5 whitespace-nowrap px-3 py-2 text-sm font-medium text-muted-foreground hover:text-foreground transition-colors rounded-md hover:bg-muted/50">
            {navItem.title}
            <ChevronDown className="h-3 w-3 opacity-50 group-hover:rotate-180 transition-transform duration-200" />
          </button>
          
          <div className="absolute left-0 top-full pt-2 opacity-0 translate-y-2 pointer-events-none group-hover:opacity-100 group-hover:translate-y-0 group-hover:pointer-events-auto transition-all duration-200 z-50">
            <div className="w-80 rounded-2xl border border-border bg-card/95 backdrop-blur-xl p-2 shadow-lg ring-1 ring-black/5 dark:ring-white/10">
              {navItem.items.map((subItem, subIndex) => (
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
      ))}
      
      <Link 
        href="#" 
        className="px-3 py-2 text-sm whitespace-nowrap font-medium text-muted-foreground hover:text-foreground transition-colors rounded-md hover:bg-muted/50"
      >
        Öne Çıkar
      </Link>
    </nav>
  );
}
