"use client";

import { Button } from "@/components/ui/button";
import { ArrowRight, Sparkles, Eye, Trophy, MessageCircle, DollarSign, GitCompareArrows } from "lucide-react";
import Link from "next/link";
import { useEffect, useState } from "react";

function istanbulDateLabel() {
  return new Intl.DateTimeFormat("tr-TR", {
    day: "numeric",
    month: "long",
    year: "numeric",
    timeZone: "Europe/Istanbul",
  })
    .format(new Date())
    .toUpperCase();
}

export function PremiumHero() {
  const [mounted, setMounted] = useState(false);
  const [dateLabel, setDateLabel] = useState("");

  useEffect(() => {
    setMounted(true);
    setDateLabel(istanbulDateLabel());
  }, []);

  return (
    <section className="relative overflow-hidden px-4 py-12 sm:py-16 lg:py-20">
      {/* Hero glow effect */}
      <div className="hero-glow dark:hero-glow hero-glow-light" />
      
      {/* Content */}
      <div className="mx-auto max-w-6xl">
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-8 lg:gap-12 items-center">
          {/* Left side - Main content */}
          <div className="space-y-6 relative z-10">
            {/* Eyebrow */}
            <div 
              className={`flex items-center gap-2 text-sm font-medium text-primary ${
                mounted ? "animate-fade-in-down" : "opacity-0"
              }`}
            >
              <div className="w-2 h-2 rounded-full bg-primary animate-glow" />
              <span>{dateLabel ? `${dateLabel} — BUGÜNÜN SEÇKİSİ` : "BUGÜNÜN SEÇKİSİ"}</span>
            </div>

            {/* Main headline */}
            <div className="space-y-4">
              <h1 
                className={`text-4xl sm:text-5xl lg:text-6xl font-extrabold tracking-tight text-foreground leading-tight ${
                  mounted ? "animate-fade-in-up animate-stagger-1" : "opacity-0"
                }`}
              >
                Yeni fikirlere{" "}
                <span className="text-primary">vitrin</span> aç.
              </h1>
              
              <p 
                className={`text-lg sm:text-xl text-muted-foreground leading-relaxed max-w-lg ${
                  mounted ? "animate-fade-in-up animate-stagger-2" : "opacity-0"
                }`}
              >
                Türkiye'nin en heyecan verici ürünlerini keşfet, destekle ve topluluğun bir sonraki favorisini birlikte öne çıkar.
              </p>
            </div>

            {/* Action buttons */}
            <div 
              className={`flex flex-col sm:flex-row gap-4 ${
                mounted ? "animate-fade-in-up animate-stagger-3" : "opacity-0"
              }`}
            >
              <Button 
                asChild 
                size="lg" 
                className="group bg-primary text-primary-foreground hover:bg-primary/90 transition-all duration-300"
              >
                <Link href="#kesfet" className="flex items-center gap-2">
                  Ürünleri keşfet
                  <ArrowRight className="h-4 w-4 group-hover:translate-x-1 transition-transform" />
                </Link>
              </Button>
              
              <Button 
                asChild 
                variant="outline" 
                size="lg"
                className="group border-border hover:bg-muted transition-all duration-300"
              >
                <Link href="/submit" className="flex items-center gap-2">
                  Ürününü ekle
                  <Sparkles className="h-4 w-4 group-hover:rotate-12 transition-transform" />
                </Link>
              </Button>
            </div>
          </div>

          {/* Right side - Ürün → Para Çevirme Makinesi Animasyonu */}
          <div 
            className={`relative z-10 hidden sm:flex items-center justify-center ${
              mounted ? "animate-slide-in-right animate-stagger-4" : "opacity-0"
            }`}
          >
            <div className="relative w-72 h-72 sm:w-80 sm:h-80">

              {/* Arka plan parıltısı */}
              <div className="absolute inset-0 bg-gradient-radial from-primary/10 to-transparent rounded-full animate-pulse" />

              {/* Yukarıdan düşen ürün 1 - Mavi / Eye */}
              <div className="absolute top-0 left-[30%]">
                <div className="animate-product-drop-1">
                  <div className="w-8 h-8 bg-gradient-to-br from-blue-400 to-blue-600 rounded-lg shadow-lg flex items-center justify-center">
                    <Eye className="w-4 h-4 text-white" />
                  </div>
                </div>
              </div>

              {/* Yukarıdan düşen ürün 2 - Yeşil / Trophy */}
              <div className="absolute -top-4 left-[45%]">
                <div className="animate-product-drop-2">
                  <div className="w-8 h-8 bg-gradient-to-br from-green-400 to-green-600 rounded-lg shadow-lg flex items-center justify-center">
                    <Trophy className="w-4 h-4 text-white" />
                  </div>
                </div>
              </div>

              {/* Yukarıdan düşen ürün 3 - Mor / MessageCircle */}
              <div className="absolute -top-8 left-[38%]">
                <div className="animate-product-drop-3">
                  <div className="w-8 h-8 bg-gradient-to-br from-purple-400 to-purple-600 rounded-lg shadow-lg flex items-center justify-center">
                    <MessageCircle className="w-4 h-4 text-white" />
                  </div>
                </div>
              </div>

              {/* Çevirici Makine - merkez */}
              <div className="animate-float-slow absolute top-[38%] left-[28%]">
                <div className="relative">
                  {/* Giriş hunisi */}
                  <div className="absolute -top-3 left-1/2 -translate-x-1/2 w-8 h-4 bg-gradient-to-b from-gray-300 to-gray-400 dark:from-gray-500 dark:to-gray-600 rounded-t-lg" />

                  {/* Makine gövdesi */}
                  <div className="w-28 h-16 bg-gradient-to-br from-gray-100 to-gray-200 dark:from-gray-700 dark:to-gray-800 rounded-2xl shadow-2xl border-2 border-gray-300 dark:border-gray-600 overflow-hidden">
                    {/* İç mekanizma */}
                    <div className="absolute inset-2 bg-gradient-to-r from-blue-50 to-purple-50 dark:from-blue-900/30 dark:to-purple-900/30 rounded-xl flex items-center justify-around px-2">
                      <div className="w-4 h-4 bg-gray-300 dark:bg-gray-500 rounded-full animate-spin-slow" />
                      <div className="flex-1 mx-1 h-0.5 bg-gradient-to-r from-blue-400 to-purple-400 animate-pulse" />
                      <div className="w-4 h-4 bg-gray-300 dark:bg-gray-500 rounded-full animate-spin-slow" style={{ animationDirection: 'reverse' }} />
                    </div>
                  </div>

                  {/* Çıkış hunisi - altın sarısı */}
                  <div className="absolute -bottom-3 left-1/2 -translate-x-1/2 w-10 h-5 bg-gradient-to-b from-yellow-400 to-yellow-600 rounded-b-xl shadow-lg" />
                </div>
              </div>

              {/* Çıkan para 1 */}
              <div className="absolute top-[60%] left-[35%]">
                <div className="animate-coin-output-1">
                  <div className="w-5 h-5 bg-gradient-to-br from-yellow-300 to-yellow-500 rounded-full shadow-lg flex items-center justify-center ring-1 ring-yellow-400">
                    <DollarSign className="w-2.5 h-2.5 text-yellow-900" />
                  </div>
                </div>
              </div>

              {/* Çıkan para 2 */}
              <div className="absolute top-[62%] left-[40%]">
                <div className="animate-coin-output-2">
                  <div className="w-5 h-5 bg-gradient-to-br from-yellow-300 to-yellow-500 rounded-full shadow-lg flex items-center justify-center ring-1 ring-yellow-400">
                    <DollarSign className="w-2.5 h-2.5 text-yellow-900" />
                  </div>
                </div>
              </div>

              {/* Çıkan para 3 */}
              <div className="absolute top-[61%] left-[45%]">
                <div className="animate-coin-output-3">
                  <div className="w-5 h-5 bg-gradient-to-br from-yellow-300 to-yellow-500 rounded-full shadow-lg flex items-center justify-center ring-1 ring-yellow-400">
                    <DollarSign className="w-2.5 h-2.5 text-yellow-900" />
                  </div>
                </div>
              </div>

              {/* Sihirli ışık çizgisi */}
              <div className="absolute top-[46%] left-[22%] w-14 h-0.5 bg-gradient-to-r from-transparent via-blue-400 to-transparent animate-magic-beam" />
              <div className="absolute top-[52%] left-[24%] w-10 h-0.5 bg-gradient-to-r from-transparent via-purple-400 to-transparent animate-magic-beam" style={{ animationDelay: '0.5s' }} />

              {/* Yüzen istatistik kartı */}
              <div className="animate-float absolute top-6 right-4">
                <div className="w-24 h-28 bg-white/85 dark:bg-card/85 backdrop-blur-sm rounded-xl border border-white/50 dark:border-border shadow-xl rotate-6">
                  <div className="p-3 space-y-2">
                    <div className="w-3 h-3 bg-emerald-500 rounded-full animate-pulse" />
                    <div className="space-y-1">
                      <div className="h-1.5 bg-gray-200 dark:bg-gray-600 rounded animate-pulse" />
                      <div className="h-1.5 bg-gray-100 dark:bg-gray-700 rounded w-3/4 animate-pulse" style={{ animationDelay: '150ms' }} />
                    </div>
                    <div className="flex justify-between items-center pt-1">
                      <Trophy className="w-3 h-3 text-yellow-500" />
                      <span className="text-xs font-bold text-primary">+48</span>
                    </div>
                  </div>
                </div>
              </div>

              {/* VS kartı */}
              <div className="animate-float-delayed absolute bottom-8 left-2">
                <div className="w-14 h-16 bg-white/75 dark:bg-card/75 backdrop-blur-sm rounded-lg border border-white/40 dark:border-border shadow-md -rotate-6">
                  <div className="p-2 text-center">
                    <GitCompareArrows className="w-4 h-4 mx-auto text-primary mb-1" />
                    <div className="text-[10px] font-bold text-muted-foreground">VS</div>
                  </div>
                </div>
              </div>

              {/* Parçacıklar */}
              <div className="absolute top-6 left-12 w-2 h-2 bg-primary/40 rounded-full animate-ping" />
              <div className="absolute top-24 right-6 w-1.5 h-1.5 bg-purple-500/50 rounded-full animate-bounce" style={{ animationDelay: '500ms' }} />
              <div className="absolute bottom-10 left-16 w-1 h-1 bg-yellow-500/60 rounded-full animate-pulse" style={{ animationDelay: '700ms' }} />
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}