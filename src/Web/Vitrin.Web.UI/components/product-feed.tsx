'use client';

import { useEffect, useMemo } from 'react';
import { ProductRow } from '@/components/product-row';
import { useProductStore } from '@/core/application/useProductStore';
import Link from 'next/link';
import { Button } from '@/components/ui/button';
import { useSession } from 'next-auth/react';
import { Product } from '@/core/domain/product.types';

function groupProducts(products: Product[]) {
  const today = new Date();
  today.setHours(0, 0, 0, 0);

  const yesterday = new Date(today);
  yesterday.setDate(yesterday.getDate() - 1);

  const lastWeek = new Date(today);
  lastWeek.setDate(lastWeek.getDate() - 7);

  const lastMonth = new Date(today);
  lastMonth.setDate(lastMonth.getDate() - 30);

  const groups = {
    today: [] as Product[],
    yesterday: [] as Product[],
    lastWeek: [] as Product[],
    lastMonth: [] as Product[],
    older: [] as Product[]
  };

  products.forEach(p => {
    if (!p.publishedAt) {
      groups.today.push(p);
      return;
    }
    const pubDate = new Date(p.publishedAt);
    if (pubDate >= today) groups.today.push(p);
    else if (pubDate >= yesterday) groups.yesterday.push(p);
    else if (pubDate >= lastWeek) groups.lastWeek.push(p);
    else if (pubDate >= lastMonth) groups.lastMonth.push(p);
    else groups.older.push(p);
  });

  // Trend skoru zaman etkisini içerir; eşitlikte oy sayısı belirleyicidir.
  Object.values(groups).forEach(group => {
    group.sort((a, b) => (b.trendScore ?? 0) - (a.trendScore ?? 0) || b.votes - a.votes);
    group.forEach((p, idx) => p.rank = idx + 1);
  });

  return groups;
}

export function ProductFeed() {
  const {
    products,
    isLoading,
    isLoadingMore,
    error,
    hasMore,
    fetchProducts,
    loadMoreProducts,
    fetchMyVotes
  } = useProductStore();
  const { data: session } = useSession();

  useEffect(() => {
    fetchProducts();
  }, [fetchProducts]);

  useEffect(() => {
    if (session?.accessToken) {
      fetchMyVotes(session.accessToken as string);
    }
  }, [session?.accessToken, fetchMyVotes]);

  const groups = useMemo(() => groupProducts(products), [products]);

  if (isLoading) {
    return <div className="p-4 text-center text-muted-foreground">Ürünler yükleniyor...</div>;
  }

  if (error) {
    return <div className="p-4 text-center text-destructive">Hata: {error}</div>;
  }

  if (products.length === 0) {
    return (
      <div className="p-12 text-center text-muted-foreground flex flex-col items-center justify-center gap-4">
        <p>Henüz ürün bulunmuyor.</p>
        <Link href="/submit">
          <Button variant="outline" className="mt-2">İlk Ürünü Sen Ekle</Button>
        </Link>
      </div>
    );
  }

  return (
    <div className="flex flex-col space-y-10">
      {groups.today.length > 0 && (
        <section aria-label="Bugünün ürünleri listesi">
          <h2 className="text-xl font-bold tracking-tight text-foreground mb-4 pl-2">Bugünün En İyileri</h2>
          <div className="rounded-3xl border border-border bg-card p-2 shadow-sm sm:p-3 flex flex-col divide-y divide-border/60">
            {groups.today.map((product) => (
              <ProductRow key={product.id} product={product} />
            ))}
          </div>
        </section>
      )}

      {groups.yesterday.length > 0 && (
        <section aria-label="Dünün ürünleri listesi">
          <h2 className="text-xl font-bold tracking-tight text-foreground mb-4 pl-2">Dünün En Popülerleri</h2>
          <div className="rounded-3xl border border-border bg-card p-2 shadow-sm sm:p-3 flex flex-col divide-y divide-border/60">
            {groups.yesterday.map((product) => (
              <ProductRow key={product.id} product={product} />
            ))}
          </div>
        </section>
      )}

      {groups.lastWeek.length > 0 && (
        <section aria-label="Geçen haftanın ürünleri listesi">
          <h2 className="text-xl font-bold tracking-tight text-foreground mb-4 pl-2">Geçen Haftanın Liderleri</h2>
          <div className="rounded-3xl border border-border bg-card p-2 shadow-sm sm:p-3 flex flex-col divide-y divide-border/60">
            {groups.lastWeek.map((product) => (
              <ProductRow key={product.id} product={product} />
            ))}
          </div>
        </section>
      )}

      {groups.lastMonth.length > 0 && (
        <section aria-label="Geçen ayın ürünleri listesi">
          <h2 className="text-xl font-bold tracking-tight text-foreground mb-4 pl-2">Geçen Ayın Parlayanları</h2>
          <div className="rounded-3xl border border-border bg-card p-2 shadow-sm sm:p-3 flex flex-col divide-y divide-border/60">
            {groups.lastMonth.map((product) => (
              <ProductRow key={product.id} product={product} />
            ))}
          </div>
        </section>
      )}
      
      {groups.older.length > 0 && (
        <section aria-label="Daha eski ürünler listesi">
          <h2 className="text-xl font-bold tracking-tight text-foreground mb-4 pl-2">Daha Eskiler</h2>
          <div className="rounded-3xl border border-border bg-card p-2 shadow-sm sm:p-3 flex flex-col divide-y divide-border/60">
            {groups.older.map((product) => (
              <ProductRow key={product.id} product={product} />
            ))}
          </div>
        </section>
      )}

      {hasMore && (
        <div className="flex justify-center">
          <Button
            type="button"
            variant="outline"
            disabled={isLoadingMore}
            onClick={() => void loadMoreProducts()}
          >
            {isLoadingMore ? 'Yükleniyor...' : 'Daha fazla ürün yükle'}
          </Button>
        </div>
      )}
    </div>
  );
}
