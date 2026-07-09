'use client';

import { useEffect } from 'react';
import { ProductRow } from '@/components/product-row';
import { useProductStore } from '@/core/application/useProductStore';
import Link from 'next/link';
import { Button } from '@/components/ui/button';
import { useSession } from 'next-auth/react';

export function ProductFeed() {
  const { products, isLoading, error, fetchProducts, fetchMyVotes } = useProductStore();
  const { data: session, status } = useSession();

  useEffect(() => {
    fetchProducts();
  }, [fetchProducts]);

  useEffect(() => {
    if (session?.accessToken) {
      fetchMyVotes(session.accessToken as string);
    }
  }, [session?.accessToken, fetchMyVotes]);

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
    <div className="flex flex-col divide-y divide-border/60">
      {products.map((product) => (
        <ProductRow key={product.id} product={product} />
      ))}
    </div>
  );
}
