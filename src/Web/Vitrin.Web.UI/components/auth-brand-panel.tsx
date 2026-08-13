"use client";

import { Sparkles, TrendingUp, Users, ArrowUp, Star, Zap } from "lucide-react";
import React, { useEffect, useState } from "react";

const floatingElements = [
  { icon: TrendingUp, name: "FinTrack", description: "Kişisel finans takibi", votes: 94, delay: "0s", position: { top: "15%", left: "8%" } },
  { icon: Sparkles, name: "NotAI", description: "Yapay zeka not de...", votes: 128, delay: "3s", position: { top: "35%", right: "12%" } },
  { icon: Zap, name: "QuickTask", description: "Hızlı görev yöneticisi", votes: 87, delay: "6s", position: { top: "60%", left: "15%" } }
];

export function AuthBrandPanel() {
  const [mounted, setMounted] = useState(false);

  useEffect(() => {
    setMounted(true);
  }, []);

  return (
    <div className="relative hidden h-full flex-col bg-[#007A52] p-10 text-white lg:flex overflow-hidden">
      {/* Enhanced animated background - orijinal sitedeki gibi */}
      <div className="absolute inset-0 opacity-20">
        {/* Animated dots pattern */}
        <div className="absolute inset-0 bg-[radial-gradient(#ffffff_1px,transparent_1px)] [background-size:24px_24px] animate-pulse" 
             style={{ animationDuration: '4s' }} />
        
        {/* Moving geometric shapes */}
        <div className="absolute top-16 left-16 w-32 h-32 bg-white/10 rounded-full blur-2xl animate-bounce" 
             style={{ animationDelay: '0s', animationDuration: '8s' }} />
        <div className="absolute top-32 right-24 w-24 h-24 bg-white/15 rounded-full blur-xl animate-pulse" 
             style={{ animationDelay: '2s', animationDuration: '6s' }} />
        <div className="absolute bottom-32 left-24 w-28 h-28 bg-white/8 rounded-full blur-2xl animate-bounce" 
             style={{ animationDelay: '4s', animationDuration: '10s' }} />
        <div className="absolute bottom-16 right-16 w-20 h-20 bg-white/12 rounded-full blur-xl animate-pulse" 
             style={{ animationDelay: '6s', animationDuration: '5s' }} />
        
        {/* Floating particles */}
        <div className="absolute top-1/4 left-1/3 w-2 h-2 bg-white/40 rounded-full animate-ping" 
             style={{ animationDelay: '1s' }} />
        <div className="absolute top-2/3 right-1/4 w-1.5 h-1.5 bg-white/30 rounded-full animate-ping" 
             style={{ animationDelay: '3s' }} />
        <div className="absolute top-1/2 left-1/4 w-1 h-1 bg-white/50 rounded-full animate-ping" 
             style={{ animationDelay: '5s' }} />
        
        {/* Subtle lines animation */}
        <div className="absolute top-0 left-0 w-full h-full">
          <div className="absolute top-1/3 left-0 w-full h-px bg-gradient-to-r from-transparent via-white/10 to-transparent animate-pulse" 
               style={{ animationDelay: '0s', animationDuration: '8s' }} />
          <div className="absolute top-2/3 left-0 w-full h-px bg-gradient-to-r from-transparent via-white/8 to-transparent animate-pulse" 
               style={{ animationDelay: '4s', animationDuration: '10s' }} />
        </div>
        
        {/* Rotating subtle gradient overlay */}
        <div className="absolute inset-0 bg-gradient-to-br from-white/5 via-transparent to-white/3 animate-pulse" 
             style={{ animationDuration: '12s' }} />
      </div>

      {/* Brand header */}
      <div className={`relative z-20 flex items-center gap-3 transition-all duration-1000 ${mounted ? 'opacity-100 translate-y-0' : 'opacity-0 -translate-y-10'}`}>
        <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-white/10 backdrop-blur-md border border-white/20 shadow-lg">
          <Sparkles className="h-6 w-6" />
        </div>
        <div className="text-2xl font-bold tracking-tight">Vitrin</div>
      </div>

      {/* Floating cards with better positioning */}
      <div className="relative z-10 flex-1 flex items-center justify-center">
        <div className="relative w-full max-w-lg h-80">
          {floatingElements.map((element, index) => {
            const IconComponent = element.icon;
            return (
              <div
                key={element.name}
                className={`absolute w-64 rounded-2xl bg-white/10 backdrop-blur-md border border-white/20 p-4 shadow-xl transition-all duration-1000 hover:scale-105 hover:bg-white/15 cursor-pointer ${
                  mounted ? 'opacity-100 translate-y-0' : 'opacity-0 translate-y-10'
                }`}
                style={{
                  ...element.position,
                  animationDelay: element.delay,
                  animation: mounted ? `float 6s ease-in-out infinite ${element.delay}` : 'none'
                }}
              >
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-3">
                    <div className="flex h-10 w-10 items-center justify-center rounded-full bg-white/20 shadow-lg">
                      <IconComponent className="h-5 w-5" />
                    </div>
                    <div>
                      <div className="font-semibold text-sm">{element.name}</div>
                      <div className="text-xs text-white/70">{element.description}</div>
                    </div>
                  </div>
                  <div className="flex flex-col items-center justify-center rounded-xl bg-white/20 px-3 py-2 text-xs font-bold border border-white/20">
                    <ArrowUp className="mb-1 h-3 w-3" />
                    {element.votes}
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      </div>

      {/* Content section */}
      <div className={`relative z-20 max-w-md transition-all duration-1000 delay-500 ${mounted ? 'opacity-100 translate-y-0' : 'opacity-0 translate-y-10'}`}>
        <h1 className="text-5xl font-bold tracking-tight mb-6 leading-tight">
          Büyümenin <br />
          <span className="text-white/80">başladığı yer.</span>
        </h1>
        <p className="text-lg text-white/80 mb-8 leading-relaxed">
          En yeni ürünleri keşfet, favorilerine oy ver ve kendi projelerini
          doğru kitleyle buluştur.
        </p>

        {/* Enhanced stats */}
        <div className="grid grid-cols-3 gap-3 mb-8">
          {[
            { icon: Users, value: "12.4k+", label: "Topluluk" },
            { icon: TrendingUp, value: "3.2k+", label: "Ürün" },
            { icon: ArrowUp, value: "48k+", label: "Oy" }
          ].map((stat, index) => {
            const IconComponent = stat.icon;
            return (
              <div
                key={stat.label}
                className={`rounded-2xl bg-white/10 backdrop-blur-md border border-white/20 p-4 text-center transition-all duration-700 hover:scale-105 hover:bg-white/15 ${
                  mounted ? 'opacity-100 translate-y-0' : 'opacity-0 translate-y-5'
                }`}
                style={{ transitionDelay: `${800 + index * 100}ms` }}
              >
                <IconComponent className="h-5 w-5 mx-auto mb-2 text-white/80" />
                <div className="font-bold text-lg">{stat.value}</div>
                <div className="text-xs text-white/60 uppercase font-semibold tracking-wide">{stat.label}</div>
              </div>
            );
          })}
        </div>

        {/* Enhanced testimonial */}
        <div className={`rounded-2xl bg-white/10 backdrop-blur-md border border-white/20 p-6 transition-all duration-700 hover:bg-white/15 ${mounted ? 'opacity-100 scale-100' : 'opacity-0 scale-95'}`} style={{ transitionDelay: '1100ms' }}>
          <div className="flex gap-1 mb-4">
            {[1, 2, 3, 4, 5].map((star) => (
              <Star key={star} className="h-4 w-4 fill-yellow-400 text-yellow-400" />
            ))}
          </div>
          <p className="text-sm text-white/90 mb-4 leading-relaxed">
            "Vitrin sayesinde harika ürünler keşfettim ve kendi projelerimi doğru kitleye ulaştırma şansı buldum."
          </p>
          <div className="flex items-center gap-3">
            <div className="flex h-11 w-11 items-center justify-center rounded-full bg-white/20 font-bold text-sm border border-white/20">
              N
            </div>
            <div>
              <div className="text-sm font-bold">Nur Aksoy</div>
              <div className="text-xs text-white/70">Ürün Tasarımcısı</div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
