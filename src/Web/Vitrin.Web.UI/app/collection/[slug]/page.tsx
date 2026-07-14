"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { ProductRow } from "@/components/product-row";
import { Product, ProductApiModel } from "@/core/domain/product.types";
import type { CollectionDetail } from "@/core/domain/collection.types";
import { Loader2, Bookmark, Calendar } from "lucide-react";

export default function CollectionDetailPage() {
  const params = useParams();
  const slug = params.slug as string;

  const [collection, setCollection] = useState<CollectionDetail | null>(null);
  const [products, setProducts] = useState<Product[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    if (!slug) return;

    const fetchCollectionDetails = async () => {
      try {
        const response = await fetch(
          process.env.NEXT_PUBLIC_API_URL + `/api/collections/by-slug/${slug}`,
        );
        if (!response.ok) {
          setCollection(null);
          return;
        }

        const data = await response.json() as CollectionDetail;
        setCollection(data);
        const mappedProducts: Product[] = (data.products ?? []).map(
          (product: ProductApiModel, index: number) => ({
            id: product.id,
            rank: index + 1,
            name: product.name,
            slug: product.slug,
            description: product.tagline || product.description,
            publishedAt: product.publishedAt,
            image: product.thumbnailUrl || '/products/notai.png',
            topics: product.topics || [],
            votes: product.upvotes || 0,
          }),
        );
        setProducts(mappedProducts);
      } catch (error) {
        console.error(error);
        setCollection(null);
      } finally {
        setIsLoading(false);
      }
    };

    void fetchCollectionDetails();
  }, [slug]);

  if (isLoading) {
    return (
      <div className="flex min-h-[60vh] flex-col items-center justify-center text-muted-foreground">
        <Loader2 className="h-10 w-10 animate-spin mb-4 text-primary" />
        <p>Koleksiyon yükleniyor...</p>
      </div>
    );
  }

  if (!collection) {
    return (
      <div className="flex min-h-[60vh] flex-col items-center justify-center text-center">
        <Bookmark className="h-16 w-16 mb-4 opacity-20" />
        <h1 className="text-3xl font-bold tracking-tight text-foreground mb-2">Bulunamadı</h1>
        <p className="text-muted-foreground">Böyle bir koleksiyon yok veya silinmiş olabilir.</p>
      </div>
    );
  }

  return (
    <main className="mx-auto w-full max-w-4xl px-4 py-8 sm:px-6 sm:py-12 min-h-screen">
      {/* Header */}
      <div className="rounded-3xl border border-border bg-card p-6 shadow-sm sm:p-10 mb-10 text-center relative overflow-hidden">
        <div className="absolute -left-20 -top-20 h-64 w-64 rounded-full bg-primary/5 blur-3xl" />
        <div className="relative z-10 flex flex-col items-center max-w-2xl mx-auto space-y-4">
          <div className="inline-flex h-16 w-16 items-center justify-center rounded-2xl bg-primary/10 text-primary mb-2 shadow-inner">
            <Bookmark className="h-8 w-8" />
          </div>
          <h1 className="text-3xl font-extrabold tracking-tight text-foreground sm:text-5xl">
            {collection.name}
          </h1>
          {collection.description && (
            <p className="text-lg text-muted-foreground font-medium">
              {collection.description}
            </p>
          )}
          <div className="flex flex-wrap items-center justify-center gap-4 pt-4 text-sm font-medium text-muted-foreground border-t border-border/50 w-full">
            <span className="flex items-center gap-1.5">
              Oluşturan: @{collection.userId.substring(0,8)}
            </span>
            <span className="flex items-center gap-1.5">
              <Calendar className="h-4 w-4" />
              {new Date(collection.createdAt).toLocaleDateString('tr-TR', { day: 'numeric', month: 'long', year: 'numeric' })}
            </span>
            <span className="inline-flex items-center rounded-full bg-primary/10 px-2.5 py-0.5 text-xs font-bold text-primary">
              {products.length} Ürün
            </span>
          </div>
        </div>
      </div>

      {/* Products List */}
      <div className="space-y-4 mt-8">
        {products.length > 0 ? (
          <div className="rounded-3xl border border-border bg-card p-2 shadow-sm sm:p-3 flex flex-col divide-y divide-border/60">
            {products.map((product) => (
              <ProductRow key={product.id} product={product} />
            ))}
          </div>
        ) : (
          <div className="flex flex-col items-center justify-center py-20 text-center text-muted-foreground border border-dashed border-border rounded-3xl bg-muted/30">
            <Bookmark className="h-12 w-12 mb-4 opacity-20" />
            <h3 className="text-xl font-semibold text-foreground mb-2">İçi Boş</h3>
            <p>Bu koleksiyona henüz hiçbir ürün eklenmemiş.</p>
          </div>
        )}
      </div>
    </main>
  );
}
