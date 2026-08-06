import type { Metadata } from "next";
import Link from "next/link";
import { ArrowRight, Boxes, FolderTree } from "lucide-react";
import type { ProductCategory } from "@/core/domain/product.types";
import { serverApiFetch } from "@/lib/server-api";

export const metadata: Metadata = {
  title: "Ürün Kategorileri — Vitrin",
  description: "Türkiye'nin teknoloji ürünlerini çözdükleri probleme ve kullanım alanına göre keşfet.",
};

export default async function CategoriesPage() {
  const categories = await serverApiFetch<ProductCategory[]>("/categories", { revalidate: 3600, tags: ["categories"] }) ?? [];
  const roots = categories.filter(category => !category.parentId);

  return <main className="mx-auto min-h-screen w-full max-w-7xl px-4 py-8 sm:px-6 sm:py-12">
    <section className="rounded-[2rem] border bg-gradient-to-br from-violet-500/10 via-card to-emerald-500/10 p-8 sm:p-12">
      <div className="inline-flex items-center gap-2 rounded-full bg-violet-500/10 px-3 py-1 text-xs font-bold uppercase tracking-[0.18em] text-violet-700"><FolderTree className="h-4 w-4" /> Kontrollü taksonomi</div>
      <h1 className="mt-4 text-4xl font-black tracking-tight sm:text-6xl">İhtiyacına göre ürün bul</h1>
      <p className="mt-4 max-w-2xl leading-7 text-muted-foreground">Kategoriler ürünün ne olduğunu ve hangi problemi çözdüğünü anlatır; topics ise teknoloji ve özellik etiketleridir.</p>
    </section>

    <div className="mt-8 grid gap-5 md:grid-cols-2 xl:grid-cols-3">
      {roots.map(root => {
        const children = categories.filter(category => category.parentId === root.id);
        return <section key={root.id} className="rounded-3xl border bg-card p-6 shadow-sm">
          <div className="flex items-start justify-between gap-4"><div className="rounded-2xl bg-violet-500/10 p-3 text-violet-600"><Boxes className="h-6 w-6" /></div><span className="rounded-full bg-muted px-2.5 py-1 text-xs font-semibold">{root.productCount ?? 0} ürün</span></div>
          <Link href={`/category/${root.slug}`} className="mt-5 block text-xl font-extrabold hover:text-emerald-600">{root.name}</Link>
          <p className="mt-2 min-h-12 text-sm leading-6 text-muted-foreground">{root.description}</p>
          <div className="mt-5 flex flex-wrap gap-2">{children.map(child => <Link key={child.id} href={`/category/${child.slug}`} className="rounded-full border bg-background px-3 py-1.5 text-xs font-semibold hover:border-emerald-500/40 hover:text-emerald-700">{child.name}</Link>)}</div>
          <Link href={`/category/${root.slug}`} className="mt-6 inline-flex items-center gap-1 text-sm font-bold text-emerald-700 hover:underline">Kategoriyi keşfet <ArrowRight className="h-4 w-4" /></Link>
        </section>;
      })}
    </div>
  </main>;
}
