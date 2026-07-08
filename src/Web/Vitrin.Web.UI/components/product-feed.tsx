'use client';

import { useEffect } from 'react';
import { ProductRow } from '@/components/product-row';
import { useProductStore } from '@/core/application/useProductStore';

export function ProductFeed() {
  const { products, isLoading, error, fetchProducts } = useProductStore();

  useEffect(() => {
    fetchProducts();
  }, [fetchProducts]);

  if (isLoading) {
    return <div className="p-4 text-center text-muted-foreground">Ürünler yükleniyor...</div>;
  }

  if (error) {
    return <div className="p-4 text-center text-destructive">Hata: {error}</div>;
  }

  if (products.length === 0) {
    return <div className="p-4 text-center text-muted-foreground">Henüz ürün bulunmuyor.</div>;
  }

  return (
    <div className="flex flex-col divide-y divide-border/60">
      {products.map((product) => (
        <ProductRow key={product.id} product={product} />
      ))}
    </div>
  );
}
