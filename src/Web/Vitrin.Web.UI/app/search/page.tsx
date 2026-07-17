"use client";

import { useCallback, useEffect, useState, Suspense } from "react";
import { useSearchParams, useRouter } from "next/navigation";
import { ProductRow } from "@/components/product-row";
import { Product, ProductApiModel } from "@/core/domain/product.types";
import { Search, Loader2, ScanSearch, Sparkles } from "lucide-react";
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

  const performSearch = useCallback(async (searchQuery: string) => {
    if (!searchQuery.trim()) return;
    
    setIsLoading(true);
    setHasSearched(true);
    try {
      const res = await fetch(process.env.NEXT_PUBLIC_API_URL + `/api/products/search?q=${encodeURIComponent(searchQuery)}`);
      if (res.ok) {
        const data = await res.json() as ProductApiModel[];
        const mappedProducts: Product[] = data.map((p: ProductApiModel, index: number) => ({
          id: p.id,
          rank: index + 1,
          name: p.name,
          slug: p.slug,
          description: p.tagline || p.description,
          publishedAt: p.publishedAt,
          image: p.thumbnailUrl || '/products/notai.png',
          topics: p.topics || [],
          votes: p.upvotes || 0,
          views: p.viewCount || 0,
          comments: p.commentCount || 0,
          trendScore: p.trendScore || 0,
          searchScore: p.searchScore || 0,
          matchType: p.matchType,
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
  }, []);

  /* URL navigation is an external state source; keep the controlled input and results in sync. */
  /* eslint-disable react-hooks/set-state-in-effect */
  useEffect(() => {
    setQuery(initialQuery);
    if (initialQuery) {
      void performSearch(initialQuery);
    } else {
      setProducts([]);
      setHasSearched(false);
    }
  }, [initialQuery, performSearch]);
  /* eslint-enable react-hooks/set-state-in-effect */

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (query.trim()) {
      router.push(`/search?q=${encodeURIComponent(query.trim())}`);
    } else {
      router.push(`/search`);
    }
  };

  return (
    <main className="mx-auto min-h-screen w-full max-w-5xl px-4 py-8 sm:px-6 sm:py-12">
      <div className="relative mb-10 overflow-hidden rounded-[2rem] border border-primary/15 bg-gradient-to-br from-primary/10 via-card to-orange-500/10 p-6 shadow-sm sm:p-10">
        <div className="pointer-events-none absolute -right-16 -top-20 h-52 w-52 rounded-full bg-primary/15 blur-3xl" />
        <div className="relative flex flex-col gap-4">
          <div className="flex items-center gap-2 text-sm font-semibold text-primary">
            <Sparkles className="h-4 w-4" /> Akıllı keşif
          </div>
          <div>
            <h1 className="text-3xl font-extrabold tracking-tight text-foreground sm:text-4xl">Aradığını tarif et</h1>
            <p className="mt-2 max-w-2xl text-sm text-muted-foreground sm:text-base">
              Ürün adı, açıklama veya kategori içinde arar; eksik ve hatalı yazımlarda benzer sonuçları da bulur.
            </p>
          </div>
          <form onSubmit={handleSearchSubmit} className="relative mt-2 flex max-w-2xl items-center gap-2">
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
              className="h-14 rounded-2xl border-primary/20 bg-background/90 pl-11 text-base shadow-lg shadow-primary/5 focus-visible:ring-primary"
            />
          </div>
          <Button type="submit" className="h-14 rounded-2xl px-7 shadow-lg shadow-primary/20">
            <ScanSearch className="mr-2 h-4 w-4" /> Bul
          </Button>
        </form>
          <div className="flex flex-wrap gap-2 text-xs text-muted-foreground">
            <span className="rounded-full border bg-background/60 px-3 py-1">Full-text sıralama</span>
            <span className="rounded-full border bg-background/60 px-3 py-1">Typo toleransı</span>
            <span className="rounded-full border bg-background/60 px-3 py-1">Kategori eşleşmesi</span>
          </div>
        </div>
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
              <div className="mb-2 flex items-center justify-between gap-3">
                <h2 className="text-lg font-semibold text-foreground">&ldquo;{initialQuery}&rdquo; sonuçları</h2>
                <span className="rounded-full bg-muted px-3 py-1 text-xs font-medium text-muted-foreground">{products.length} ürün</span>
              </div>
              <div className="flex flex-col divide-y divide-border/60 rounded-3xl border border-border bg-card p-2 shadow-sm sm:p-3">
                {products.map((product) => (
                  <ProductRow key={product.id} product={product} />
                ))}
              </div>
            </div>
          ) : (
            <div className="flex flex-col items-center justify-center py-20 text-center text-muted-foreground">
              <Search className="h-12 w-12 mb-4 opacity-20" />
              <h3 className="text-xl font-semibold text-foreground mb-2">Sonuç Bulunamadı</h3>
              <p>&ldquo;{initialQuery}&rdquo; ile eşleşen bir ürün göremedik. Farklı kelimeler denemeye ne dersin?</p>
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
