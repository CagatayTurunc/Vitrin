"use client";

import { useEffect, useState } from "react";
import { TrendingUp } from "lucide-react";
import type { ProductApiModel } from "@/core/domain/product.types";

interface Props {
  products: ProductApiModel[];
}

export function LiveDiscoveryTicker({ products }: Props) {
  const [mounted, setMounted] = useState(false);

  useEffect(() => {
    setMounted(true);
  }, []);

  if (!mounted || products.length === 0) {
    return null;
  }

  // Duplicate products for seamless loop
  const duplicatedProducts = [...products, ...products];

  return (
    <section className="py-8 bg-background border-y border-border/50 relative">
      <div className="mx-auto max-w-6xl px-4">
        {/* Header */}
        <div className="mb-6 space-y-1">
          <h2 className="text-sm font-semibold tracking-wider uppercase text-primary">
            CANLI KEŞİF AKIŞI
          </h2>
          <p className="text-sm text-muted-foreground">
            Topluluğun anlık ilgisi
          </p>
        </div>

        {/* Ticker */}
        <div className="relative overflow-hidden">
          <div className="marquee-content space-x-4">
            {duplicatedProducts.map((product, index) => (
              <div
                key={`${product.id}-${index}`}
                className="flex items-center gap-2 bg-card border border-border rounded-full px-4 py-2 whitespace-nowrap shadow-sm hover:shadow-md transition-shadow card-hover flex-shrink-0"
              >
                {/* Live dot */}
                <div className="w-2 h-2 rounded-full bg-primary animate-glow" />
                
                {/* Product name */}
                <span className="font-medium text-foreground text-sm">
                  {product.name}
                </span>
                
                {/* Trending icon and votes */}
                <div className="flex items-center gap-1 text-primary">
                  <TrendingUp className="h-3 w-3" />
                  <span className="text-sm font-semibold">
                    {product.upvotes ?? 0}
                  </span>
                </div>
              </div>
            ))}
          </div>
          
          {/* Fade edges */}
          <div className="absolute left-0 top-0 bottom-0 w-8 bg-gradient-to-r from-background to-transparent pointer-events-none z-10" />
          <div className="absolute right-0 top-0 bottom-0 w-8 bg-gradient-to-l from-background to-transparent pointer-events-none z-10" />
        </div>
      </div>
    </section>
  );
}
