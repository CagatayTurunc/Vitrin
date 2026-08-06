"use client";

import { useEffect, useState } from "react";
import { useSession } from "next-auth/react";
import { Loader2, Copy, Code, ArrowRight } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import type { ProductApiModel } from "@/core/domain/product.types";

const API_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";

export default function BadgeBuilderPage() {
  const { data: session } = useSession();
  const [products, setProducts] = useState<ProductApiModel[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [selectedProductId, setSelectedProductId] = useState<string>("");
  const [theme, setTheme] = useState<"light" | "dark">("light");
  
  useEffect(() => {
    if (!session?.user?.id) return;
    
    fetch(`${API_URL}/api/products/maker/${session.user.id}`)
      .then(res => res.ok ? res.json() : [])
      .then(data => {
        setProducts(data);
        if (data.length > 0) {
          setSelectedProductId(data[0].id);
        }
      })
      .catch(console.error)
      .finally(() => setIsLoading(false));
  }, [session]);

  const embedCode = `<iframe src="https://vitrin.com/badge/${selectedProductId}?theme=${theme}" width="250" height="54" frameborder="0" scrolling="no"></iframe>`;

  const copyToClipboard = () => {
    navigator.clipboard.writeText(embedCode);
    alert("Embed kodu kopyalandı!");
  };

  if (isLoading) {
    return <div className="flex h-64 items-center justify-center"><Loader2 className="w-8 h-8 animate-spin text-primary" /></div>;
  }

  if (products.length === 0) {
    return (
      <div className="rounded-3xl border border-dashed p-12 text-center">
        <Code className="mx-auto mb-4 w-12 h-12 text-muted-foreground opacity-20" />
        <h2 className="text-xl font-bold mb-2">Henüz ürününüz yok</h2>
        <p className="text-muted-foreground">Badge oluşturabilmek için önce bir ürün paylaşmalısınız.</p>
      </div>
    );
  }

  return (
    <div className="max-w-4xl mx-auto py-8">
      <div className="mb-8">
        <h1 className="text-3xl font-extrabold mb-2">Embed Badge Builder</h1>
        <p className="text-muted-foreground">Ürününüzün Vitrin'deki canlı performansını web sitenizde veya Github Readme dosyanızda sergileyin.</p>
      </div>

      <div className="grid md:grid-cols-2 gap-8">
        <div className="space-y-6 rounded-3xl border bg-card p-6">
          <div>
            <label className="block text-sm font-semibold mb-2">Ürün Seçimi</label>
            <select 
              value={selectedProductId}
              onChange={e => setSelectedProductId(e.target.value)}
              className="w-full h-10 rounded-lg border border-input bg-background px-3 text-sm"
            >
              {products.map(p => (
                <option key={p.id} value={p.id}>{p.name}</option>
              ))}
            </select>
          </div>

          <div>
            <label className="block text-sm font-semibold mb-2">Tema</label>
            <div className="flex gap-4">
              <label className="flex items-center gap-2">
                <input type="radio" name="theme" value="light" checked={theme === 'light'} onChange={() => setTheme('light')} className="accent-primary" />
                Açık Tema
              </label>
              <label className="flex items-center gap-2">
                <input type="radio" name="theme" value="dark" checked={theme === 'dark'} onChange={() => setTheme('dark')} className="accent-primary" />
                Koyu Tema
              </label>
            </div>
          </div>

          <div>
            <label className="block text-sm font-semibold mb-2">Embed Kodu</label>
            <div className="relative">
              <textarea 
                readOnly
                value={embedCode}
                className="w-full h-24 rounded-lg border border-input bg-muted/50 p-3 text-sm font-mono focus:outline-none"
              />
              <Button size="sm" variant="secondary" className="absolute bottom-3 right-3" onClick={copyToClipboard}>
                <Copy className="w-4 h-4 mr-2" /> Kopyala
              </Button>
            </div>
          </div>
        </div>

        <div className="rounded-3xl border bg-muted/10 p-6 flex flex-col items-center justify-center text-center">
          <h3 className="text-sm font-semibold text-muted-foreground mb-8">CANLI ÖNİZLEME</h3>
          
          <div className="bg-checkered p-8 rounded-2xl border bg-background/50 flex items-center justify-center relative w-full overflow-hidden">
            <iframe 
              src={`/badge/${selectedProductId}?theme=${theme}`} 
              width="250" 
              height="54" 
              frameBorder="0" 
              scrolling="no"
              className="relative z-10"
            />
          </div>
          
          <p className="text-xs text-muted-foreground mt-8 max-w-xs">
            Badge her zaman ürününüzün güncel oy sayısını gösterir. Tıklandığında kullanıcıları Vitrin'deki ürün sayfanıza yönlendirir.
          </p>
        </div>
      </div>
    </div>
  );
}
