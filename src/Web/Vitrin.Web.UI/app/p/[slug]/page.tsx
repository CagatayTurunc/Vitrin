"use client";
import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { Loader2, MessagesSquare } from "lucide-react";
import { CommunityFeed } from "@/components/community-feed";
import type { ProductApiModel } from "@/core/domain/product.types";
const API_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000";
export default function ProductForumPage() {
  const { slug } = useParams<{ slug: string }>(); const [product, setProduct] = useState<ProductApiModel | null>();
  useEffect(() => { void fetch(`${API_URL}/api/products/${slug}`).then(async response => setProduct(response.ok ? await response.json() : null)); }, [slug]);
  if (product === undefined) return <div className="flex min-h-[60vh] items-center justify-center"><Loader2 className="h-8 w-8 animate-spin" /></div>;
  if (!product) return <div className="py-24 text-center">Ürün bulunamadı.</div>;
  return <main className="mx-auto min-h-screen w-full max-w-5xl px-4 py-10 sm:px-6"><section className="mb-8 rounded-[2rem] border bg-card p-8"><div className="mb-3 flex items-center gap-3"><MessagesSquare className="h-7 w-7 text-primary" /><span className="text-sm font-bold text-primary">Ürün forumu</span></div><h1 className="text-4xl font-black">{product.name} topluluğu</h1><p className="mt-2 text-muted-foreground">Destek, geri bildirim, güncellemeler ve maker ekibinin resmî cevapları.</p></section><CommunityFeed productId={product.id} productName={product.name} /></main>;
}
