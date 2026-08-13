"use client";

import { ArrowRight, Sparkles } from "lucide-react";
import Link from "next/link";
import { useEffect, useState } from "react";

interface FeaturedProduct {
  id: string;
  name: string;
  description: string;
  category: string;
  number: string;
  slug: string;
}

// Sample data - replace with real API data
const featuredProducts: FeaturedProduct[] = [
  {
    id: "1",
    name: "Atlas",
    description: "Ekiplerin fikirden teslimata kadar tüm başlama tek yerde tutun çalışma alanı.",
    category: "Yapay Zekâ",
    number: "01",
    slug: "atlas"
  },
  {
    id: "2", 
    name: "Pixelmind",
    description: "Yaratıcı ekipler için ürettiken yapay zeka ile görsel prototipleme aracı.",
    category: "Yapay Zekâ",
    number: "01",
    slug: "pixelmind"
  },
  {
    id: "3",
    name: "Novaform",
    description: "Müşteri geri bildirimini karar verme ürün ekipleri için net toplulara dönüştürür.",
    category: "Yapay Zekâ", 
    number: "01",
    slug: "novaform"
  }
];

export function FeaturedProducts() {
  const [mounted, setMounted] = useState(false);

  useEffect(() => {
    setMounted(true);
  }, []);

  return (
    <section id="kesfet" className="py-16 bg-background">
      <div className="mx-auto max-w-6xl px-4">
        {/* Header */}
        <div className="mb-12 flex items-end justify-between">
          <div className="space-y-2">
            <h2 className="text-sm font-semibold tracking-wider uppercase text-primary">
              KÜRSÜDEKİLER
            </h2>
            <h3 className="text-3xl font-bold text-foreground">
              Bugün keşfedilenler
            </h3>
          </div>
          
          <Link 
            href="/discover" 
            className="text-sm font-medium text-primary hover:text-primary/80 transition-colors flex items-center gap-1 group"
          >
            Tüm ürünleri gör
            <ArrowRight className="h-4 w-4 group-hover:translate-x-1 transition-transform" />
          </Link>
        </div>

        {/* Product Grid */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {featuredProducts.map((product, index) => (
            <Link
              key={product.id}
              href={`/product/${product.slug}`}
              className={`group block ${
                mounted ? `animate-fade-in-up animate-stagger-${index + 1}` : "opacity-0"
              }`}
            >
              <div className="bg-card border border-border rounded-xl p-6 space-y-4 card-hover">
                {/* Product icon area */}
                <div className="flex items-center justify-between">
                  <div className="w-12 h-12 rounded-xl bg-primary/10 flex items-center justify-center">
                    <Sparkles className="h-6 w-6 text-primary" />
                  </div>
                  <span className="text-sm font-mono text-muted-foreground">
                    {product.number}
                  </span>
                </div>

                {/* Product info */}
                <div className="space-y-3">
                  <h4 className="text-xl font-bold text-foreground group-hover:text-primary transition-colors">
                    {product.name}
                  </h4>
                  
                  <p className="text-sm text-muted-foreground line-clamp-3 leading-relaxed">
                    {product.description}
                  </p>
                  
                  <div className="flex items-center justify-between pt-2">
                    <span className="inline-block bg-primary/10 text-primary text-xs font-medium px-3 py-1 rounded-full">
                      {product.category}
                    </span>
                    
                    <div className="flex items-center gap-1 text-sm font-medium text-primary group-hover:translate-x-1 transition-transform">
                      İncele
                      <ArrowRight className="h-3 w-3" />
                    </div>
                  </div>
                </div>
              </div>
            </Link>
          ))}
        </div>
      </div>
    </section>
  );
}