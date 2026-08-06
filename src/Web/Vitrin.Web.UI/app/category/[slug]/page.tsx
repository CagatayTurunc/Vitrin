import type { Metadata } from "next";
import Image from "next/image";
import Link from "next/link";
import { ArrowLeft, ArrowUp, Eye, MessageSquare } from "lucide-react";
import type { ProductApiModel, ProductCategory } from "@/core/domain/product.types";
import { serverApiFetch } from "@/lib/server-api";

type Props = { params: Promise<{ slug: string }>; searchParams: Promise<{ period?: string }> };
const periods = [{ value: "today", label: "Bugün" }, { value: "week", label: "Bu hafta" }, { value: "month", label: "Bu ay" }, { value: "all", label: "Tüm zamanlar" }];

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { slug } = await params;
  const category = await serverApiFetch<ProductCategory>(`/categories/${encodeURIComponent(slug)}`, { revalidate: 3600, tags: ["categories"] });
  return category ? { title: `${category.name} Ürünleri — Vitrin`, description: category.description } : { title: "Kategori bulunamadı — Vitrin" };
}

export default async function CategoryPage({ params, searchParams }: Props) {
  const { slug } = await params;
  const query = await searchParams;
  const period = periods.some(item => item.value === query.period) ? query.period! : "all";
  const [category, products] = await Promise.all([
    serverApiFetch<ProductCategory>(`/categories/${encodeURIComponent(slug)}`, { revalidate: 3600, tags: ["categories"] }),
    serverApiFetch<ProductApiModel[]>(`/categories/${encodeURIComponent(slug)}/products?period=${period}&limit=50`, { revalidate: 60, tags: [`category-${slug}`] }),
  ]);

  if (!category) return <main className="mx-auto min-h-[60vh] max-w-4xl px-4 py-20 text-center"><h1 className="text-3xl font-black">Kategori bulunamadı</h1><Link href="/categories" className="mt-5 inline-flex text-emerald-700 hover:underline">Kategorilere dön</Link></main>;

  return <main className="mx-auto min-h-screen w-full max-w-6xl px-4 py-8 sm:px-6 sm:py-12">
    <Link href="/categories" className="inline-flex items-center gap-2 text-sm font-bold text-muted-foreground hover:text-foreground"><ArrowLeft className="h-4 w-4" /> Tüm kategoriler</Link>
    <section className="mt-6 rounded-[2rem] border bg-gradient-to-br from-violet-500/10 via-card to-emerald-500/10 p-8 sm:p-10"><p className="text-xs font-bold uppercase tracking-[0.18em] text-violet-700">Ürün kategorisi</p><h1 className="mt-2 text-4xl font-black tracking-tight sm:text-5xl">{category.name}</h1><p className="mt-3 max-w-2xl leading-7 text-muted-foreground">{category.description}</p><p className="mt-4 text-sm font-semibold">{category.productCount ?? products?.length ?? 0} yayınlanmış ürün</p></section>
    <nav className="my-7 flex flex-wrap gap-2" aria-label="Dönem filtresi">{periods.map(item => <Link key={item.value} href={`/category/${slug}?period=${item.value}`} className={`rounded-full border px-4 py-2 text-sm font-bold ${period === item.value ? "border-emerald-600 bg-emerald-600 text-white" : "bg-card hover:border-emerald-500/40"}`}>{item.label}</Link>)}</nav>
    <div className="space-y-3">{(products?.length ?? 0) === 0 && <div className="rounded-3xl border border-dashed py-16 text-center text-muted-foreground">Bu dönemde kategoride ürün bulunmuyor.</div>}{products?.map((product, index) => <article key={product.id} className="grid grid-cols-[38px_56px_minmax(0,1fr)] items-center gap-3 rounded-2xl border bg-card p-4 sm:grid-cols-[44px_64px_minmax(0,1fr)_auto]"><div className="text-center text-lg font-black text-muted-foreground">#{index + 1}</div><Link href={`/product/${product.slug}`} className="relative h-14 w-14 overflow-hidden rounded-2xl border bg-muted sm:h-16 sm:w-16"><Image src={product.thumbnailUrl || "/products/notai.png"} alt="" fill sizes="64px" className="object-cover" /></Link><div className="min-w-0"><Link href={`/product/${product.slug}`} className="font-extrabold hover:text-emerald-600 sm:text-lg">{product.name}</Link><p className="line-clamp-2 text-sm text-muted-foreground">{product.tagline || product.description}</p></div><div className="col-start-2 col-span-2 mt-2 flex gap-4 border-t pt-3 text-xs text-muted-foreground sm:col-auto sm:mt-0 sm:border-0 sm:pt-0"><span className="inline-flex items-center gap-1"><ArrowUp className="h-3.5 w-3.5" />{product.upvotes ?? 0}</span><span className="inline-flex items-center gap-1"><MessageSquare className="h-3.5 w-3.5" />{product.commentCount ?? 0}</span><span className="inline-flex items-center gap-1"><Eye className="h-3.5 w-3.5" />{product.viewCount ?? 0}</span></div></article>)}</div>
  </main>;
}
