"use client";

import { useEffect, useState, Suspense } from "react";
import { useSearchParams, useRouter } from "next/navigation";
import { ProductRow } from "@/components/product-row";
import { Product } from "@/core/domain/product.types";
import { Search, Loader2 } from "lucide-react";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";

function SearchContent() {
  const searchParams = useSearchParams();
  const router = useRouter();
  const initialQuery = searchParams.get("q") || "";
  
  const [query, setQuery] = useState(initialQuery);
  const [products, setProducts] = useState<Product[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [hasSearched, setHasSearched] = useState(false);

  useEffect(() => {
    if (initialQuery) {
      setQuery(initialQuery);
      performSearch(initialQuery);
    } else {
      setProducts([]);
      setHasSearched(false);
    }
  }, [initialQuery]);

  const performSearch = async (searchQuery: string) => {
    if (!searchQuery.trim()) return;
    
    setIsLoading(true);
    setHasSearched(true);
    try {
      const res = await fetch(process.env.NEXT_PUBLIC_API_URL + `/api/products/search?q=${encodeURIComponent(searchQuery)}`);
      if (res.ok) {
        const data = await res.json();
        const mappedProducts: Product[] = data.map((p: any, index: number) => ({
          id: p.id,
          rank: index + 1,
          name: p.name,
          slug: p.slug,
          description: p.tagline || p.description,
          publishedAt: p.publishedAt,
          image: p.thumbnailUrl || '/products/notai.png',
          topics: p.topics || [],
          votes: p.upvotes || 0,
        }));
        setProducts(mappedProducts);
      } else {
        setProducts([]);
      }
    } catch (error) {
      console.error("Arama hatası:", error);
      setProducts([]);
    } finally {
      setIsLoading(false);
    }
  };

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (query.trim()) {
      router.push(`/search?q=${encodeURIComponent(query.trim())}`);
    } else {
      router.push(`/search`);
    }
  };

  return (
    <main className="mx-auto w-full max-w-4xl px-4 py-8 sm:px-6 sm:py-12 min-h-screen">
      <div className="mb-8 flex flex-col gap-4">
        <h1 className="text-3xl font-extrabold tracking-tight text-foreground">
          Arama
        </h1>
        
        {/* Mobile Search Form (Also useful on desktop if you want to tweak search) */}
        <form onSubmit={handleSearchSubmit} className="relative flex items-center gap-2 max-w-xl">
          <div className="relative flex-1">
            <Search
              className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground"
              aria-hidden="true"
            />
            <Input
              type="search"
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              placeholder="Ürün, kategori veya özellik ara..."
              className="h-12 rounded-full border-border bg-card pl-10 text-base shadow-sm focus-visible:ring-primary"
            />
          </div>
          <Button type="submit" className="h-12 rounded-full px-6 shadow-sm shadow-primary/30">
            Bul
          </Button>
        </form>
      </div>

      <div className="mt-8">
        {isLoading ? (
          <div className="flex flex-col items-center justify-center py-20 text-muted-foreground">
            <Loader2 className="h-8 w-8 animate-spin mb-4 text-primary" />
            <p>Sonuçlar aranıyor...</p>
          </div>
        ) : hasSearched ? (
          products.length > 0 ? (
            <div className="flex flex-col space-y-4">
              <h2 className="text-lg font-medium text-muted-foreground mb-2">
                "{initialQuery}" için {products.length} sonuç bulundu
              </h2>
              <div className="rounded-3xl border border-border bg-card p-2 shadow-sm sm:p-3 flex flex-col divide-y divide-border/60">
                {products.map((product) => (
                  <ProductRow key={product.id} product={product} />
                ))}
              </div>
            </div>
          ) : (
            <div className="flex flex-col items-center justify-center py-20 text-center text-muted-foreground">
              <Search className="h-12 w-12 mb-4 opacity-20" />
              <h3 className="text-xl font-semibold text-foreground mb-2">Sonuç Bulunamadı</h3>
              <p>"{initialQuery}" ile eşleşen bir ürün göremedik. Farklı kelimeler denemeye ne dersin?</p>
            </div>
          )
        ) : (
          <div className="flex flex-col items-center justify-center py-20 text-center text-muted-foreground">
            <Search className="h-12 w-12 mb-4 opacity-20" />
            <p>Aramaya başlamak için yukarıya bir kelime yazın.</p>
          </div>
        )}
      </div>
    </main>
  );
}

export default function SearchPage() {
  return (
    <Suspense fallback={<div className="flex min-h-screen items-center justify-center"><Loader2 className="h-6 w-6 animate-spin text-primary" /></div>}>
      <SearchContent />
    </Suspense>
  );
}
