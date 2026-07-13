"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { Loader2, Bookmark, LayoutGrid } from "lucide-react";

export default function CollectionsPage() {
  const [collections, setCollections] = useState<any[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    fetchCollections();
  }, []);

  const fetchCollections = async () => {
    try {
      const res = await fetch(process.env.NEXT_PUBLIC_API_URL + "/api/collections");
      if (res.ok) {
        const data = await res.json();
        setCollections(data);
      }
    } catch (e) {
      console.error(e);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <main className="mx-auto w-full max-w-5xl px-4 py-8 sm:px-6 sm:py-12 min-h-screen">
      <div className="mb-10 text-center max-w-2xl mx-auto space-y-4">
        <div className="inline-flex items-center justify-center p-3 bg-primary/10 rounded-2xl mb-2">
          <LayoutGrid className="h-8 w-8 text-primary" />
        </div>
        <h1 className="text-4xl font-extrabold tracking-tight text-foreground">
          Koleksiyonları Keşfet
        </h1>
        <p className="text-lg text-muted-foreground">
          Topluluğumuz tarafından özenle hazırlanan tematik ürün listelerini inceleyin ve favorilerinizi bulun.
        </p>
      </div>

      {isLoading ? (
        <div className="flex flex-col items-center justify-center py-20 text-muted-foreground">
          <Loader2 className="h-8 w-8 animate-spin mb-4 text-primary" />
          <p>Koleksiyonlar yükleniyor...</p>
        </div>
      ) : collections.length > 0 ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {collections.map((c) => (
            <Link key={c.id} href={`/collection/${c.slug}`} className="group block">
              <div className="h-full rounded-3xl border border-border bg-card p-6 shadow-sm transition-all hover:shadow-md hover:border-primary/30 flex flex-col relative overflow-hidden">
                <div className="absolute top-0 right-0 p-4 opacity-10 group-hover:opacity-20 transition-opacity">
                  <Bookmark className="h-24 w-24 -mr-6 -mt-6" />
                </div>
                
                <h3 className="text-xl font-bold mb-2 group-hover:text-primary transition-colors relative z-10">{c.name}</h3>
                <p className="text-muted-foreground text-sm flex-1 relative z-10 line-clamp-2">
                  {c.description || "Açıklama bulunmuyor."}
                </p>
                
                <div className="mt-6 pt-4 border-t border-border flex items-center justify-between relative z-10">
                  <span className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                    Oluşturan: @{c.userId.substring(0,8)}
                  </span>
                  <span className="inline-flex items-center rounded-full bg-primary/10 px-2.5 py-0.5 text-xs font-medium text-primary">
                    {c.productCount} Ürün
                  </span>
                </div>
              </div>
            </Link>
          ))}
        </div>
      ) : (
        <div className="flex flex-col items-center justify-center py-20 text-center text-muted-foreground border border-dashed border-border rounded-3xl bg-muted/30">
          <Bookmark className="h-12 w-12 mb-4 opacity-20" />
          <h3 className="text-xl font-semibold text-foreground mb-2">Henüz Koleksiyon Yok</h3>
          <p>Platformda henüz hiç koleksiyon oluşturulmamış.</p>
        </div>
      )}
    </main>
  );
}
