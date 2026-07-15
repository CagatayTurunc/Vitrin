"use client";

import { useEffect, useState } from "react";
import { ProductRow } from "@/components/product-row";
import { CursorPage, Product, ProductApiModel, Topic } from "@/core/domain/product.types";
import { Loader2, Hash } from "lucide-react";
import { useParams } from "next/navigation";
import { Button } from "@/components/ui/button";

export default function TopicPage() {
  const params = useParams();
  const topicSlug = params.slug as string;

  const [products, setProducts] = useState<Product[]>([]);
  const [topicName, setTopicName] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isLoadingMore, setIsLoadingMore] = useState(false);
  const [nextCursor, setNextCursor] = useState<string | null>(null);
  const [hasMore, setHasMore] = useState(false);
  const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000";

  useEffect(() => {
    if (!topicSlug) return;
    
    // Paralel olarak topic ismini ve ürünleri çekelim
    Promise.all([
      fetch(`${apiUrl}/api/topics`),
      fetch(`${apiUrl}/api/products?topicSlug=${encodeURIComponent(topicSlug)}&pageSize=20`)
    ])
    .then(async ([topicsRes, productsRes]) => {
      if (topicsRes.ok) {
        const topics = await topicsRes.json() as Topic[];
        const foundTopic = topics.find((topic) => topic.slug === topicSlug);
        setTopicName(foundTopic ? foundTopic.name : topicSlug);
      }
      
      if (productsRes.ok) {
        const page = await productsRes.json() as CursorPage<ProductApiModel>;
        const mappedProducts: Product[] = page.items.map((p: ProductApiModel, index: number) => ({
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
        setNextCursor(page.nextCursor);
        setHasMore(page.hasMore);
      } else {
        setProducts([]);
        setNextCursor(null);
        setHasMore(false);
      }
    })
    .catch((error) => {
      console.error("Veriler alınırken hata:", error);
    })
    .finally(() => {
      setIsLoading(false);
    });
  }, [apiUrl, topicSlug]);

  const loadMore = async () => {
    if (!nextCursor || isLoadingMore) return;

    setIsLoadingMore(true);
    try {
      const response = await fetch(
        `${apiUrl}/api/products?topicSlug=${encodeURIComponent(topicSlug)}&pageSize=20&cursor=${encodeURIComponent(nextCursor)}`
      );
      if (!response.ok) return;

      const page = await response.json() as CursorPage<ProductApiModel>;
      setProducts((current) => [
        ...current,
        ...page.items.map((product, index) => ({
          id: product.id,
          rank: current.length + index + 1,
          name: product.name,
          slug: product.slug,
          description: product.tagline || product.description,
          publishedAt: product.publishedAt,
          image: product.thumbnailUrl || '/products/notai.png',
          topics: product.topics || [],
          votes: product.upvotes || 0,
        }))
      ]);
      setNextCursor(page.nextCursor);
      setHasMore(page.hasMore);
    } catch (error) {
      console.error("Daha fazla ürün alınırken hata:", error);
    } finally {
      setIsLoadingMore(false);
    }
  };

  return (
    <main className="mx-auto w-full max-w-4xl px-4 py-8 sm:px-6 sm:py-12 min-h-screen">
      <div className="mb-8 flex flex-col gap-2">
        <div className="flex items-center gap-2 text-primary">
          <Hash className="h-6 w-6" />
          <h1 className="text-3xl font-extrabold tracking-tight text-foreground">
            {topicName || "Yükleniyor..."}
          </h1>
        </div>
        <p className="text-muted-foreground text-sm">
          Bu etiketle paylaşılan en popüler ürünler.
        </p>
      </div>

      <div className="mt-8">
        {isLoading ? (
          <div className="flex flex-col items-center justify-center py-20 text-muted-foreground">
            <Loader2 className="h-8 w-8 animate-spin mb-4 text-primary" />
            <p>Ürünler yükleniyor...</p>
          </div>
        ) : products.length > 0 ? (
          <div className="flex flex-col gap-6">
            <div className="rounded-3xl border border-border bg-card p-2 shadow-sm sm:p-3 flex flex-col divide-y divide-border/60">
              {products.map((product) => (
                <ProductRow key={product.id} product={product} />
              ))}
            </div>
            {hasMore && (
              <Button
                type="button"
                variant="outline"
                className="self-center"
                disabled={isLoadingMore}
                onClick={() => void loadMore()}
              >
                {isLoadingMore ? "Yükleniyor..." : "Daha fazla ürün yükle"}
              </Button>
            )}
          </div>
        ) : (
          <div className="flex flex-col items-center justify-center py-20 text-center text-muted-foreground">
            <Hash className="h-12 w-12 mb-4 opacity-20" />
            <h3 className="text-xl font-semibold text-foreground mb-2">Henüz Ürün Yok</h3>
            <p>&ldquo;{topicName}&rdquo; etiketiyle henüz hiçbir ürün eklenmemiş.</p>
          </div>
        )}
      </div>
    </main>
  );
}
