"use client";

import { TrendingUp } from "lucide-react";
import { useEffect, useState } from "react";

interface TrendingProduct {
  id: string;
  name: string;
  votes: number;
  position: number;
}

// Sample data - replace with real API data
const trendingProducts: TrendingProduct[] = [
  { id: "1", name: "FigmaFlow", votes: 248, position: 1 },
  { id: "2", name: "Briefly", votes: 192, position: 2 },
  { id: "3", name: "Railnote", votes: 181, position: 3 },
];

export function TrendingSection() {
  const [mounted, setMounted] = useState(false);

  useEffect(() => {
    setMounted(true);
  }, []);

  return (
    <section className="py-16">
      <div className="mx-auto max-w-6xl px-4">
        {/* Gradient background */}
        <div className="relative rounded-2xl bg-gradient-to-br from-muted/30 via-muted/20 to-transparent border border-border/50 p-8 lg:p-12 overflow-hidden">
          {/* Background glow */}
          <div className="absolute top-0 left-0 w-96 h-96 bg-gradient-to-br from-primary/5 to-transparent rounded-full blur-3xl" />
          
          {/* Content */}
          <div className="relative z-10">
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-8 lg:gap-12 items-start">
              {/* Left side - Description */}
              <div className={`space-y-4 ${mounted ? "animate-fade-in-up" : "opacity-0"}`}>
                <h2 className="text-sm font-semibold tracking-wider uppercase text-primary">
                  YÜKSELENLER
                </h2>
                
                <h3 className="text-3xl font-bold text-foreground">
                  Şu anda konuşulan fikirler.
                </h3>
                
                <div className="space-y-3 text-muted-foreground">
                  <p>
                    Oylama, kaydetme ve yorum sinyallerinden beslenen seçki;
                  </p>
                  <p>
                    yeni ama gerçekten yankı uyandıran ürünleri öne çıkarır.
                  </p>
                </div>
              </div>

              {/* Right side - Ranking panel */}
              <div className={`${mounted ? "animate-slide-in-right animate-stagger-2" : "opacity-0"}`}>
                <div className="bg-card/50 backdrop-blur-sm border border-border rounded-xl p-6 space-y-4">
                  {trendingProducts.map((product, index) => (
                    <div
                      key={product.id}
                      className={`flex items-center justify-between p-3 rounded-lg bg-background/50 border border-border/50 card-hover ${
                        mounted ? `animate-fade-in-up animate-stagger-${index + 3}` : "opacity-0"
                      }`}
                    >
                      {/* Position and name */}
                      <div className="flex items-center gap-4">
                        <span className="text-lg font-mono font-bold text-muted-foreground w-8">
                          {product.position.toString().padStart(2, '0')}
                        </span>
                        <span className="font-semibold text-foreground">
                          {product.name}
                        </span>
                      </div>

                      {/* Votes */}
                      <div className="flex items-center gap-2 text-primary">
                        <TrendingUp className="h-4 w-4" />
                        <span className="font-bold">
                          +{product.votes}
                        </span>
                      </div>
                    </div>
                  ))}
                  
                  {/* Footer note */}
                  <p className="text-xs text-muted-foreground text-center pt-2">
                    Topluluğun anlık ilgisi
                  </p>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}