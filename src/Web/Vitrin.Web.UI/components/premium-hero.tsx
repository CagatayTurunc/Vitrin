"use client";

import { Button } from "@/components/ui/button";
import { ArrowRight, Sparkles } from "lucide-react";
import Link from "next/link";
import { useEffect, useState } from "react";

export function PremiumHero() {
  const [mounted, setMounted] = useState(false);

  useEffect(() => {
    setMounted(true);
  }, []);

  return (
    <section className="relative overflow-hidden px-4 py-16 sm:py-20">
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
              <span>13 AĞUSTOS 2026 — BUGÜNÜN SEÇKİSİ</span>
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

          {/* Right side - Signal card */}
          <div 
            className={`relative z-10 ${
              mounted ? "animate-slide-in-right animate-stagger-4" : "opacity-0"
            }`}
          >
            <div className="bg-card border border-border rounded-xl p-6 shadow-sm card-hover">
              <div className="space-y-4">
                <div className="flex items-center justify-between">
                  <h3 className="font-semibold text-sm tracking-wider uppercase text-primary">
                    VİTRİN SİNYALİ
                  </h3>
                  <div className="w-2 h-2 rounded-full bg-primary animate-glow" />
                </div>
                
                <div className="space-y-3">
                  <h4 className="text-xl font-bold text-foreground">
                    Topluluğun keşif nabzı burada atıyor.
                  </h4>
                  
                  <div className="flex items-center justify-between pt-2">
                    <div>
                      <p className="text-2xl font-bold text-foreground">48</p>
                      <p className="text-sm text-muted-foreground">yeni ürün</p>
                    </div>
                    <div className="text-right">
                      <p className="text-sm text-muted-foreground">bu hafta</p>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}