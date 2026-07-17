"use client";

import { useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import { CheckCircle2, XCircle, Clock, Eye, Handshake } from "lucide-react";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Textarea } from "@/components/ui/textarea";
import { Label } from "@/components/ui/label";
import { useSession } from "next-auth/react";
import { getApiProblemMessage } from "@/lib/errors";

interface PendingProduct {
  id: string;
  name: string;
  slug: string;
  tagline: string;
  description: string;
  thumbnailUrl: string;
  makerId: string;
  createdAt: string;
  scheduledLaunchAt: string | null;
}

interface OwnershipClaim {
  id: string;
  productId: string;
  productName: string;
  productSlug: string;
  currentOwnerUserId: string;
  claimantUserId: string;
  claimantUsername: string;
  message: string;
  status: number;
  createdAt: string;
}

export default function AdminProducts() {
  const { data: session } = useSession();
  const [products, setProducts] = useState<PendingProduct[]>([]);
  const [claims, setClaims] = useState<OwnershipClaim[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  // Reject modal state
  const [rejectTarget, setRejectTarget] = useState<PendingProduct | null>(null);
  const [rejectReason, setRejectReason] = useState("");
  const [isRejecting, setIsRejecting] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);

  // Preview modal state
  const [previewProduct, setPreviewProduct] = useState<PendingProduct | null>(null);
  const [claimDecision, setClaimDecision] = useState<{ claim: OwnershipClaim; approved: boolean } | null>(null);
  const [claimNote, setClaimNote] = useState("");
  const [isDecidingClaim, setIsDecidingClaim] = useState(false);

  useEffect(() => {
    const token = session?.accessToken;
    if (!token) return;

    const fetchProducts = async () => {
      try {
        const [productsResponse, claimsResponse] = await Promise.all([
          fetch(process.env.NEXT_PUBLIC_API_URL + "/api/products/admin/pending", {
            headers: { Authorization: `Bearer ${token}` },
          }),
          fetch(process.env.NEXT_PUBLIC_API_URL + "/api/products/admin/claims?status=Pending", {
            headers: { Authorization: `Bearer ${token}` },
          }),
        ]);
        if (productsResponse.ok) setProducts(await productsResponse.json() as PendingProduct[]);
        if (claimsResponse.ok) setClaims(await claimsResponse.json() as OwnershipClaim[]);
      } catch (error) {
        console.error(error);
      } finally {
        setIsLoading(false);
      }
    };

    void fetchProducts();
  }, [session?.accessToken]);

  const handleApprove = async (id: string) => {
    if (!session?.accessToken) return;
    setActionError(null);
    try {
      const res = await fetch(process.env.NEXT_PUBLIC_API_URL + `/api/products/admin/${id}/approve`, {
        method: "POST",
        headers: { Authorization: `Bearer ${session.accessToken}` }
      });
      if (!res.ok) {
        const data: unknown = await res.json();
        setActionError(getApiProblemMessage(data, "Ürün onaylanamadı."));
        return;
      }
      setProducts((current) => current.filter((p) => p.id !== id));
    } catch {
      setActionError("Bağlantı hatası nedeniyle ürün onaylanamadı.");
    }
  };

  const openRejectModal = (product: PendingProduct) => {
    setRejectTarget(product);
    setRejectReason("");
  };

  const confirmReject = async () => {
    const reason = rejectReason.trim();
    if (!session?.accessToken || !rejectTarget || !reason) return;
    setActionError(null);
    setIsRejecting(true);
    try {
      const res = await fetch(process.env.NEXT_PUBLIC_API_URL + `/api/products/admin/${rejectTarget.id}/reject`, {
        method: "POST",
        headers: {
          Authorization: `Bearer ${session.accessToken}`,
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ reason }),
      });
      if (!res.ok) {
        const data: unknown = await res.json();
        setActionError(getApiProblemMessage(data, "Ürün reddedilemedi."));
        return;
      }
      setProducts((current) => current.filter((p) => p.id !== rejectTarget.id));
      setRejectTarget(null);
    } catch {
      setActionError("Bağlantı hatası nedeniyle ürün reddedilemedi.");
    } finally {
      setIsRejecting(false);
    }
  };

  const confirmClaimDecision = async () => {
    if (!session?.accessToken || !claimDecision) return;
    setIsDecidingClaim(true);
    setActionError(null);
    try {
      const response = await fetch(
        process.env.NEXT_PUBLIC_API_URL + `/api/products/admin/claims/${claimDecision.claim.id}/decision`,
        {
          method: "POST",
          headers: {
            Authorization: `Bearer ${session.accessToken}`,
            "Content-Type": "application/json",
          },
          body: JSON.stringify({ approved: claimDecision.approved, note: claimNote.trim() || null }),
        },
      );
      if (!response.ok) {
        const payload: unknown = await response.json().catch(() => null);
        setActionError(getApiProblemMessage(payload, "Sahiplik talebi sonuçlandırılamadı."));
        return;
      }
      setClaims((current) => current.filter((claim) => claim.id !== claimDecision.claim.id));
      setClaimDecision(null);
      setClaimNote("");
    } catch {
      setActionError("Bağlantı hatası nedeniyle sahiplik talebi sonuçlandırılamadı.");
    } finally {
      setIsDecidingClaim(false);
    }
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Onay Bekleyen Ürünler</h1>
        <p className="text-muted-foreground">Kullanıcıların eklediği ve yayına alınmayı bekleyen ürünler.</p>
      </div>

      {actionError && (
        <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700 dark:border-red-800 dark:bg-red-950/30 dark:text-red-300">
          {actionError}
        </div>
      )}

      <div className="rounded-md border bg-card">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Ürün</TableHead>
              <TableHead>Açıklama</TableHead>
              <TableHead>Tarih</TableHead>
              <TableHead>Durum</TableHead>
              <TableHead className="text-right">Aksiyon</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading ? (
              <TableRow>
                <TableCell colSpan={5} className="text-center h-24">Yükleniyor...</TableCell>
              </TableRow>
            ) : products.length === 0 ? (
              <TableRow>
                <TableCell colSpan={5} className="text-center h-24 text-muted-foreground">Onay bekleyen ürün bulunmuyor.</TableCell>
              </TableRow>
            ) : (
              products.map((product) => (
                <TableRow key={product.id}>
                  <TableCell className="font-medium">
                    <div className="flex items-center gap-3">
                      {product.thumbnailUrl ? (
                        // eslint-disable-next-line @next/next/no-img-element
                        <img src={product.thumbnailUrl} alt={product.name} className="h-10 w-10 rounded-md object-cover border border-border" />
                      ) : (
                        <div className="h-10 w-10 rounded-md bg-muted flex items-center justify-center">
                          <span className="text-xs font-bold text-muted-foreground">{product.name.substring(0, 2).toUpperCase()}</span>
                        </div>
                      )}
                      <div>
                        <div>{product.name}</div>
                        <div className="text-xs text-muted-foreground">{product.slug}</div>
                      </div>
                    </div>
                  </TableCell>
                  <TableCell className="max-w-[300px] truncate">{product.tagline}</TableCell>
                  <TableCell>
                    <p>{new Date(product.createdAt).toLocaleString('tr-TR', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' })}</p>
                    {product.scheduledLaunchAt && <p className="mt-1 text-xs text-sky-600">Plan: {new Date(product.scheduledLaunchAt).toLocaleString("tr-TR")}</p>}
                  </TableCell>
                  <TableCell>
                    <div className="flex items-center gap-1.5 text-amber-500 text-sm font-medium">
                      <Clock className="h-4 w-4" />
                      Bekliyor
                    </div>
                  </TableCell>
                  <TableCell className="text-right space-x-2">
                    <Button variant="outline" size="sm" onClick={() => setPreviewProduct(product)}>
                      <Eye className="mr-1.5 h-4 w-4" /> İncele
                    </Button>
                    <Button variant="outline" size="sm" className="text-green-600 hover:text-green-700 hover:bg-green-50" onClick={() => handleApprove(product.id)}>
                      <CheckCircle2 className="mr-1.5 h-4 w-4" /> Onayla
                    </Button>
                    <Button variant="outline" size="sm" className="text-red-600 hover:text-red-700 hover:bg-red-50" onClick={() => openRejectModal(product)}>
                      <XCircle className="mr-1.5 h-4 w-4" /> Reddet
                    </Button>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>

      <div className="space-y-3 pt-4">
        <div>
          <h2 className="flex items-center gap-2 text-2xl font-semibold tracking-tight">
            <Handshake className="h-5 w-5 text-emerald-500" /> Product ownership talepleri
          </h2>
          <p className="text-sm text-muted-foreground">Mevcut ürünler için maker sahiplik başvurularını doğrulayın.</p>
        </div>
        <div className="rounded-md border bg-card">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Ürün</TableHead>
                <TableHead>Talep eden</TableHead>
                <TableHead>Kanıt / açıklama</TableHead>
                <TableHead>Tarih</TableHead>
                <TableHead className="text-right">Karar</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {isLoading ? (
                <TableRow><TableCell colSpan={5} className="h-20 text-center">Yükleniyor...</TableCell></TableRow>
              ) : claims.length === 0 ? (
                <TableRow><TableCell colSpan={5} className="h-20 text-center text-muted-foreground">Bekleyen sahiplik talebi yok.</TableCell></TableRow>
              ) : claims.map((claim) => (
                <TableRow key={claim.id}>
                  <TableCell>
                    <p className="font-medium">{claim.productName}</p>
                    <p className="text-xs text-muted-foreground">{claim.productSlug}</p>
                  </TableCell>
                  <TableCell>
                    <p className="font-medium">@{claim.claimantUsername}</p>
                    <p className="max-w-[160px] truncate font-mono text-xs text-muted-foreground">{claim.claimantUserId}</p>
                  </TableCell>
                  <TableCell className="max-w-[360px] whitespace-pre-wrap text-sm">{claim.message}</TableCell>
                  <TableCell>{new Date(claim.createdAt).toLocaleString("tr-TR")}</TableCell>
                  <TableCell className="text-right space-x-2">
                    <Button size="sm" variant="outline" className="text-green-600" onClick={() => { setClaimDecision({ claim, approved: true }); setClaimNote(""); }}>
                      <CheckCircle2 className="mr-1 h-4 w-4" /> Onayla
                    </Button>
                    <Button size="sm" variant="outline" className="text-red-600" onClick={() => { setClaimDecision({ claim, approved: false }); setClaimNote(""); }}>
                      <XCircle className="mr-1 h-4 w-4" /> Reddet
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      </div>

      <Dialog open={!!claimDecision} onOpenChange={(open) => { if (!open) setClaimDecision(null); }}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>{claimDecision?.approved ? "Sahiplik talebini onayla" : "Sahiplik talebini reddet"}</DialogTitle>
            <DialogDescription>
              {claimDecision?.approved
                ? `${claimDecision.claim.productName} ürününün sahipliği @${claimDecision.claim.claimantUsername} hesabına devredilecek. Önceki sahip editör olarak takımda kalacak.`
                : `@${claimDecision?.claim.claimantUsername} tarafından gönderilen talep reddedilecek.`}
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-2 py-2">
            <Label htmlFor="claim-note">Admin notu (isteğe bağlı)</Label>
            <Textarea id="claim-note" rows={4} maxLength={500} value={claimNote} onChange={(event) => setClaimNote(event.target.value)} placeholder="Kararın gerekçesi veya doğrulama notu..." />
            <p className="text-right text-xs text-muted-foreground">{claimNote.length}/500</p>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setClaimDecision(null)} disabled={isDecidingClaim}>İptal</Button>
            <Button variant={claimDecision?.approved ? "default" : "destructive"} onClick={confirmClaimDecision} disabled={isDecidingClaim}>
              {isDecidingClaim ? "Kaydediliyor..." : claimDecision?.approved ? "Onayla ve sahipliği devret" : "Talebi reddet"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Reject Modal */}
      <Dialog open={!!rejectTarget} onOpenChange={(open) => { if (!open) setRejectTarget(null); }}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Ürünü Reddet</DialogTitle>
            <DialogDescription>
              <span className="font-semibold text-foreground">{rejectTarget?.name}</span> adlı ürünü reddediyorsunuz.
              Ürün sahibine gösterilecek reddetme sebebini yazın.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-2 py-2">
            <Label htmlFor="reject-reason">Reddetme Sebebi</Label>
            <Textarea
              id="reject-reason"
              placeholder="Örn: Ürün açıklaması yeterince detaylı değil veya içerik kurallarına aykırı."
              rows={4}
              maxLength={500}
              required
              value={rejectReason}
              onChange={(e) => setRejectReason(e.target.value)}
              className="resize-none"
            />
            <div className="flex justify-between text-xs text-muted-foreground">
              <span>Bu açıklama kullanıcıya hem ürününde hem bildirimde gösterilir.</span>
              <span>{rejectReason.length}/500</span>
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setRejectTarget(null)} disabled={isRejecting}>İptal</Button>
            <Button variant="destructive" onClick={confirmReject} disabled={isRejecting || !rejectReason.trim()}>
              {isRejecting ? "Reddediliyor..." : "Reddet"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Preview Modal */}
      <Dialog open={!!previewProduct} onOpenChange={(open) => { if (!open) setPreviewProduct(null); }}>
        <DialogContent className="sm:max-w-2xl max-h-[80vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>{previewProduct?.name}</DialogTitle>
            <DialogDescription>{previewProduct?.tagline}</DialogDescription>
          </DialogHeader>
          {previewProduct && (
            <div className="space-y-4">
              {previewProduct.thumbnailUrl && (
                // eslint-disable-next-line @next/next/no-img-element
                <img src={previewProduct.thumbnailUrl} alt={previewProduct.name} className="w-20 h-20 rounded-xl object-cover border border-border" />
              )}
              <div>
                <p className="text-sm font-semibold text-muted-foreground mb-1">Açıklama</p>
                <p className="text-sm text-foreground whitespace-pre-wrap">{previewProduct.description}</p>
              </div>
              <div>
                <p className="text-sm font-semibold text-muted-foreground">Maker ID</p>
                <p className="text-xs font-mono text-muted-foreground">{previewProduct.makerId}</p>
              </div>
            </div>
          )}
          <DialogFooter>
            <Button variant="outline" onClick={() => setPreviewProduct(null)}>Kapat</Button>
            <Button
              variant="default"
              className="bg-green-600 hover:bg-green-700 text-white"
              onClick={() => { if (previewProduct) { void handleApprove(previewProduct.id); setPreviewProduct(null); } }}
            >
              <CheckCircle2 className="mr-1.5 h-4 w-4" /> Onayla
            </Button>
            <Button
              variant="destructive"
              onClick={() => { if (previewProduct) { openRejectModal(previewProduct); setPreviewProduct(null); } }}
            >
              <XCircle className="mr-1.5 h-4 w-4" /> Reddet
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
