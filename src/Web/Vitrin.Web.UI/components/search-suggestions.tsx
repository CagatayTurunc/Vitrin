"use client";

import { Search, Crown, TrendingUp, Sparkles, Users } from 'lucide-react';
import Link from 'next/link';
import { useState } from 'react';

const suggestions = [
  {
    icon: <Crown className="h-4 w-4 text-amber-500" />,
    title: "Günün En İyileri",
    description: "Topluluğun bugün keşfettiği 'favori' ürünler",
    href: "/launches"
  },
  {
    icon: <TrendingUp className="h-4 w-4 text-rose-500" />,
    title: "Trend Olan Kategoriler",
    description: "Yapay Zeka, SaaS, Üretkenlik ve daha fazlası",
    href: "/categories"
  },
  {
    icon: <Sparkles className="h-4 w-4 text-yellow-400" />,
    title: "Yeni Eklenenler",
    description: "Vitrin'e yeni giriş yapan en taze araçlar",
    href: "/discover?sort=newest"
  },
  {
    icon: <Users className="h-4 w-4 text-cyan-500" />,
    title: "Koleksiyonlar",
    description: "Farklı ihtiyaçlara göre derlenmiş ürün listeleri",
    href: "/collections"
  }
];

export function SearchSuggestions({ onClose }: { onClose?: () => void }) {
  return (
    <div className="absolute top-full left-0 right-0 mt-3 bg-background/95 backdrop-blur-md border border-border/50 rounded-2xl shadow-2xl shadow-primary/10 z-[100] overflow-hidden ring-1 ring-primary/20">
      <div className="p-4 border-b border-border/50 bg-muted/20">
        <div className="flex items-center gap-2 text-sm font-medium text-muted-foreground">
          <Search className="h-4 w-4" />
          <span>Öneriler</span>
        </div>
      </div>
      
      <div className="max-h-80 overflow-y-auto bg-card/50 backdrop-blur-sm">
        {suggestions.map((suggestion, index) => (
          <Link
            key={index}
            href={suggestion.href}
            onClick={onClose}
            className="flex items-start gap-3 p-4 hover:bg-primary/5 hover:shadow-sm transition-all duration-200 border-b border-border/30 last:border-0 group"
          >
            <div className="mt-1 flex h-9 w-9 shrink-0 items-center justify-center rounded-xl bg-background/80 shadow-sm border border-border/50 group-hover:border-primary/30 group-hover:bg-primary/5 transition-all duration-200">
              {suggestion.icon}
            </div>
            <div className="flex flex-col gap-1.5 flex-1">
              <span className="text-sm font-semibold text-foreground group-hover:text-primary transition-colors duration-200">
                {suggestion.title}
              </span>
              <span className="text-xs text-muted-foreground leading-relaxed group-hover:text-muted-foreground/80">
                {suggestion.description}
              </span>
            </div>
          </Link>
        ))}
      </div>
    </div>
  );
}