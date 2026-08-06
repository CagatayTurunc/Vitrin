"use client";

import { Suspense, useEffect, useMemo, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { useSession } from "next-auth/react";
import {
  Bell, BookmarkPlus, Check, ChevronDown, Loader2, RotateCcw, Search,
  SlidersHorizontal, Sparkles, Trash2, X,
} from "lucide-react";
import { ProductRow } from "@/components/product-row";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import type {
  Product, ProductApiModel, ProductFilters, ProductSort, SavedSearch, Topic,
} from "@/core/domain/product.types";
import { ProductRepository } from "@/core/infrastructure/product.repository";

const SORT_OPTIONS: Array<{ value: ProductSort; label: string }> = [
  { value: "relevance", label: "En alakalı" },
  { value: "newest", label: "En yeni" },
  { value: "trending", label: "Trend" },
  { value: "most_voted", label: "En çok oy alan" },
  { value: "most_commented", label: "En çok konuşulan" },
  { value: "most_viewed", label: "En çok görüntülenen" },
];

function SearchRoute() {
  const params = useSearchParams();
  const routeKey = params.toString();
  const filters = useMemo<ProductFilters>(() => {
    const routeParams = new URLSearchParams(routeKey);
    const query = routeParams.get("q")?.trim() ?? "";
    return {
      q: query,
      topics: (routeParams.get("topics") ?? "").split(",").filter(Boolean),
      minUpvotes: numberParam(routeParams.get("minUpvotes")),
      minComments: numberParam(routeParams.get("minComments")),
      minViews: numberParam(routeParams.get("minViews")),
      publishedFrom: routeParams.get("publishedFrom") ?? undefined,
      publishedTo: routeParams.get("publishedTo") ?? undefined,
      sort: parseSort(routeParams.get("sort"), query),
    };
  }, [routeKey]);

  return <SearchExperience key={routeKey} appliedFilters={filters} />;
}

function SearchExperience({ appliedFilters }: { appliedFilters: ProductFilters }) {
  const router = useRouter();
  const { data: session, status } = useSession();
  const [draft, setDraft] = useState<ProductFilters>(appliedFilters);
  const [products, setProducts] = useState<Product[]>([]);
  const [topics, setTopics] = useState<Topic[]>([]);
  const [savedSearches, setSavedSearches] = useState<SavedSearch[]>([]);
  const [nextCursor, setNextCursor] = useState<string | null>(null);
  const [hasMore, setHasMore] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [isLoadingMore, setIsLoadingMore] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [showAdvanced, setShowAdvanced] = useState(hasAdvancedFilters(appliedFilters));
  const [showSaveForm, setShowSaveForm] = useState(false);
  const [savedName, setSavedName] = useState("");
  const [notifyOnNewMatches, setNotifyOnNewMatches] = useState(true);
  const [message, setMessage] = useState("");

  useEffect(() => {
    let cancelled = false;
    ProductRepository.filterProducts(appliedFilters)
      .then((page) => {
        if (cancelled) return;
        setProducts(page.items.map(mapProduct));
        setNextCursor(page.nextCursor);
        setHasMore(page.hasMore);
      })
      .catch(() => !cancelled && setMessage("Arama sonuçları yüklenemedi. Lütfen tekrar dene."))
      .finally(() => !cancelled && setIsLoading(false));
    return () => { cancelled = true; };
  }, [appliedFilters]);

  useEffect(() => {
    let cancelled = false;
    ProductRepository.getTopics()
      .then((items) => !cancelled && setTopics(items))
      .catch(() => undefined);
    return () => { cancelled = true; };
  }, []);

  useEffect(() => {
    if (!session?.accessToken) return;
    let cancelled = false;
    ProductRepository.getSavedSearches(session.accessToken)
      .then((items) => !cancelled && setSavedSearches(items))
      .catch(() => undefined);
    return () => { cancelled = true; };
  }, [session?.accessToken]);

  const activeFilterCount = useMemo(() => [
    appliedFilters.topics?.length,
    appliedFilters.minUpvotes,
    appliedFilters.minComments,
    appliedFilters.minViews,
    appliedFilters.publishedFrom,
    appliedFilters.publishedTo,
  ].filter((value) => value !== undefined && value !== null && value !== "" && value !== 0).length, [appliedFilters]);

  const applyFilters = (event?: React.FormEvent) => {
    event?.preventDefault();
    const next = { ...draft };
    if (!next.q?.trim() && next.sort === "relevance") next.sort = "newest";
    router.push(buildSearchUrl(next));
  };

  const loadMore = async () => {
    if (!nextCursor || isLoadingMore) return;
    setIsLoadingMore(true);
    try {
      const page = await ProductRepository.filterProducts(appliedFilters, nextCursor);
      setProducts((current) => [...current, ...page.items.map(mapProduct)]);
      setNextCursor(page.nextCursor);
      setHasMore(page.hasMore);
    } catch {
      setMessage("Diğer sonuçlar yüklenemedi.");
    } finally {
      setIsLoadingMore(false);
    }
  };

  const saveSearch = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!session?.accessToken || savedName.trim().length < 2) return;
    setIsSaving(true);
    setMessage("");
    try {
      const saved = await ProductRepository.saveSearch(
        savedName.trim(), appliedFilters, notifyOnNewMatches, session.accessToken,
      );
      setSavedSearches((current) => [saved, ...current]);
      setSavedName("");
      setShowSaveForm(false);
      setMessage("Araman kaydedildi.");
    } catch {
      setMessage("Arama kaydedilemedi. Aynı isimde başka bir araman olabilir.");
    } finally {
      setIsSaving(false);
    }
  };

  const deleteSavedSearch = async (id: string) => {
    if (!session?.accessToken) return;
    try {
      await ProductRepository.deleteSavedSearch(id, session.accessToken);
      setSavedSearches((current) => current.filter((item) => item.id !== id));
    } catch {
      setMessage("Kayıtlı arama silinemedi.");
    }
  };

  return (
    <main className="min-h-screen bg-[radial-gradient(circle_at_top,rgba(16,185,129,0.10),transparent_32%)] px-4 pb-20 pt-10 sm:px-6">
      <div className="mx-auto max-w-7xl">
        <header className="relative overflow-hidden rounded-[2rem] border border-emerald-500/15 bg-card p-6 shadow-sm sm:p-9">
          <div className="pointer-events-none absolute -right-16 -top-20 h-56 w-56 rounded-full bg-emerald-500/15 blur-3xl" />
          <div className="relative max-w-4xl">
            <div className="mb-3 inline-flex items-center gap-2 text-sm font-semibold text-emerald-700 dark:text-emerald-400"><Sparkles className="h-4 w-4" /> Akıllı ürün keşfi</div>
            <h1 className="text-3xl font-extrabold tracking-tight sm:text-5xl">Aradığın ürünü gerçekten bul</h1>
            <p className="mt-3 max-w-2xl text-sm leading-6 text-muted-foreground sm:text-base">Full-text ve yazım hatası toleranslı aramayı topics, etkileşim, tarih ve sıralama filtreleriyle birlikte kullan.</p>
            <form onSubmit={applyFilters} className="mt-7 flex flex-col gap-3 sm:flex-row">
              <div className="relative flex-1">
                <Search className="pointer-events-none absolute left-4 top-1/2 h-5 w-5 -translate-y-1/2 text-muted-foreground" />
                <Input type="search" value={draft.q ?? ""} onChange={(event) => setDraft((current) => ({ ...current, q: event.target.value, sort: current.sort === "newest" && event.target.value ? "relevance" : current.sort }))} placeholder="Örn. ekipler için yapay zeka not aracı" className="h-14 rounded-2xl bg-background pl-12 text-base" />
              </div>
              <Button type="submit" className="h-14 rounded-2xl bg-emerald-600 px-8 text-white hover:bg-emerald-700"><Search className="mr-2 h-4 w-4" /> Ara</Button>
            </form>
            <div className="mt-4 flex flex-wrap gap-2 text-xs text-muted-foreground"><span className="rounded-full border bg-background/70 px-3 py-1">TSVector full-text</span><span className="rounded-full border bg-background/70 px-3 py-1">Trigram typo toleransı</span><span className="rounded-full border bg-background/70 px-3 py-1">Alaka skoru</span></div>
          </div>
        </header>

        <div className="mt-8 grid gap-7 lg:grid-cols-[300px_minmax(0,1fr)]">
          <aside className="space-y-5">
            <div className="rounded-3xl border bg-card p-5 shadow-sm">
              <div className="mb-5 flex items-center justify-between"><h2 className="flex items-center gap-2 font-bold"><SlidersHorizontal className="h-4 w-4 text-emerald-600" /> Filtreler</h2>{activeFilterCount > 0 && <span className="rounded-full bg-emerald-500/10 px-2.5 py-1 text-xs font-semibold text-emerald-700">{activeFilterCount} aktif</span>}</div>

              <FilterSection title="Sıralama">
                <div className="relative"><select value={draft.sort ?? "newest"} onChange={(event) => setDraft((current) => ({ ...current, sort: event.target.value as ProductSort }))} className="h-11 w-full appearance-none rounded-xl border bg-background px-3 pr-9 text-sm outline-none focus:border-emerald-500" aria-label="Sıralama">{SORT_OPTIONS.filter((option) => option.value !== "relevance" || Boolean(draft.q?.trim())).map((option) => <option key={option.value} value={option.value}>{option.label}</option>)}</select><ChevronDown className="pointer-events-none absolute right-3 top-3.5 h-4 w-4 text-muted-foreground" /></div>
              </FilterSection>

              <FilterSection title="Topics">
                <div className="flex max-h-56 flex-wrap gap-2 overflow-y-auto pr-1">{topics.map((topic) => { const selected = draft.topics?.includes(topic.slug) ?? false; return <button key={topic.id} type="button" onClick={() => setDraft((current) => ({ ...current, topics: toggleValue(current.topics ?? [], topic.slug) }))} className={`rounded-full border px-3 py-1.5 text-xs font-semibold transition ${selected ? "border-emerald-600 bg-emerald-600 text-white" : "bg-background hover:border-emerald-500/50"}`}>{selected && <Check className="mr-1 inline h-3 w-3" />}{topic.name}</button>; })}{topics.length === 0 && <span className="text-xs text-muted-foreground">Topics yükleniyor...</span>}</div>
              </FilterSection>

              <button type="button" onClick={() => setShowAdvanced((value) => !value)} className="flex w-full items-center justify-between border-t pt-4 text-sm font-semibold"><span>Gelişmiş filtreler</span><ChevronDown className={`h-4 w-4 transition ${showAdvanced ? "rotate-180" : ""}`} /></button>
              {showAdvanced && <div className="mt-4 space-y-4">
                <NumberFilter label="En az oy" value={draft.minUpvotes} onChange={(value) => setDraft((current) => ({ ...current, minUpvotes: value }))} />
                <NumberFilter label="En az yorum" value={draft.minComments} onChange={(value) => setDraft((current) => ({ ...current, minComments: value }))} />
                <NumberFilter label="En az görüntülenme" value={draft.minViews} onChange={(value) => setDraft((current) => ({ ...current, minViews: value }))} />
                <div className="grid grid-cols-2 gap-2"><DateFilter label="Başlangıç" value={draft.publishedFrom} onChange={(value) => setDraft((current) => ({ ...current, publishedFrom: value }))} /><DateFilter label="Bitiş" value={draft.publishedTo} onChange={(value) => setDraft((current) => ({ ...current, publishedTo: value }))} /></div>
              </div>}
              <div className="mt-5 grid grid-cols-[1fr_auto] gap-2"><Button type="button" onClick={() => applyFilters()} className="rounded-xl bg-emerald-600 text-white hover:bg-emerald-700">Filtreleri uygula</Button><Button type="button" variant="outline" aria-label="Filtreleri temizle" onClick={() => router.push("/search")} className="rounded-xl"><RotateCcw className="h-4 w-4" /></Button></div>
            </div>

            <SavedSearchPanel status={status} savedSearches={savedSearches} showSaveForm={showSaveForm} setShowSaveForm={setShowSaveForm} savedName={savedName} setSavedName={setSavedName} notify={notifyOnNewMatches} setNotify={setNotifyOnNewMatches} isSaving={isSaving} onSave={saveSearch} onApply={(saved) => router.push(buildSearchUrl(savedToFilters(saved)))} onDelete={(id) => void deleteSavedSearch(id)} />
          </aside>

          <section className="min-w-0">
            <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
              <div><p className="text-xs font-semibold uppercase tracking-[0.16em] text-emerald-600">Arama sonuçları</p><h2 className="mt-1 text-2xl font-bold">{appliedFilters.q ? `“${appliedFilters.q}” için` : "Tüm ürünler"}</h2></div>
              {!isLoading && <span className="rounded-full border bg-card px-3 py-1.5 text-xs font-semibold text-muted-foreground">{products.length}{hasMore ? "+" : ""} ürün</span>}
            </div>

            {message && <div className={`mb-4 flex items-center justify-between rounded-2xl border p-3 text-sm ${message.includes("kaydedildi") ? "border-emerald-500/20 bg-emerald-500/5 text-emerald-700" : "border-amber-500/20 bg-amber-500/5 text-amber-700"}`}><span>{message}</span><button type="button" onClick={() => setMessage("")} aria-label="Mesajı kapat"><X className="h-4 w-4" /></button></div>}

            {isLoading ? <div className="flex min-h-80 flex-col items-center justify-center rounded-3xl border bg-card text-muted-foreground"><Loader2 className="mb-3 h-8 w-8 animate-spin text-emerald-600" />Sonuçlar sıralanıyor...</div>
              : products.length > 0 ? <><div className="divide-y divide-border/60 rounded-3xl border bg-card p-2 shadow-sm sm:p-3">{products.map((product, index) => <ProductRow key={product.id} product={{ ...product, rank: index + 1 }} />)}</div>{hasMore && <Button type="button" variant="outline" onClick={() => void loadMore()} disabled={isLoadingMore} className="mt-5 h-12 w-full rounded-2xl">{isLoadingMore ? <><Loader2 className="mr-2 h-4 w-4 animate-spin" /> Yükleniyor</> : "Daha fazla sonuç göster"}</Button>}</>
                : <div className="flex min-h-80 flex-col items-center justify-center rounded-3xl border bg-card p-8 text-center"><div className="mb-4 flex h-14 w-14 items-center justify-center rounded-2xl bg-muted"><Search className="h-6 w-6 text-muted-foreground" /></div><h3 className="text-xl font-bold">Eşleşen ürün bulunamadı</h3><p className="mt-2 max-w-md text-sm leading-6 text-muted-foreground">Arama ifadesini sadeleştir, bir topic kaldır veya minimum etkileşim değerlerini düşür.</p><Button type="button" variant="outline" onClick={() => router.push("/search")} className="mt-5 rounded-xl">Filtreleri temizle</Button></div>}
          </section>
        </div>
      </div>
    </main>
  );
}

function SavedSearchPanel({ status, savedSearches, showSaveForm, setShowSaveForm, savedName, setSavedName, notify, setNotify, isSaving, onSave, onApply, onDelete }: { status: "authenticated" | "loading" | "unauthenticated"; savedSearches: SavedSearch[]; showSaveForm: boolean; setShowSaveForm: (value: boolean) => void; savedName: string; setSavedName: (value: string) => void; notify: boolean; setNotify: (value: boolean) => void; isSaving: boolean; onSave: (event: React.FormEvent) => void; onApply: (saved: SavedSearch) => void; onDelete: (id: string) => void }) {
  return <div className="rounded-3xl border bg-card p-5 shadow-sm"><div className="flex items-center justify-between"><h2 className="flex items-center gap-2 font-bold"><BookmarkPlus className="h-4 w-4 text-emerald-600" /> Kayıtlı aramalar</h2>{status === "authenticated" && <button type="button" onClick={() => setShowSaveForm(!showSaveForm)} className="text-xs font-semibold text-emerald-700 hover:underline">{showSaveForm ? "Kapat" : "Bu aramayı kaydet"}</button>}</div>
    {status === "unauthenticated" ? <p className="mt-3 text-sm leading-6 text-muted-foreground">Aramalarını kaydetmek ve yeni eşleşmelerden haberdar olmak için giriş yap.</p> : showSaveForm ? <form onSubmit={onSave} className="mt-4 space-y-3"><Input value={savedName} onChange={(event) => setSavedName(event.target.value)} minLength={2} maxLength={60} required placeholder="Örn. Haftalık AI araçları" className="h-10 rounded-xl" /><label className="flex cursor-pointer items-start gap-2 text-xs leading-5 text-muted-foreground"><input type="checkbox" checked={notify} onChange={(event) => setNotify(event.target.checked)} className="mt-1" /><Bell className="mt-0.5 h-3.5 w-3.5 shrink-0" /> Yeni eşleşmeler olduğunda bildirim al</label><Button type="submit" disabled={isSaving} className="h-10 w-full rounded-xl bg-emerald-600 text-white hover:bg-emerald-700">{isSaving ? "Kaydediliyor..." : "Aramayı kaydet"}</Button></form> : <div className="mt-4 space-y-2">{savedSearches.map((saved) => <div key={saved.id} className="group flex items-center gap-2 rounded-2xl border p-3"><button type="button" onClick={() => onApply(saved)} className="min-w-0 flex-1 text-left"><span className="block truncate text-sm font-semibold">{saved.name}</span><span className="mt-0.5 block truncate text-xs text-muted-foreground">{saved.query || saved.topics.join(", ") || "Filtrelenmiş ürünler"}</span></button>{saved.notifyOnNewMatches && <Bell className="h-3.5 w-3.5 text-emerald-600" />}<button type="button" onClick={() => onDelete(saved.id)} aria-label={`${saved.name} aramasını sil`} className="rounded-lg p-1.5 text-muted-foreground hover:bg-destructive/10 hover:text-destructive"><Trash2 className="h-3.5 w-3.5" /></button></div>)}{status === "loading" && <span className="text-xs text-muted-foreground">Yükleniyor...</span>}{status === "authenticated" && savedSearches.length === 0 && <p className="text-xs leading-5 text-muted-foreground">Henüz kayıtlı araman yok.</p>}</div>}
  </div>;
}

function FilterSection({ title, children }: { title: string; children: React.ReactNode }) { return <div className="mb-5 space-y-3"><h3 className="text-xs font-bold uppercase tracking-[0.12em] text-muted-foreground">{title}</h3>{children}</div>; }
function NumberFilter({ label, value, onChange }: { label: string; value?: number; onChange: (value?: number) => void }) { return <label className="grid grid-cols-[1fr_100px] items-center gap-3 text-sm"><span className="text-muted-foreground">{label}</span><Input type="number" min={0} value={value ?? ""} onChange={(event) => onChange(numberParam(event.target.value))} className="h-9 rounded-lg" /></label>; }
function DateFilter({ label, value, onChange }: { label: string; value?: string; onChange: (value?: string) => void }) { return <label className="space-y-1 text-xs text-muted-foreground"><span>{label}</span><Input type="date" value={value?.slice(0, 10) ?? ""} onChange={(event) => onChange(event.target.value || undefined)} className="h-9 rounded-lg px-2 text-xs" /></label>; }

function mapProduct(product: ProductApiModel): Product { return { id: product.id, name: product.name, slug: product.slug, description: product.tagline || product.description, publishedAt: product.publishedAt, image: product.thumbnailUrl || "/products/notai.png", topics: product.topics ?? [], votes: product.upvotes ?? 0, views: product.viewCount ?? 0, comments: product.commentCount ?? 0, trendScore: product.trendScore ?? 0, searchScore: product.searchScore ?? 0, matchType: product.matchType }; }
function toggleValue(values: string[], value: string) { return values.includes(value) ? values.filter((item) => item !== value) : [...values, value]; }
function numberParam(value: string | null): number | undefined { if (value === null || value.trim() === "") return undefined; const parsed = Number(value); return Number.isFinite(parsed) && parsed >= 0 ? parsed : undefined; }
function parseSort(value: string | null, query: string): ProductSort { const allowed: ProductSort[] = ["relevance", "newest", "trending", "most_voted", "most_commented", "most_viewed"]; return allowed.includes(value as ProductSort) && (value !== "relevance" || Boolean(query)) ? value as ProductSort : query ? "relevance" : "newest"; }
function hasAdvancedFilters(filters: ProductFilters) { return filters.minUpvotes !== undefined || filters.minComments !== undefined || filters.minViews !== undefined || Boolean(filters.publishedFrom || filters.publishedTo); }
function buildSearchUrl(filters: ProductFilters) { const params = new URLSearchParams(); if (filters.q?.trim()) params.set("q", filters.q.trim()); if (filters.topics?.length) params.set("topics", filters.topics.join(",")); if (filters.sort && filters.sort !== (filters.q ? "relevance" : "newest")) params.set("sort", filters.sort); if (filters.minUpvotes !== undefined) params.set("minUpvotes", String(filters.minUpvotes)); if (filters.minComments !== undefined) params.set("minComments", String(filters.minComments)); if (filters.minViews !== undefined) params.set("minViews", String(filters.minViews)); if (filters.publishedFrom) params.set("publishedFrom", filters.publishedFrom); if (filters.publishedTo) params.set("publishedTo", filters.publishedTo); return `/search${params.size ? `?${params}` : ""}`; }
function savedToFilters(saved: SavedSearch): ProductFilters { return { q: saved.query ?? undefined, topics: saved.topics, minUpvotes: saved.minUpvotes ?? undefined, minComments: saved.minComments ?? undefined, minViews: saved.minViews ?? undefined, publishedFrom: saved.publishedFrom?.slice(0, 10), publishedTo: saved.publishedTo?.slice(0, 10), sort: saved.sort }; }

export default function SearchPage() {
  return <Suspense fallback={<div className="flex min-h-screen items-center justify-center"><Loader2 className="h-7 w-7 animate-spin text-emerald-600" /></div>}><SearchRoute /></Suspense>;
}
