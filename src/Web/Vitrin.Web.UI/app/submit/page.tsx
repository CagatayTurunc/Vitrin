"use client";

import { useState, useEffect } from "react";
import { useSession } from "next-auth/react";
import { useRouter } from "next/navigation";
import Image from "next/image";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Sparkles, Type, AlignLeft, Globe, Link as LinkIcon, Tag, ImageIcon, ArrowRight, Check, X } from "lucide-react";
import dynamic from "next/dynamic";
import "@uiw/react-md-editor/markdown-editor.css";
import "@uiw/react-markdown-preview/markdown.css";
const MDEditor = dynamic(() => import("@uiw/react-md-editor"), { ssr: false });

import type { UserProfile } from "@/core/domain/user.types";
import { getApiProblemMessage } from "@/lib/errors";

function getRoleString(role: unknown): string {
  if (role === 0) return 'Member';
  if (role === 1) return 'Maker';
  if (role === 2) return 'Admin';
  return typeof role === 'string' && role ? role : 'Kullanıcı';
}

const AVAILABLE_CATEGORIES = [
  "SaaS", "Yapay Zeka", "Ücretsiz", "Geliştirici Araçları", "Tasarım", "Verimlilik", "Mobil", "Web", "Açık Kaynak"
];

export default function SubmitPage() {
  const { data: session, status } = useSession();
  const router = useRouter();
  
  const [profileData, setProfileData] = useState<UserProfile | null>(null);

  useEffect(() => {
    if (status === "unauthenticated") router.replace("/login");
  }, [router, status]);

  useEffect(() => {
    if (session?.accessToken) {
      fetch(process.env.NEXT_PUBLIC_API_URL + '/api/auth/users/me', {
        headers: {
          'Authorization': `Bearer ${session.accessToken}`
        }
      }).then(async res => {
        if (!res.ok) return null;
        const text = await res.text();
        return text ? JSON.parse(text) as UserProfile : null;
      }).then(data => {
        if (data) setProfileData(data);
      }).catch(err => console.error('Profile fetch error:', err));
    }
  }, [session]);
  
  if (status !== "authenticated" || !session?.user) {
    return <div className="p-8 text-center min-h-screen">Yükleniyor...</div>;
  }

  const currentRole = profileData?.role !== undefined ? getRoleString(profileData.role) : getRoleString(session.user.role);
  const isMakerOrAdmin = currentRole === "Maker" || currentRole === "Admin";

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
          session.accessToken
            ? <MakerApplicationForm accessToken={session.accessToken} />
            : <p className="text-center text-destructive">Oturum anahtarı bulunamadı. Lütfen yeniden giriş yapın.</p>
        ) : (
          session.accessToken
            ? <ProductSubmitForm accessToken={session.accessToken} />
            : <p className="text-center text-destructive">Oturum anahtarı bulunamadı. Lütfen yeniden giriş yapın.</p>
        )}
      </div>
    </div>
  );
}

function MakerApplicationForm({ accessToken }: { accessToken: string }) {
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
        headers: {
          "Content-Type": "application/json",
          "Authorization": `Bearer ${accessToken}`,
        },
        body: JSON.stringify({ portfolioUrl: portfolio, reason })
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
      <div className="text-center p-8 bg-card rounded-3xl border border-border">
        <h2 className="text-2xl font-bold mb-2 text-foreground">Başvurunuz Alındı! 🎉</h2>
        <p className="text-muted-foreground">Maker olma talebiniz yönetici onayına gönderildi. Onaylandığında ürün ekleyebileceksiniz.</p>
      </div>
    );
  }

  return (
    <div className="bg-card rounded-3xl border border-border p-6 sm:p-8 shadow-2xl">
      <form onSubmit={handleSubmit} className="space-y-8">
        <div className="space-y-3">
          <label className="text-sm font-semibold flex items-center gap-2 text-foreground">
            <LinkIcon className="w-4 h-4 text-muted-foreground" /> LinkedIn / Github veya Portfolyo Linki
          </label>
          <Input required placeholder="https://..." value={portfolio} onChange={e => setPortfolio(e.target.value)} className="bg-background border-border h-12" />
        </div>
        <div className="space-y-3">
          <label className="text-sm font-semibold flex items-center gap-2 text-foreground">
            <AlignLeft className="w-4 h-4 text-muted-foreground" /> Neden Maker Olmak İstiyorsunuz?
          </label>
          <Textarea required placeholder="Kendi projelerimi sergilemek, topluluğa katkı sağlamak..." rows={4} value={reason} onChange={e => setReason(e.target.value)} className="bg-background border-border resize-none" />
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

function ProductSubmitForm({ accessToken }: { accessToken: string }) {
  const [name, setName] = useState("");
  const [tagline, setTagline] = useState("");
  const [description, setDescription] = useState("");
  const [slug, setSlug] = useState("");
  const [website, setWebsite] = useState("");
  const [logoUrl, setLogoUrl] = useState("");
  const [isUploadingLogo, setIsUploadingLogo] = useState(false);
  const [galleryUrls, setGalleryUrls] = useState<string[]>([]);
  const [isUploadingGallery, setIsUploadingGallery] = useState(false);
  
  const [selectedCategories, setSelectedCategories] = useState<string[]>([]);
  
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [success, setSuccess] = useState<"submitted" | "draft" | false>(false);
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

  const handleGalleryUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = e.target.files;
    if (!files || files.length === 0) return;

    setIsUploadingGallery(true);
    
    try {
      const uploadPromises = Array.from(files).map(async (file) => {
        const formData = new FormData();
        formData.append("file", file);
        formData.append("upload_preset", process.env.NEXT_PUBLIC_CLOUDINARY_UPLOAD_PRESET || "");
        
        const res = await fetch(`https://api.cloudinary.com/v1_1/${process.env.NEXT_PUBLIC_CLOUDINARY_CLOUD_NAME}/image/upload`, {
          method: "POST",
          body: formData,
        });
        const data = await res.json();
        return data.secure_url;
      });
      
      const newUrls = await Promise.all(uploadPromises);
      setGalleryUrls(prev => [...prev, ...newUrls.filter(Boolean)]);
    } catch (err) {
      console.error("Galeri yükleme hatası:", err);
    } finally {
      setIsUploadingGallery(false);
    }
  };

  const removeGalleryImage = (index: number) => {
    setGalleryUrls(prev => prev.filter((_, i) => i !== index));
  };

  const handleSubmit = async (e: React.FormEvent | React.MouseEvent, saveAsDraft = false) => {
    e.preventDefault();
    setIsSubmitting(true);
    setError("");

    // For drafts, name is the only required field
    if (saveAsDraft && !name.trim()) {
      setError("Taslak kaydı için en azından ürün adı gereklidir.");
      setIsSubmitting(false);
      return;
    }

    try {
      const res = await fetch(process.env.NEXT_PUBLIC_API_URL + "/api/products", {
        method: "POST",
        headers: { 
          "Content-Type": "application/json",
          "Authorization": `Bearer ${accessToken}`
        },
        body: JSON.stringify({ 
          name: name || "Taslak",
          tagline: tagline || "",
          description: description || "",
          slug: slug || name.trim().toLowerCase().replace(/[^a-z0-9]+/g, "-").replace(/(^-|-$)+/g, "") || `taslak-${Date.now()}`,
          topics: selectedCategories,
          thumbnailUrl: logoUrl || "",
          galleryUrls: galleryUrls,
          saveAsDraft,
        })
      });
      
      if (res.ok) setSuccess(saveAsDraft ? "draft" : "submitted");
      else {
        const data: unknown = await res.json();
        setError(getApiProblemMessage(data, "Bir hata oluştu."));
      }
    } catch {
      setError("Bağlantı hatası.");
    } finally {
      setIsSubmitting(false);
    }
  };

  if (success === "submitted") {
    return (
      <div className="text-center p-12 bg-card text-foreground rounded-3xl border border-emerald-500/30 shadow-[0_0_40px_rgba(16,185,129,0.1)]">
        <h2 className="text-3xl font-bold mb-4">Ürününüz Gönderildi! 🚀</h2>
        <p className="text-muted-foreground text-lg max-w-md mx-auto">Ürününüz inceleme için admin onayına gönderildi. İnceleme tamamlandığında anasayfada yer alacak.</p>
        <a href="/my-products" className="inline-block mt-6 text-sm text-emerald-600 hover:underline">Ürünlerimi Görüntüle →</a>
      </div>
    );
  }

  if (success === "draft") {
    return (
      <div className="text-center p-12 bg-card text-foreground rounded-3xl border border-border shadow-xl">
        <h2 className="text-3xl font-bold mb-4">Taslak Kaydedildi 📝</h2>
        <p className="text-muted-foreground text-lg max-w-md mx-auto">Ürününüz taslak olarak kaydedildi. İstediğinizde düzenleyip incelemeye gönderebilirsiniz.</p>
        <a href="/my-products" className="inline-block mt-6 text-sm text-emerald-600 hover:underline">Ürünlerimi Görüntüle →</a>
      </div>
    );
  }

  return (
    <div className="space-y-12">
      {/* Form Card */}
      <div className="bg-card rounded-3xl border border-border shadow-2xl overflow-hidden">
        <form onSubmit={handleSubmit} className="p-6 sm:p-10 space-y-10">
          
          {/* Logo Placeholder */}
          <div className="flex items-center gap-6">
            <div className="relative group w-24 h-24 rounded-2xl border-2 border-dashed border-border/60 bg-background flex flex-col items-center justify-center text-muted-foreground overflow-hidden hover:border-emerald-500/50 transition-colors cursor-pointer">
              {isUploadingLogo ? (
                <span className="text-xs font-medium animate-pulse">Yükleniyor...</span>
              ) : logoUrl ? (
                <Image src={logoUrl} alt="Logo" fill sizes="96px" className="object-cover" />
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

          {/* Medya Galerisi */}
          <div className="space-y-4 relative">
            <div className="flex justify-between items-center">
              <label className="text-sm font-semibold flex items-center gap-2 text-foreground">
                <ImageIcon className="w-4 h-4 text-muted-foreground" /> Medya Galerisi (Ekran Görüntüleri vb.)
              </label>
              <span className="text-xs text-muted-foreground">{galleryUrls.length}/5</span>
            </div>
            
            <div className="flex gap-4 overflow-x-auto pb-4 no-scrollbar">
              {galleryUrls.map((url, i) => (
                <div key={i} className="relative w-40 h-28 flex-shrink-0 rounded-xl overflow-hidden border border-border group">
                  <Image src={url} alt={`Gallery ${i}`} fill sizes="160px" className="object-cover" />
                  <button type="button" onClick={() => removeGalleryImage(i)} className="absolute top-2 right-2 bg-black/50 text-white rounded-full p-1 opacity-0 group-hover:opacity-100 transition-opacity">
                    <X className="w-4 h-4" />
                  </button>
                </div>
              ))}
              
              {galleryUrls.length < 5 && (
                <div className="relative w-40 h-28 flex-shrink-0 rounded-xl border-2 border-dashed border-border/60 bg-background flex flex-col items-center justify-center text-muted-foreground hover:border-emerald-500/50 transition-colors cursor-pointer">
                  {isUploadingGallery ? (
                    <span className="text-xs font-medium animate-pulse">Yükleniyor...</span>
                  ) : (
                    <>
                      <ImageIcon className="w-6 h-6 mb-1 opacity-50" />
                      <span className="text-xs font-medium">Görsel Ekle</span>
                    </>
                  )}
                  <input 
                    type="file" 
                    accept="image/png, image/jpeg, image/gif" 
                    multiple
                    className="absolute inset-0 w-full h-full opacity-0 cursor-pointer"
                    onChange={handleGalleryUpload}
                    disabled={isUploadingGallery}
                  />
                </div>
              )}
            </div>
          </div>

          <div className="space-y-4 relative">
            <div className="flex justify-between items-center">
              <label className="text-sm font-semibold flex items-center gap-2 text-foreground">
                <Type className="w-4 h-4 text-muted-foreground" /> Ürün Adı
              </label>
              <span className="text-xs text-muted-foreground">{name.length}/40</span>
            </div>
            <Input required placeholder="Örn. Vitrin" maxLength={40} value={name} onChange={e => setName(e.target.value)} className="bg-background border-border h-12 text-base" />
          </div>

          <div className="space-y-4 relative">
            <div className="flex justify-between items-center">
              <label className="text-sm font-semibold flex items-center gap-2 text-foreground">
                <AlignLeft className="w-4 h-4 text-muted-foreground" /> Kısa Açıklama (Tagline)
              </label>
              <span className="text-xs text-muted-foreground">{tagline.length}/60</span>
            </div>
            <Input required placeholder="Yerli Product Hunt alternatifi" maxLength={60} value={tagline} onChange={e => setTagline(e.target.value)} className="bg-background border-border h-12 text-base" />
          </div>

          <div className="space-y-4 relative" data-color-mode="light">
            <div className="flex justify-between items-center">
              <label className="text-sm font-semibold flex items-center gap-2 text-foreground">
                <AlignLeft className="w-4 h-4 text-muted-foreground rotate-180" /> Ürün Hikayesi (Detaylı Açıklama)
              </label>
            </div>
            <div className="bg-background rounded-xl border border-border text-foreground overflow-hidden">
              <MDEditor 
                value={description} 
                onChange={(val) => setDescription(val || "")} 
                preview="edit"
                height={300}
              />
            </div>
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
                      : "bg-background text-muted-foreground border border-border hover:border-muted-foreground/50 hover:text-foreground"
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
            <Input placeholder="https://vitrin.app" value={website} onChange={e => setWebsite(e.target.value)} className="bg-background border-border h-12 text-base" />
          </div>

          <div className="space-y-4 relative">
            <div className="flex justify-between items-center">
              <label className="text-sm font-semibold flex items-center gap-2 text-foreground">
                <LinkIcon className="w-4 h-4 text-muted-foreground" /> Özel URL (Slug)
              </label>
            </div>
            <Input placeholder="vitrin.app/urun-adi" value={slug} onChange={e => setSlug(e.target.value)} className="bg-background border-border h-12 text-base" />
            <p className="text-xs text-muted-foreground mt-2">Boş bırakırsanız ürün adına göre otomatik oluşturulur.</p>
          </div>
          
          {error && <p className="text-sm text-destructive font-medium bg-destructive/10 p-3 rounded-lg border border-destructive/20">{error}</p>}
          
          <div className="pt-8 mt-8 border-t border-border/50 flex flex-col sm:flex-row items-center justify-between gap-4">
            <button
              type="button"
              disabled={isSubmitting || isUploadingLogo || isUploadingGallery}
              onClick={(e) => { e.preventDefault(); void handleSubmit(e, true); }}
              className="text-muted-foreground text-sm font-medium hover:text-foreground cursor-pointer transition-colors disabled:opacity-50"
            >
              Taslak olarak kaydet
            </button>
            <Button
              type="submit"
              disabled={isSubmitting || isUploadingLogo || isUploadingGallery}
              className="w-full sm:w-auto bg-emerald-500 hover:bg-emerald-600 text-white font-semibold rounded-full px-8 py-6 h-auto shadow-[0_0_30px_rgba(16,185,129,0.25)] hover:shadow-[0_0_40px_rgba(16,185,129,0.4)] transition-all"
            >
              {isSubmitting ? "Gönderiliyor..." : (isUploadingLogo || isUploadingGallery) ? "Görseller Yükleniyor..." : "Ürünü İncelemeye Gönder"} <ArrowRight className="ml-2 w-4 h-4" />
            </Button>
          </div>
        </form>
      </div>

      {/* Live Preview Section */}
      <div className="space-y-6">
        <h3 className="text-lg font-bold flex items-center gap-2 text-foreground">
          <Sparkles className="w-5 h-5 text-emerald-500" /> Canlı Önizleme
        </h3>
        
        <div className="bg-card rounded-3xl border border-border p-6 shadow-xl">
           <div className="flex items-center gap-4">
              <div className="relative w-16 h-16 rounded-xl border border-border bg-background flex items-center justify-center text-muted-foreground shrink-0 overflow-hidden">
                {logoUrl ? (
                  <Image src={logoUrl} alt="Preview Logo" fill sizes="64px" className="object-cover" />
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
                    <span className="text-xs bg-background text-muted-foreground border border-border px-2 py-1 rounded-md font-medium">
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
                <div className="flex flex-col items-center justify-center w-12 h-14 bg-background border border-border rounded-xl">
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
