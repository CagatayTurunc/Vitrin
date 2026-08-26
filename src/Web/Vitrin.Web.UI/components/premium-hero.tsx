"use client";

import { Button } from "@/components/ui/button";
import { ArrowRight, Sparkles } from "lucide-react";
import Link from "next/link";
import { useEffect, useState } from "react";
import { HeroShowcase } from "@/components/hero-showcase";

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

          {/* Right side - Premium Interactive Showcase */}
          <div 
            className={`relative z-10 hidden sm:flex items-center justify-center ${
              mounted ? "animate-slide-in-right animate-stagger-4" : "opacity-0"
            }`}
          >
            <div className="relative w-full max-w-sm h-80 lg:h-96">
              <HeroShowcase />
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}