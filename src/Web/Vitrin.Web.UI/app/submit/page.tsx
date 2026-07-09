"use client";

import { useState } from "react";
import { useSession } from "next-auth/react";
import { useRouter } from "next/navigation";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Sparkles, Type, AlignLeft, Globe, Link as LinkIcon, Tag, ImageIcon, ArrowRight, Check } from "lucide-react";
import { ProductRow } from "@/components/product-row"; // We'll use this for Live Preview if possible, or just a custom preview card

const AVAILABLE_CATEGORIES = [
  "SaaS", "Yapay Zeka", "Ücretsiz", "Geliştirici Araçları", "Tasarım", "Verimlilik", "Mobil", "Web", "Açık Kaynak"
];

export default function SubmitPage() {
  const { data: session, status } = useSession();
  const router = useRouter();
  
  if (status === "loading") return <div className="p-8 text-center min-h-screen">Yükleniyor...</div>;
  if (status === "unauthenticated") {
    router.push("/login");
    return null;
  }

  const role = (session?.user as any)?.role;
  const isMakerOrAdmin = role === "Maker" || role === "Admin";

  return (
    <div className="min-h-screen bg-background">
      {/* Header Area */}
      <div className="pt-16 pb-8 text-center px-4 max-w-2xl mx-auto">
        <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-secondary text-secondary-foreground border border-border text-sm mb-6">
          <Sparkles className="w-4 h-4 text-emerald-500" />
          <span>Topluluğa katıl</span>
        </div>
        {!isMakerOrAdmin ? (
          <>
            <h1 className="text-4xl sm:text-5xl font-extrabold tracking-tight mb-4">Maker Ol</h1>
            <p className="text-muted-foreground text-lg">Ürün ekleyebilmek için topluluğumuzun onaylı bir üreticisi olmalısınız. Lütfen aşağıdaki formu doldurun.</p>
          </>
        ) : (
          <>
            <h1 className="text-4xl sm:text-5xl font-extrabold tracking-tight mb-4 text-foreground">Yeni Ürün Ekle</h1>
            <p className="text-muted-foreground text-lg text-balance">Harika projenizi toplulukla paylaşın. Doğru kitleye ulaşın, geri bildirim toplayın ve oyları izleyin.</p>
          </>
        )}
      </div>

      <div className="max-w-3xl mx-auto px-4 pb-24">
        {!isMakerOrAdmin ? (
          <MakerApplicationForm userId={(session?.user as any)?.id} />
        ) : (
          <ProductSubmitForm makerId={(session?.user as any)?.id} accessToken={(session as any)?.accessToken} />
        )}
      </div>
    </div>
  );
}

function MakerApplicationForm({ userId }: { userId: string }) {
  const [portfolio, setPortfolio] = useState("");
  const [reason, setReason] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [success, setSuccess] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsSubmitting(true);
    try {
      const res = await fetch(process.env.NEXT_PUBLIC_API_URL + "/api/auth/maker-applications", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ userId, portfolioUrl: portfolio, reason })
      });
      if (res.ok) setSuccess(true);
    } catch (err) {
      console.error(err);
    } finally {
      setIsSubmitting(false);
    }
  };

  if (success) {
    return (
      <div className="text-center p-8 bg-[#1c1c1c] rounded-3xl border border-border">
        <h2 className="text-2xl font-bold mb-2 text-foreground">Başvurunuz Alındı! 🎉</h2>
        <p className="text-muted-foreground">Maker olma talebiniz yönetici onayına gönderildi. Onaylandığında ürün ekleyebileceksiniz.</p>
      </div>
    );
  }

  return (
    <div className="bg-[#1c1c1c] rounded-3xl border border-border p-6 sm:p-8 shadow-2xl">
      <form onSubmit={handleSubmit} className="space-y-8">
        <div className="space-y-3">
          <label className="text-sm font-semibold flex items-center gap-2 text-foreground">
            <LinkIcon className="w-4 h-4 text-muted-foreground" /> LinkedIn / Github veya Portfolyo Linki
          </label>
          <Input required placeholder="https://..." value={portfolio} onChange={e => setPortfolio(e.target.value)} className="bg-[#141414] border-border h-12" />
        </div>
        <div className="space-y-3">
          <label className="text-sm font-semibold flex items-center gap-2 text-foreground">
            <AlignLeft className="w-4 h-4 text-muted-foreground" /> Neden Maker Olmak İstiyorsunuz?
          </label>
          <Textarea required placeholder="Kendi projelerimi sergilemek, topluluğa katkı sağlamak..." rows={4} value={reason} onChange={e => setReason(e.target.value)} className="bg-[#141414] border-border resize-none" />
        </div>
        <div className="pt-4 border-t border-border/50 flex justify-end">
          <Button type="submit" disabled={isSubmitting} className="bg-emerald-500 hover:bg-emerald-600 text-white font-semibold rounded-full px-6 py-6 h-auto shadow-[0_0_20px_rgba(16,185,129,0.3)] transition-all">
            {isSubmitting ? "Gönderiliyor..." : "Başvuruyu Gönder"} <ArrowRight className="ml-2 w-4 h-4" />
          </Button>
        </div>
      </form>
    </div>
  );
}

function ProductSubmitForm({ makerId, accessToken }: { makerId: string, accessToken: string }) {
  const [name, setName] = useState("");
  const [tagline, setTagline] = useState("");
  const [description, setDescription] = useState("");
  const [slug, setSlug] = useState("");
  const [website, setWebsite] = useState("");
  const [logoUrl, setLogoUrl] = useState("");
  const [isUploadingLogo, setIsUploadingLogo] = useState(false);
  
  const [selectedCategories, setSelectedCategories] = useState<string[]>([]);
  
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [success, setSuccess] = useState(false);
  const [error, setError] = useState("");

  const toggleCategory = (cat: string) => {
    if (selectedCategories.includes(cat)) {
      setSelectedCategories(selectedCategories.filter(c => c !== cat));
    } else {
      if (selectedCategories.length < 4) {
        setSelectedCategories([...selectedCategories, cat]);
      }
    }
  };

  const handleLogoUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    setIsUploadingLogo(true);
    const formData = new FormData();
    formData.append("file", file);
    formData.append("upload_preset", process.env.NEXT_PUBLIC_CLOUDINARY_UPLOAD_PRESET || "");

    try {
      const res = await fetch(`https://api.cloudinary.com/v1_1/${process.env.NEXT_PUBLIC_CLOUDINARY_CLOUD_NAME}/image/upload`, {
        method: "POST",
        body: formData,
      });
      const data = await res.json();
      if (data.secure_url) {
        setLogoUrl(data.secure_url);
      }
    } catch (err) {
      console.error("Logo yükleme hatası:", err);
    } finally {
      setIsUploadingLogo(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsSubmitting(true);
    setError("");

    try {
      const res = await fetch(process.env.NEXT_PUBLIC_API_URL + "/api/products", {
        method: "POST",
        headers: { 
          "Content-Type": "application/json",
          "Authorization": `Bearer ${accessToken}`
        },
        body: JSON.stringify({ 
          makerId,
          name, 
          tagline, 
          description,
          slug: slug || name.toLowerCase().replace(/[^a-z0-9]+/g, "-").replace(/(^-|-$)+/g, ""),
          topics: selectedCategories,
          thumbnailUrl: logoUrl
        })
      });
      
      if (res.ok) setSuccess(true);
      else {
        const data = await res.json();
        setError(data.Error || "Bir hata oluştu.");
      }
    } catch (err) {
      setError("Bağlantı hatası.");
    } finally {
      setIsSubmitting(false);
    }
  };

  if (success) {
    return (
      <div className="text-center p-12 bg-[#1c1c1c] text-foreground rounded-3xl border border-emerald-500/30 shadow-[0_0_40px_rgba(16,185,129,0.1)]">
        <h2 className="text-3xl font-bold mb-4">Ürününüz Gönderildi! 🚀</h2>
        <p className="text-muted-foreground text-lg max-w-md mx-auto">Ürününüz inceleme için admin onayına gönderildi. İnceleme tamamlandığında anasayfada yer alacak.</p>
      </div>
    );
  }

  return (
    <div className="space-y-12">
      {/* Form Card */}
      <div className="bg-[#1c1c1c] rounded-3xl border border-border shadow-2xl overflow-hidden">
        <form onSubmit={handleSubmit} className="p-6 sm:p-10 space-y-10">
          
          {/* Logo Placeholder */}
          <div className="flex items-center gap-6">
            <div className="relative group w-24 h-24 rounded-2xl border-2 border-dashed border-border/60 bg-[#141414] flex flex-col items-center justify-center text-muted-foreground overflow-hidden hover:border-emerald-500/50 transition-colors cursor-pointer">
              {isUploadingLogo ? (
                <span className="text-xs font-medium animate-pulse">Yükleniyor...</span>
              ) : logoUrl ? (
                <img src={logoUrl} alt="Logo" className="w-full h-full object-cover" />
              ) : (
                <>
                  <ImageIcon className="w-8 h-8 mb-1 opacity-50 group-hover:scale-110 transition-transform" />
                  <span className="text-[10px] font-medium opacity-0 group-hover:opacity-100 transition-opacity">Yükle</span>
                </>
              )}
              <input 
                type="file" 
                accept="image/png, image/jpeg" 
                className="absolute inset-0 w-full h-full opacity-0 cursor-pointer"
                onChange={handleLogoUpload}
                disabled={isUploadingLogo}
              />
            </div>
            <div>
              <h3 className="font-semibold text-foreground text-lg">Ürün Logosu</h3>
              <p className="text-sm text-muted-foreground">Kare (1:1) PNG veya JPG. En az 240x240px önerilir.</p>
            </div>
          </div>

          <div className="space-y-4 relative">
            <div className="flex justify-between items-center">
              <label className="text-sm font-semibold flex items-center gap-2 text-foreground">
                <Type className="w-4 h-4 text-muted-foreground" /> Ürün Adı
              </label>
              <span className="text-xs text-muted-foreground">{name.length}/40</span>
            </div>
            <Input required placeholder="Örn. Vitrin" maxLength={40} value={name} onChange={e => setName(e.target.value)} className="bg-[#141414] border-border h-12 text-base" />
          </div>

          <div className="space-y-4 relative">
            <div className="flex justify-between items-center">
              <label className="text-sm font-semibold flex items-center gap-2 text-foreground">
                <AlignLeft className="w-4 h-4 text-muted-foreground" /> Kısa Açıklama (Tagline)
              </label>
              <span className="text-xs text-muted-foreground">{tagline.length}/60</span>
            </div>
            <Input required placeholder="Yerli Product Hunt alternatifi" maxLength={60} value={tagline} onChange={e => setTagline(e.target.value)} className="bg-[#141414] border-border h-12 text-base" />
          </div>

          <div className="space-y-4 relative">
            <div className="flex justify-between items-center">
              <label className="text-sm font-semibold flex items-center gap-2 text-foreground">
                <AlignLeft className="w-4 h-4 text-muted-foreground rotate-180" /> Detaylı Açıklama
              </label>
              <span className="text-xs text-muted-foreground">{description.length}/400</span>
            </div>
            <Textarea required placeholder="Ürününüzün ne yaptığını, kimin için olduğunu ve neden özel olduğunu anlatın..." maxLength={400} rows={5} value={description} onChange={e => setDescription(e.target.value)} className="bg-[#141414] border-border text-base resize-y min-h-[120px]" />
          </div>

          <div className="space-y-4 relative">
            <div className="flex justify-between items-center">
              <label className="text-sm font-semibold flex items-center gap-2 text-foreground">
                <Tag className="w-4 h-4 text-muted-foreground" /> Kategoriler
              </label>
              <span className="text-xs text-muted-foreground">{selectedCategories.length}/4 seçildi</span>
            </div>
            <div className="flex flex-wrap gap-2 pt-1">
              {AVAILABLE_CATEGORIES.map(cat => {
                const isSelected = selectedCategories.includes(cat);
                return (
                  <button
                    key={cat}
                    type="button"
                    onClick={() => toggleCategory(cat)}
                    className={`px-4 py-2 rounded-full text-sm font-medium transition-colors ${
                      isSelected 
                      ? "bg-primary text-primary-foreground border border-primary" 
                      : "bg-[#141414] text-muted-foreground border border-border hover:border-muted-foreground/50 hover:text-foreground"
                    }`}
                  >
                    {cat}
                  </button>
                )
              })}
            </div>
          </div>

          <div className="space-y-4 relative">
            <div className="flex justify-between items-center">
              <label className="text-sm font-semibold flex items-center gap-2 text-foreground">
                <Globe className="w-4 h-4 text-muted-foreground" /> Web Sitesi
              </label>
            </div>
            <Input placeholder="https://vitrin.app" value={website} onChange={e => setWebsite(e.target.value)} className="bg-[#141414] border-border h-12 text-base" />
          </div>

          <div className="space-y-4 relative">
            <div className="flex justify-between items-center">
              <label className="text-sm font-semibold flex items-center gap-2 text-foreground">
                <LinkIcon className="w-4 h-4 text-muted-foreground" /> Özel URL (Slug)
              </label>
            </div>
            <Input placeholder="vitrin.app/urun-adi" value={slug} onChange={e => setSlug(e.target.value)} className="bg-[#141414] border-border h-12 text-base" />
            <p className="text-xs text-muted-foreground mt-2">Boş bırakırsanız ürün adına göre otomatik oluşturulur.</p>
          </div>
          
          {error && <p className="text-sm text-destructive font-medium bg-destructive/10 p-3 rounded-lg border border-destructive/20">{error}</p>}
          
          <div className="pt-8 mt-8 border-t border-border/50 flex flex-col sm:flex-row items-center justify-between gap-4">
            <span className="text-muted-foreground text-sm font-medium hover:text-foreground cursor-pointer transition-colors">
              Taslak olarak kaydet
            </span>
            <Button type="submit" disabled={isSubmitting} className="w-full sm:w-auto bg-emerald-500 hover:bg-emerald-600 text-white font-semibold rounded-full px-8 py-6 h-auto shadow-[0_0_30px_rgba(16,185,129,0.25)] hover:shadow-[0_0_40px_rgba(16,185,129,0.4)] transition-all">
              {isSubmitting ? "Gönderiliyor..." : "Ürünü İncelemeye Gönder"} <ArrowRight className="ml-2 w-4 h-4" />
            </Button>
          </div>
        </form>
      </div>

      {/* Live Preview Section */}
      <div className="space-y-6">
        <h3 className="text-lg font-bold flex items-center gap-2 text-foreground">
          <Sparkles className="w-5 h-5 text-emerald-500" /> Canlı Önizleme
        </h3>
        
        <div className="bg-[#1c1c1c] rounded-3xl border border-border p-6 shadow-xl">
           <div className="flex items-center gap-4">
              <div className="w-16 h-16 rounded-xl border border-border bg-[#141414] flex items-center justify-center text-muted-foreground shrink-0 overflow-hidden">
                {logoUrl ? (
                  <img src={logoUrl} alt="Preview Logo" className="w-full h-full object-cover" />
                ) : (
                  <ImageIcon className="w-6 h-6 opacity-50" />
                )}
              </div>
              <div className="flex-1 min-w-0">
                <h4 className="text-lg font-bold text-foreground truncate">
                  {name || "Ürün Adı"}
                </h4>
                <p className="text-sm text-muted-foreground truncate mt-1">
                  {tagline || "Kısa açıklamanız burada görünecek"}
                </p>
                <div className="flex gap-2 mt-2">
                  {selectedCategories.length > 0 ? (
                    selectedCategories.slice(0, 3).map(cat => (
                      <span key={cat} className="text-xs bg-secondary text-secondary-foreground px-2 py-1 rounded-md font-medium">
                        {cat}
                      </span>
                    ))
                  ) : (
                    <span className="text-xs bg-[#141414] text-muted-foreground border border-border px-2 py-1 rounded-md font-medium">
                      Kategori
                    </span>
                  )}
                  {selectedCategories.length > 3 && (
                    <span className="text-xs bg-secondary text-secondary-foreground px-2 py-1 rounded-md font-medium">
                      +{selectedCategories.length - 3}
                    </span>
                  )}
                </div>
              </div>
              <div className="shrink-0">
                <div className="flex flex-col items-center justify-center w-12 h-14 bg-[#141414] border border-border rounded-xl">
                  <span className="text-xs text-muted-foreground">▲</span>
                  <span className="font-bold text-foreground mt-0.5">0</span>
                </div>
              </div>
           </div>
           
           <div className="mt-8 text-center border-t border-border/50 pt-4">
             <span className="text-xs text-muted-foreground font-medium">
               vitrin.app/{slug || name.toLowerCase().replace(/[^a-z0-9]+/g, "-").replace(/(^-|-$)+/g, "") || "urun-adi"}
             </span>
           </div>
        </div>

        <ul className="space-y-3 text-sm text-muted-foreground pt-4">
          <li className="flex items-start gap-2">
            <Check className="w-4 h-4 text-emerald-500 mt-0.5" />
            İncelemeler genellikle 24 saat içinde tamamlanır.
          </li>
          <li className="flex items-start gap-2">
            <Check className="w-4 h-4 text-emerald-500 mt-0.5" />
            Net bir logo ve açıklama onay şansını artırır.
          </li>
          <li className="flex items-start gap-2">
            <Check className="w-4 h-4 text-emerald-500 mt-0.5" />
            En fazla 4 kategori seçebilirsiniz.
          </li>
        </ul>
      </div>
    </div>
  );
}
