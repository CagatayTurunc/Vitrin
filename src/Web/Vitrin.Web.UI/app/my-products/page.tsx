"use client";

import { useCallback, useEffect, useState } from "react";
import { useSession } from "next-auth/react";
import { useRouter } from "next/navigation";
import Image from "next/image";
import Link from "next/link";
import { Button } from "@/components/ui/button";
import {
  Clock, CheckCircle2, XCircle, Archive, FileText, ArrowUpRight,
  RefreshCw, ArchiveIcon, PlusCircle, AlertCircle, CalendarClock, Handshake
} from "lucide-react";
import { getApiProblemMessage } from "@/lib/errors";
import { ProductManagementDialog } from "@/components/product-management-dialog";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";

// Matches MyProductResponse on the backend
interface MyProduct {
  id: string;
  name: string;
  slug: string;
  tagline: string;
  thumbnailUrl: string;
  status: number; // 0=Draft, 1=UnderReview, 2=Published, 3=Rejected, 4=Archived, 5=Scheduled
  rejectionReason: string | null;
  createdAt: string;
  publishedAt: string | null;
  archivedAt: string | null;
  scheduledLaunchAt: string | null;
  upvotes: number;
  isOwner: boolean;
  teamRole: number | null;
}

const STATUS_LABEL: Record<number, { label: string; colorClass: string }> = {
  0: { label: "Taslak",      colorClass: "text-muted-foreground bg-muted" },
  1: { label: "İncelemede",  colorClass: "text-amber-600 bg-amber-50 dark:bg-amber-950/30" },
  2: { label: "Yayında",     colorClass: "text-emerald-600 bg-emerald-50 dark:bg-emerald-950/30" },
  3: { label: "Reddedildi",  colorClass: "text-red-600 bg-red-50 dark:bg-red-950/30" },
  4: { label: "Arşivlendi",  colorClass: "text-slate-500 bg-slate-100 dark:bg-slate-800" },
  5: { label: "Planlandı", colorClass: "text-sky-600 bg-sky-50 dark:bg-sky-950/30" },
};

const STATUS_TABS = [
  { value: -1, label: "Tümü" },
  { value: 0, label: "Taslak" },
  { value: 1, label: "İncelemede" },
  { value: 2, label: "Yayında" },
  { value: 3, label: "Reddedildi" },
  { value: 4, label: "Arşiv" },
  { value: 5, label: "Planlandı" },
];

export default function MyProductsPage() {
  const { data: session, status } = useSession();
  const accessToken = session?.accessToken;
  const router = useRouter();
  const [products, setProducts] = useState<MyProduct[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [activeTab, setActiveTab] = useState(-1);
  const [actionLoading, setActionLoading] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [claimOpen, setClaimOpen] = useState(false);
  const [claimProduct, setClaimProduct] = useState("");
  const [claimMessage, setClaimMessage] = useState("");
  const [claimLoading, setClaimLoading] = useState(false);

  useEffect(() => {
    if (status === "unauthenticated") router.replace("/login");
  }, [router, status]);

  const fetchProducts = useCallback(async () => {
    if (!accessToken) return;
    const response = await fetch(process.env.NEXT_PUBLIC_API_URL + "/api/products/my-products", {
      headers: { Authorization: `Bearer ${accessToken}` },
    });
    if (!response.ok) throw response;
    setProducts(await response.json() as MyProduct[]);
  }, [accessToken]);

  useEffect(() => {
    if (!accessToken) return;
    fetch(process.env.NEXT_PUBLIC_API_URL + "/api/products/my-products", {
      headers: { Authorization: `Bearer ${accessToken}` },
    })
      .then((response) => (response.ok ? response.json() : Promise.reject(response)))
      .then((data: MyProduct[]) => setProducts(data))
      .catch(console.error)
      .finally(() => setIsLoading(false));
  }, [accessToken]);

  const filtered = activeTab === -1 ? products : products.filter((p) => p.status === activeTab);

  const doAction = async (productId: string, endpoint: string, method = "POST", body?: object) => {
    if (!session?.accessToken) return;
    setActionLoading(productId + endpoint);
    setActionError(null);
    try {
      const res = await fetch(process.env.NEXT_PUBLIC_API_URL + endpoint, {
        method,
        headers: {
          Authorization: `Bearer ${session.accessToken}`,
          "Content-Type": "application/json",
        },
        body: body ? JSON.stringify(body) : undefined,
      });
      if (!res.ok) {
        const data: unknown = await res.json();
        setActionError(getApiProblemMessage(data, "İşlem başarısız."));
        return;
      }
      await fetchProducts();
    } catch {
      setActionError("Bağlantı hatası.");
    } finally {
      setActionLoading(null);
    }
  };

  const submitClaim = async () => {
    if (!session?.accessToken || !claimProduct.trim() || claimMessage.trim().length < 10) return;
    setClaimLoading(true);
    setActionError(null);
    try {
      const response = await fetch(process.env.NEXT_PUBLIC_API_URL + "/api/products/claims", {
        method: "POST",
        headers: {
          Authorization: `Bearer ${session.accessToken}`,
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ productSlugOrUrl: claimProduct.trim(), message: claimMessage.trim() }),
      });
      if (!response.ok) {
        const payload: unknown = await response.json().catch(() => null);
        setActionError(getApiProblemMessage(payload, "Sahiplik talebi gönderilemedi."));
        return;
      }
      setClaimOpen(false);
      setClaimProduct("");
      setClaimMessage("");
    } catch {
      setActionError("Bağlantı hatası nedeniyle sahiplik talebi gönderilemedi.");
    } finally {
      setClaimLoading(false);
    }
  };

  if (status === "loading" || (status === "authenticated" && isLoading)) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <p className="text-muted-foreground">Yükleniyor...</p>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-background py-12 px-4">
      <div className="max-w-4xl mx-auto space-y-8">
        {/* Header */}
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-3xl font-bold tracking-tight text-foreground">Ürünlerim</h1>
            <p className="text-muted-foreground mt-1">Tüm ürünlerinizi yönetin — taslak, inceleme, yayın ve arşiv.</p>
          </div>
          <div className="flex items-center gap-2">
            <Dialog open={claimOpen} onOpenChange={setClaimOpen}>
              <DialogTrigger asChild>
                <Button variant="outline" className="rounded-full px-5">
                  <Handshake className="mr-2 h-4 w-4" /> Ürün sahipliği talep et
                </Button>
              </DialogTrigger>
              <DialogContent className="sm:max-w-md">
                <DialogHeader>
                  <DialogTitle>Product ownership claim</DialogTitle>
                  <DialogDescription>
                    Size ait mevcut bir ürün için slug veya ürün bağlantısını ve sahiplik kanıtınızı gönderin. Admin incelemesinden sonra sahiplik devredilir.
                  </DialogDescription>
                </DialogHeader>
                <div className="space-y-4 py-2">
                  <div className="space-y-2">
                    <Label htmlFor="claim-product">Ürün slug veya bağlantısı</Label>
                    <Input id="claim-product" placeholder="ornek-urun veya ürün bağlantısı" value={claimProduct} onChange={(event) => setClaimProduct(event.target.value)} />
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="claim-message">Sahiplik açıklaması</Label>
                    <Textarea id="claim-message" rows={5} maxLength={1000} placeholder="Ürünle ilişkinizi ve doğrulanabilecek kanıtı açıklayın..." value={claimMessage} onChange={(event) => setClaimMessage(event.target.value)} />
                    <p className="text-right text-xs text-muted-foreground">{claimMessage.length}/1000</p>
                  </div>
                </div>
                <DialogFooter>
                  <Button variant="outline" onClick={() => setClaimOpen(false)}>İptal</Button>
                  <Button onClick={submitClaim} disabled={claimLoading || !claimProduct.trim() || claimMessage.trim().length < 10}>
                    {claimLoading ? "Gönderiliyor..." : "Talebi gönder"}
                  </Button>
                </DialogFooter>
              </DialogContent>
            </Dialog>
            <Link href="/submit">
              <Button className="bg-emerald-500 hover:bg-emerald-600 text-white rounded-full px-5">
                <PlusCircle className="w-4 h-4 mr-2" /> Yeni Ürün
              </Button>
            </Link>
          </div>
        </div>

        {actionError && (
          <div className="flex items-center gap-2 text-sm text-red-600 bg-red-50 border border-red-200 rounded-lg p-3 dark:bg-red-950/30 dark:border-red-800">
            <AlertCircle className="w-4 h-4 shrink-0" />
            {actionError}
          </div>
        )}

        {/* Tabs */}
        <div className="flex gap-1 flex-wrap">
          {STATUS_TABS.map((tab) => {
            const count = tab.value === -1 ? products.length : products.filter((p) => p.status === tab.value).length;
            return (
              <button
                key={tab.value}
                onClick={() => setActiveTab(tab.value)}
                className={`px-4 py-1.5 rounded-full text-sm font-medium transition-colors ${
                  activeTab === tab.value
                    ? "bg-foreground text-background"
                    : "bg-muted text-muted-foreground hover:bg-muted/80"
                }`}
              >
                {tab.label}
                {count > 0 && (
                  <span className={`ml-1.5 text-xs ${activeTab === tab.value ? "opacity-70" : "text-muted-foreground"}`}>
                    {count}
                  </span>
                )}
              </button>
            );
          })}
        </div>

        {/* Product List */}
        {filtered.length === 0 ? (
          <div className="text-center py-16 text-muted-foreground">
            <FileText className="w-10 h-10 mx-auto mb-3 opacity-30" />
            <p className="text-sm">Bu kategoride ürün yok.</p>
          </div>
        ) : (
          <div className="space-y-3">
            {filtered.map((product) => {
              const statusInfo = STATUS_LABEL[product.status];
              const isActing = actionLoading?.startsWith(product.id) ?? false;
              const canEdit = product.isOwner || product.teamRole === 1;
              return (
                <div
                  key={product.id}
                  className="bg-card border border-border rounded-2xl p-4 flex items-start gap-4"
                >
                  {/* Thumbnail */}
                  <div className="relative w-14 h-14 rounded-xl overflow-hidden border border-border bg-muted shrink-0">
                    {product.thumbnailUrl ? (
                      <Image src={product.thumbnailUrl} alt={product.name} fill sizes="56px" className="object-cover" />
                    ) : (
                      <span className="absolute inset-0 flex items-center justify-center text-sm font-bold text-muted-foreground">
                        {product.name.substring(0, 2).toUpperCase()}
                      </span>
                    )}
                  </div>

                  {/* Info */}
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2 flex-wrap">
                      <span className="font-semibold text-foreground truncate">{product.name}</span>
                      <span className={`inline-flex items-center gap-1 text-xs font-medium px-2 py-0.5 rounded-full ${statusInfo.colorClass}`}>
                        {product.status === 0 && <FileText className="w-3.5 h-3.5" />}
                        {product.status === 1 && <Clock className="w-3.5 h-3.5" />}
                        {product.status === 2 && <CheckCircle2 className="w-3.5 h-3.5" />}
                        {product.status === 3 && <XCircle className="w-3.5 h-3.5" />}
                        {product.status === 4 && <Archive className="w-3.5 h-3.5" />}
                        {product.status === 5 && <CalendarClock className="w-3.5 h-3.5" />}
                        {statusInfo.label}
                      </span>
                      <span className="text-xs text-muted-foreground">
                        {product.isOwner ? "Sahibi" : product.teamRole === 1 ? "Takım · Editör" : "Takım · Görüntüleyici"}
                      </span>
                      {product.status === 2 && (
                        <span className="text-xs text-muted-foreground">▲ {product.upvotes}</span>
                      )}
                    </div>
                    <p className="text-sm text-muted-foreground truncate mt-0.5">{product.tagline}</p>

                    {/* Rejection reason */}
                    {product.status === 3 && product.rejectionReason && (
                      <div className="mt-2 text-xs text-red-600 bg-red-50 dark:bg-red-950/30 border border-red-200 dark:border-red-800 rounded-lg px-3 py-2">
                        <span className="font-semibold">Reddetme sebebi: </span>
                        {product.rejectionReason}
                      </div>
                    )}

                    <p className="text-xs text-muted-foreground mt-1.5">
                      {new Date(product.createdAt).toLocaleDateString("tr-TR", { day: "numeric", month: "long", year: "numeric" })}
                      {product.publishedAt && (
                        <> · Yayınlandı: {new Date(product.publishedAt).toLocaleDateString("tr-TR", { day: "numeric", month: "long", year: "numeric" })}</>
                      )}
                      {product.scheduledLaunchAt && product.status !== 2 && (
                        <> · Planlanan yayın: {new Date(product.scheduledLaunchAt).toLocaleString("tr-TR")}</>
                      )}
                    </p>
                  </div>

                  {/* Actions */}
                  <div className="flex flex-col gap-2 shrink-0">
                    {session?.accessToken && (
                      <ProductManagementDialog product={product} accessToken={session.accessToken} onUpdated={fetchProducts} />
                    )}
                    {/* Published: view */}
                    {product.status === 2 && (
                      <Link href={`/product/${product.slug}`} target="_blank">
                        <Button variant="outline" size="sm">
                          <ArrowUpRight className="w-3.5 h-3.5 mr-1" /> Görüntüle
                        </Button>
                      </Link>
                    )}

                    {/* Draft: submit for review */}
                    {canEdit && product.status === 0 && (
                      <Button
                        size="sm"
                        disabled={isActing}
                        className="bg-emerald-500 hover:bg-emerald-600 text-white"
                        onClick={() => doAction(product.id, `/api/products/${product.id}/submit`)}
                      >
                        {isActing ? "..." : <><RefreshCw className="w-3.5 h-3.5 mr-1" /> İncelemeye Gönder</>}
                      </Button>
                    )}

                    {/* Rejected: retract to draft to edit & resubmit */}
                    {canEdit && product.status === 3 && (
                      <Button
                        size="sm"
                        variant="outline"
                        disabled={isActing}
                        onClick={() => doAction(product.id, `/api/products/${product.id}/retract`)}
                      >
                        {isActing ? "..." : <><RefreshCw className="w-3.5 h-3.5 mr-1" /> Taslağa Al</>}
                      </Button>
                    )}

                    {/* UnderReview: retract (pull back) */}
                    {canEdit && product.status === 1 && (
                      <Button
                        size="sm"
                        variant="outline"
                        disabled={isActing}
                        onClick={() => doAction(product.id, `/api/products/${product.id}/retract`)}
                      >
                        {isActing ? "..." : "Geri Çek"}
                      </Button>
                    )}

                    {/* Published or Draft or Rejected: archive */}
                    {product.isOwner && (product.status === 2 || product.status === 0 || product.status === 3 || product.status === 5) && (
                      <Button
                        size="sm"
                        variant="ghost"
                        className="text-muted-foreground hover:text-foreground"
                        disabled={isActing}
                        onClick={() => doAction(product.id, `/api/products/${product.id}/archive`)}
                      >
                        {isActing ? "..." : <><ArchiveIcon className="w-3.5 h-3.5 mr-1" /> Arşivle</>}
                      </Button>
                    )}
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </div>
    </div>
  );
}
