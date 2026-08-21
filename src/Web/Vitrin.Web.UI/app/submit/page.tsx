"use client";

import { useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { useSession } from "next-auth/react";
import Image from "next/image";
import Link from "next/link";
import {
  AlignLeft, ArrowLeft, ArrowRight, Check, CheckCircle2, Eye, Globe2, ImageIcon,
  CalendarClock, Images, Link as LinkIcon, LoaderCircle, Rocket, Save, Sparkles, Tag, Type, UploadCloud, X,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import type { UserProfile } from "@/core/domain/user.types";
import type { ProductCategory } from "@/core/domain/product.types";
import { getApiProblemMessage } from "@/lib/errors";

const STEPS = [
  { title: "İsim", icon: Type },
  { title: "Açıklama", icon: AlignLeft },
  { title: "Görseller", icon: Images },
  { title: "Kategori & Topics", icon: Tag },
  { title: "Lansman", icon: CalendarClock },
  { title: "Önizleme", icon: Eye },
  { title: "Gönder", icon: Rocket },
];

const AVAILABLE_TOPICS = [
  "SaaS", "Yapay Zeka", "Ücretsiz", "Geliştirici Araçları", "Tasarım",
  "Verimlilik", "Mobil", "Web", "Açık Kaynak", "E-ticaret", "Fintech", "Eğitim",
];

type SubmitResult = "submitted" | "draft" | null;
type CloudinaryUpload = { secure_url?: string };

export default function SubmitPage() {
  const { data: session, status } = useSession();
  const router = useRouter();
  const [profile, setProfile] = useState<UserProfile | null>(null);

  useEffect(() => {
    if (status === "unauthenticated") router.replace("/login");
  }, [router, status]);

  useEffect(() => {
    if (!session?.accessToken) return;
    void fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/auth/users/me`, {
      headers: { Authorization: `Bearer ${session.accessToken}` },
    }).then(async (response) => response.ok ? response.json() as Promise<UserProfile> : null)
      .then((data) => data && setProfile(data))
      .catch(() => undefined);
  }, [session]);

  if (status !== "authenticated" || !session?.user) {
    return <div className="flex min-h-[70vh] items-center justify-center"><LoaderCircle className="h-7 w-7 animate-spin text-emerald-600" /></div>;
  }

  const role = profile?.role ?? session.user.role;
  const normalizedRole = typeof role === "number" ? ["Member", "Maker", "Admin"][role] : String(role);
  const canSubmit = normalizedRole === "Maker" || normalizedRole === "Admin";

  return (
    <main className="min-h-screen bg-[radial-gradient(circle_at_top,rgba(16,185,129,0.10),transparent_34%)] px-4 pb-24 pt-14">
      <div className="mx-auto max-w-5xl">
        <div className="mb-9 text-center">
          <div className="mb-4 inline-flex items-center gap-2 rounded-full border bg-background/80 px-3 py-1 text-sm"><Sparkles className="h-4 w-4 text-emerald-500" /> Topluluğa katıl</div>
          <h1 className="text-4xl font-extrabold tracking-tight sm:text-5xl">{canSubmit ? "Yeni ürün ekle" : "Maker ol"}</h1>
          <p className="mx-auto mt-4 max-w-2xl text-lg text-muted-foreground">
            {canSubmit ? "Ürününü adım adım hazırla, son halini kontrol et ve incelemeye gönder." : "Ürün gönderebilmek için maker başvurunu tamamla."}
          </p>
        </div>
        {!session.accessToken ? <p className="text-center text-destructive">Oturum anahtarı bulunamadı. Lütfen yeniden giriş yap.</p>
          : canSubmit ? <ProductWizard accessToken={session.accessToken} /> : <MakerApplicationForm accessToken={session.accessToken} />}
      </div>
    </main>
  );
}

function MakerApplicationForm({ accessToken }: { accessToken: string }) {
  const [portfolioUrl, setPortfolioUrl] = useState("");
  const [reason, setReason] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitted, setSubmitted] = useState(false);
  const [error, setError] = useState("");

  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    setIsSubmitting(true);
    setError("");
    try {
      const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/auth/maker-applications`, {
        method: "POST",
        headers: { "Content-Type": "application/json", Authorization: `Bearer ${accessToken}` },
        body: JSON.stringify({ portfolioUrl, reason }),
      });
      if (!response.ok) {
        const text = await response.text();
        let data: unknown;
        try { data = JSON.parse(text); } catch { data = null; }
        // Debug: tam response'u console'a yaz
        console.error("[maker-app] HTTP", response.status, "body:", text);
        const message = getApiProblemMessage(data, `Başvuru gönderilemedi. (HTTP ${response.status})`);
        throw new Error(message);
      }
      setSubmitted(true);
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "Bağlantı hatası oluştu.");
    } finally {
      setIsSubmitting(false);
    }
  };

  if (submitted) return <SuccessCard title="Başvurun alındı" body="Maker başvurun yönetici incelemesine gönderildi. Onaylandığında hem uygulama içinden hem e-postayla haber vereceğiz." />;

  return (
    <form onSubmit={submit} className="mx-auto max-w-2xl space-y-6 rounded-3xl border bg-card p-7 shadow-xl sm:p-10">
      <FieldLabel icon={LinkIcon} label="LinkedIn, GitHub veya portfolyo bağlantın" />
      <Input type="url" required value={portfolioUrl} onChange={(event) => setPortfolioUrl(event.target.value)} placeholder="https://github.com/kullanici" className="h-12 rounded-xl" />
      <FieldLabel icon={AlignLeft} label="Neden maker olmak istiyorsun?" />
      <Textarea required minLength={30} maxLength={1000} value={reason} onChange={(event) => setReason(event.target.value)} placeholder="Ürettiğin ürünlerden ve topluluğa nasıl katkı sunacağından bahset..." className="min-h-36 rounded-xl" />
      {error && <ErrorBox message={error} />}
      <Button type="submit" disabled={isSubmitting} className="h-12 w-full rounded-xl bg-emerald-600 text-white hover:bg-emerald-700">{isSubmitting ? "Gönderiliyor..." : "Başvuruyu gönder"}</Button>
    </form>
  );
}

function ProductWizard({ accessToken }: { accessToken: string }) {
  const [step, setStep] = useState(0);
  const [name, setName] = useState("");
  const [tagline, setTagline] = useState("");
  const [websiteUrl, setWebsiteUrl] = useState("");
  const [slug, setSlug] = useState("");
  const [slugEdited, setSlugEdited] = useState(false);
  const [description, setDescription] = useState("");
  const [thumbnailUrl, setThumbnailUrl] = useState("");
  const [galleryUrls, setGalleryUrls] = useState<string[]>([]);
  const [topics, setTopics] = useState<string[]>([]);
  const [categoryOptions, setCategoryOptions] = useState<ProductCategory[]>([]);
  const [categories, setCategories] = useState<string[]>([]);
  const [launchVersionLabel, setLaunchVersionLabel] = useState("İlk Lansman");
  const [launchTagline, setLaunchTagline] = useState("");
  const [scheduleMode, setScheduleMode] = useState<"asap" | "scheduled">("asap");
  const [scheduledLaunchAt, setScheduledLaunchAt] = useState("");
  const [isUploading, setIsUploading] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [result, setResult] = useState<SubmitResult>(null);
  const [error, setError] = useState("");

  const productPath = useMemo(() => slug || slugify(name) || "urun-adi", [name, slug]);

  useEffect(() => {
    const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000";
    void fetch(`${apiUrl}/api/categories`)
      .then(response => response.ok ? response.json() as Promise<ProductCategory[]> : [])
      .then(setCategoryOptions)
      .catch(() => setCategoryOptions([]));
  }, []);

  const updateName = (value: string) => {
    setName(value);
    if (!slugEdited) setSlug(slugify(value));
  };

  const validateStep = (index: number) => {
    if (index === 0) {
      if (name.trim().length < 2) return "Ürün adı en az 2 karakter olmalı.";
      if (tagline.trim().length < 10) return "Kısa açıklama en az 10 karakter olmalı.";
      if (!slug) return "Geçerli bir ürün URL'si oluşturulamadı.";
      if (websiteUrl && !isHttpUrl(websiteUrl)) return "Web sitesi http:// veya https:// ile başlayan geçerli bir adres olmalı.";
    }
    if (index === 1 && description.trim().length < 80) return "Ürün hikayesi en az 80 karakter olmalı.";
    if (index === 2 && !thumbnailUrl) return "İncelemeye göndermek için bir ürün logosu yüklemelisin.";
    if (index === 3 && categories.length === 0) return "En az bir ürün kategorisi seçmelisin.";
    if (index === 3 && topics.length === 0) return "En az bir topic seçmelisin.";
    if (index === 4) {
      if (!launchVersionLabel.trim()) return "Lansman sürümü veya adı zorunludur.";
      if (launchVersionLabel.trim().length > 80) return "Lansman adı en fazla 80 karakter olabilir.";
      if ((launchTagline.trim() || tagline.trim()).length < 10) return "Lansman mesajı en az 10 karakter olmalı.";
      if (scheduleMode === "scheduled") {
        if (!scheduledLaunchAt) return "Planlı lansman için tarih ve saat seçmelisin.";
        if (new Date(scheduledLaunchAt).getTime() <= Date.now() + 5 * 60 * 1000) return "Lansman zamanı en az 5 dakika ileride olmalı.";
      }
    }
    return "";
  };

  const goNext = () => {
    const problem = validateStep(step);
    if (problem) return setError(problem);
    setError("");
    setStep((current) => Math.min(current + 1, STEPS.length - 1));
    window.scrollTo({ top: 0, behavior: "smooth" });
  };

  const upload = async (files: File[], kind: "thumbnail" | "gallery") => {
    const remaining = kind === "gallery" ? Math.max(0, 5 - galleryUrls.length) : 1;
    const selected = files.slice(0, remaining);
    if (!selected.length) return;
    const invalid = selected.find((file) => !file.type.startsWith("image/") || file.size > 8 * 1024 * 1024);
    if (invalid) return setError("Görseller PNG, JPG, WebP veya GIF olmalı ve dosya başına 8 MB'ı geçmemeli.");

    const cloudName = process.env.NEXT_PUBLIC_CLOUDINARY_CLOUD_NAME;
    const uploadPreset = process.env.NEXT_PUBLIC_CLOUDINARY_UPLOAD_PRESET;
    if (!cloudName || !uploadPreset || cloudName === "your_cloud_name") return setError("Cloudinary ayarları eksik. .env dosyasındaki cloud name ve upload preset değerlerini doldur.");

    setIsUploading(true);
    setError("");
    try {
      const urls = await Promise.all(selected.map(async (file) => {
        const body = new FormData();
        body.append("file", file);
        body.append("upload_preset", uploadPreset);
        body.append("folder", kind === "thumbnail" ? "vitrin/products/thumbnails" : "vitrin/products/gallery");
        const response = await fetch(`https://api.cloudinary.com/v1_1/${cloudName}/image/upload`, { method: "POST", body });
        const data = await response.json() as CloudinaryUpload & { error?: { message?: string } };
        if (!response.ok || !data.secure_url) throw new Error(data.error?.message ?? "Cloudinary yüklemesi başarısız oldu.");
        return kind === "thumbnail" ? cloudinaryTransform(data.secure_url, "c_fill,g_auto,w_480,h_480,f_auto,q_auto") : cloudinaryTransform(data.secure_url, "f_auto,q_auto");
      }));
      if (kind === "thumbnail") setThumbnailUrl(urls[0]);
      else setGalleryUrls((current) => [...current, ...urls].slice(0, 5));
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "Görsel yüklenemedi.");
    } finally {
      setIsUploading(false);
    }
  };

  const toggleTopic = (topic: string) => {
    setTopics((current) => current.includes(topic) ? current.filter((item) => item !== topic) : current.length < 4 ? [...current, topic] : current);
  };

  const toggleCategory = (categorySlug: string) => {
    setCategories((current) => current.includes(categorySlug)
      ? current.filter((item) => item !== categorySlug)
      : current.length < 3 ? [...current, categorySlug] : current);
  };

  const submitProduct = async (saveAsDraft: boolean) => {
    if (!name.trim()) return setError("Taslak için en az ürün adı gereklidir.");
    if (!saveAsDraft) {
      for (let index = 0; index <= 4; index += 1) {
        const problem = validateStep(index);
        if (problem) { setStep(index); setError(problem); return; }
      }
    }

    setIsSubmitting(true);
    setError("");
    try {
      const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/products`, {
        method: "POST",
        headers: { "Content-Type": "application/json", Authorization: `Bearer ${accessToken}` },
        body: JSON.stringify({
          name: name.trim(), tagline: tagline.trim(), description: description.trim(), slug: productPath,
          topics, categories, thumbnailUrl, galleryUrls, websiteUrl: websiteUrl.trim() || null, saveAsDraft,
          launchVersionLabel: launchVersionLabel.trim(),
          launchTagline: launchTagline.trim() || tagline.trim(),
          scheduledLaunchAt: scheduleMode === "scheduled" && scheduledLaunchAt
            ? new Date(scheduledLaunchAt).toISOString()
            : null,
        }),
      });
      if (!response.ok) {
        const data: unknown = await response.json();
        throw new Error(getApiProblemMessage(data, "Ürün kaydedilemedi."));
      }
      setResult(saveAsDraft ? "draft" : "submitted");
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "Bağlantı hatası oluştu.");
    } finally {
      setIsSubmitting(false);
    }
  };

  if (result) return <SuccessCard title={result === "submitted" ? "Ürünün incelemeye gönderildi" : "Taslağın kaydedildi"} body={result === "submitted" ? "İnceleme tamamlandığında uygulama içinden ve e-posta tercihlerine göre bildirim alacaksın." : "Ürünlerim sayfasından düzenlemeye devam edebilirsin."} />;

  return (
    <div className="grid gap-8 lg:grid-cols-[220px_minmax(0,1fr)]">
      <nav aria-label="Ürün gönderme adımları" className="h-fit rounded-3xl border bg-card p-4 shadow-sm lg:sticky lg:top-24">
        <ol className="grid grid-cols-3 gap-2 lg:grid-cols-1">
          {STEPS.map((item, index) => {
            const Icon = item.icon;
            return <li key={item.title}><button type="button" onClick={() => index <= step && setStep(index)} className={`flex w-full items-center gap-3 rounded-2xl px-3 py-3 text-left text-sm transition ${index === step ? "bg-emerald-600 text-white" : index < step ? "text-emerald-700 hover:bg-emerald-500/10" : "text-muted-foreground"}`}><span className={`flex h-7 w-7 shrink-0 items-center justify-center rounded-full ${index === step ? "bg-white/15" : "bg-muted"}`}>{index < step ? <Check className="h-4 w-4" /> : <Icon className="h-4 w-4" />}</span><span className="hidden lg:inline">{index + 1}. {item.title}</span></button></li>;
          })}
        </ol>
        <div className="mt-4 h-1.5 overflow-hidden rounded-full bg-muted"><div className="h-full rounded-full bg-emerald-500 transition-all" style={{ width: `${((step + 1) / STEPS.length) * 100}%` }} /></div>
      </nav>

      <section className="overflow-hidden rounded-3xl border bg-card shadow-xl">
        <div className="border-b px-6 py-5 sm:px-9"><p className="text-xs font-semibold uppercase tracking-[0.18em] text-emerald-600">Adım {step + 1} / {STEPS.length}</p><h2 className="mt-1 text-2xl font-bold">{stepTitle(step)}</h2></div>
        <div className="min-h-[440px] space-y-7 p-6 sm:p-9">
          {step === 0 && <NameStep name={name} updateName={updateName} tagline={tagline} setTagline={setTagline} websiteUrl={websiteUrl} setWebsiteUrl={setWebsiteUrl} slug={slug} setSlug={(value) => { setSlugEdited(true); setSlug(slugify(value)); }} />}
          {step === 1 && <DescriptionStep description={description} setDescription={setDescription} />}
          {step === 2 && <ImagesStep thumbnailUrl={thumbnailUrl} galleryUrls={galleryUrls} isUploading={isUploading} upload={upload} removeThumbnail={() => setThumbnailUrl("")} removeGallery={(index) => setGalleryUrls((current) => current.filter((_, itemIndex) => itemIndex !== index))} />}
          {step === 3 && <TopicsStep topics={topics} toggleTopic={toggleTopic} categories={categories} categoryOptions={categoryOptions} toggleCategory={toggleCategory} />}
          {step === 4 && <LaunchStep versionLabel={launchVersionLabel} setVersionLabel={setLaunchVersionLabel} launchTagline={launchTagline} setLaunchTagline={setLaunchTagline} productTagline={tagline} scheduleMode={scheduleMode} setScheduleMode={setScheduleMode} scheduledLaunchAt={scheduledLaunchAt} setScheduledLaunchAt={setScheduledLaunchAt} />}
          {step === 5 && <ProductPreview name={name} tagline={tagline} description={description} websiteUrl={websiteUrl} slug={productPath} thumbnailUrl={thumbnailUrl} galleryUrls={galleryUrls} topics={topics} categories={categories.map(categorySlug => categoryOptions.find(item => item.slug === categorySlug)?.name ?? categorySlug)} launchVersionLabel={launchVersionLabel} launchTagline={launchTagline.trim() || tagline.trim()} scheduledLaunchAt={scheduleMode === "scheduled" ? scheduledLaunchAt : ""} />}
          {step === 6 && <SubmitReview name={name} slug={productPath} thumbnailUrl={thumbnailUrl} topics={topics} categories={categories} launchVersionLabel={launchVersionLabel} scheduledLaunchAt={scheduleMode === "scheduled" ? scheduledLaunchAt : ""} />}
          {error && <ErrorBox message={error} />}
        </div>
        <div className="flex flex-col-reverse gap-3 border-t bg-muted/20 px-6 py-5 sm:flex-row sm:items-center sm:justify-between sm:px-9">
          <button type="button" disabled={isSubmitting || isUploading} onClick={() => void submitProduct(true)} className="inline-flex items-center justify-center gap-2 text-sm font-semibold text-muted-foreground hover:text-foreground disabled:opacity-50"><Save className="h-4 w-4" /> Taslak kaydet</button>
          <div className="flex gap-3">
            {step > 0 && <Button type="button" variant="outline" onClick={() => { setError(""); setStep((current) => current - 1); }} className="h-11 rounded-xl"><ArrowLeft className="mr-2 h-4 w-4" /> Geri</Button>}
            {step < STEPS.length - 1 ? <Button type="button" onClick={goNext} disabled={isUploading} className="h-11 flex-1 rounded-xl bg-emerald-600 text-white hover:bg-emerald-700 sm:flex-none">Devam et <ArrowRight className="ml-2 h-4 w-4" /></Button>
              : <Button type="button" onClick={() => void submitProduct(false)} disabled={isSubmitting || isUploading} className="h-11 flex-1 rounded-xl bg-emerald-600 px-6 text-white hover:bg-emerald-700 sm:flex-none">{isSubmitting ? "Gönderiliyor..." : "İncelemeye gönder"} <Rocket className="ml-2 h-4 w-4" /></Button>}
          </div>
        </div>
      </section>
    </div>
  );
}

function NameStep({ name, updateName, tagline, setTagline, websiteUrl, setWebsiteUrl, slug, setSlug }: { name: string; updateName: (value: string) => void; tagline: string; setTagline: (value: string) => void; websiteUrl: string; setWebsiteUrl: (value: string) => void; slug: string; setSlug: (value: string) => void }) {
  return <>
    <div className="space-y-3"><FieldLabel icon={Type} label="Ürün adı" trailing={`${name.length}/40`} /><Input autoFocus maxLength={40} value={name} onChange={(event) => updateName(event.target.value)} placeholder="Örn. Vitrin" className="h-12 rounded-xl text-base" /></div>
    <div className="space-y-3"><FieldLabel icon={AlignLeft} label="Kısa açıklama" trailing={`${tagline.length}/80`} /><Input maxLength={80} value={tagline} onChange={(event) => setTagline(event.target.value)} placeholder="Ürünün ne yaptığını tek cümlede anlat" className="h-12 rounded-xl text-base" /></div>
    <div className="space-y-3"><FieldLabel icon={Globe2} label="Web sitesi" /><Input type="url" value={websiteUrl} onChange={(event) => setWebsiteUrl(event.target.value)} placeholder="https://urun.com" className="h-12 rounded-xl" /></div>
    <div className="space-y-3"><FieldLabel icon={LinkIcon} label="Vitrin URL'si" /><div className="flex h-12 items-center rounded-xl border bg-background px-3"><span className="whitespace-nowrap text-sm text-muted-foreground">vitrin.app/</span><input value={slug} onChange={(event) => setSlug(event.target.value)} className="min-w-0 flex-1 bg-transparent text-sm outline-none" aria-label="Ürün slug" /></div><p className="text-xs text-muted-foreground">Ürün adından otomatik oluşturulur; istersen değiştirebilirsin.</p></div>
  </>;
}

function DescriptionStep({ description, setDescription }: { description: string; setDescription: (value: string) => void }) {
  return <div className="space-y-3"><FieldLabel icon={AlignLeft} label="Ürün hikayesi" trailing={`${description.length}/4000`} /><Textarea autoFocus minLength={80} maxLength={4000} value={description} onChange={(event) => setDescription(event.target.value)} placeholder="Hangi problemi çözüyor? Kimler için? Diğer çözümlerden farkı ne?" className="min-h-72 resize-y rounded-2xl text-base leading-7" /><div className="grid gap-3 rounded-2xl bg-muted/40 p-4 text-sm text-muted-foreground sm:grid-cols-3"><span>• Problemi açıkla</span><span>• Çözümünü anlat</span><span>• Farkını göster</span></div></div>;
}

function ImagesStep({ thumbnailUrl, galleryUrls, isUploading, upload, removeThumbnail, removeGallery }: { thumbnailUrl: string; galleryUrls: string[]; isUploading: boolean; upload: (files: File[], kind: "thumbnail" | "gallery") => Promise<void>; removeThumbnail: () => void; removeGallery: (index: number) => void }) {
  return <>
    <div className="space-y-4"><FieldLabel icon={ImageIcon} label="Ürün logosu / thumbnail" trailing="Zorunlu" /><div className="grid gap-4 sm:grid-cols-[160px_1fr]">
      <UploadTile imageUrl={thumbnailUrl} label="Logo yükle" isUploading={isUploading} square onFiles={(files) => void upload(files, "thumbnail")} onRemove={removeThumbnail} />
      <div className="rounded-2xl bg-muted/40 p-5 text-sm leading-6 text-muted-foreground"><strong className="text-foreground">Cloudinary thumbnail</strong><br />Kare PNG, JPG veya WebP önerilir. Yüklenen görsel otomatik olarak 480×480 boyutunda kırpılır ve optimize edilir. En fazla 8 MB.</div>
    </div></div>
    <div className="space-y-4"><FieldLabel icon={Images} label="Ürün galerisi" trailing={`${galleryUrls.length}/5`} /><div className="grid grid-cols-2 gap-3 sm:grid-cols-3">{galleryUrls.map((url, index) => <UploadTile key={url} imageUrl={url} label={`Görsel ${index + 1}`} isUploading={false} onFiles={() => undefined} onRemove={() => removeGallery(index)} />)}{galleryUrls.length < 5 && <UploadTile label="Görsel ekle" isUploading={isUploading} multiple onFiles={(files) => void upload(files, "gallery")} />}</div></div>
  </>;
}

function UploadTile({ imageUrl, label, isUploading, square = false, multiple = false, onFiles, onRemove }: { imageUrl?: string; label: string; isUploading: boolean; square?: boolean; multiple?: boolean; onFiles: (files: File[]) => void; onRemove?: () => void }) {
  return <div className={`group relative overflow-hidden rounded-2xl border-2 border-dashed bg-background ${square ? "aspect-square" : "aspect-[4/3]"}`}>
    {imageUrl ? <><Image src={imageUrl} alt={label} fill sizes="240px" className="object-cover" />{onRemove && <button type="button" onClick={onRemove} aria-label={`${label} görselini kaldır`} className="absolute right-2 top-2 rounded-full bg-black/65 p-1.5 text-white"><X className="h-4 w-4" /></button>}</>
      : <label className="absolute inset-0 flex cursor-pointer flex-col items-center justify-center gap-2 text-sm font-semibold text-muted-foreground hover:text-emerald-600">{isUploading ? <LoaderCircle className="h-7 w-7 animate-spin" /> : <UploadCloud className="h-7 w-7" />}<span>{isUploading ? "Yükleniyor..." : label}</span><input type="file" accept="image/png,image/jpeg,image/webp,image/gif" multiple={multiple} disabled={isUploading} className="sr-only" onChange={(event) => onFiles(Array.from(event.target.files ?? []))} /></label>}
  </div>;
}

function TopicsStep({ topics, toggleTopic, categories, categoryOptions, toggleCategory }: { topics: string[]; toggleTopic: (topic: string) => void; categories: string[]; categoryOptions: ProductCategory[]; toggleCategory: (slug: string) => void }) {
  return <div className="space-y-8">
    <div className="space-y-4">
      <div><FieldLabel icon={Tag} label="Ürün kategorileri" trailing={`${categories.length}/3`} /><p className="mt-2 text-sm text-muted-foreground">Ürünün ne olduğunu veya çözdüğü temel problemi anlatan en fazla üç kontrollü kategori seç.</p></div>
      <div className="grid gap-2 sm:grid-cols-2">{categoryOptions.map((category) => { const selected = categories.includes(category.slug); return <button key={category.id} type="button" aria-pressed={selected} onClick={() => toggleCategory(category.slug)} className={`rounded-2xl border p-3 text-left transition ${selected ? "border-violet-600 bg-violet-500/10 text-violet-800 dark:text-violet-300" : "bg-background hover:border-violet-500/40"}`}><span className="flex items-center justify-between gap-2 font-semibold">{category.name}{selected && <Check className="h-4 w-4" />}</span>{category.description && <span className="mt-1 line-clamp-2 block text-xs leading-5 text-muted-foreground">{category.description}</span>}</button>; })}</div>
      {categories.length === 3 && <p className="text-xs text-muted-foreground">En fazla 3 kategori seçebilirsin.</p>}
    </div>
    <div className="space-y-4">
      <div><FieldLabel icon={Sparkles} label="İlgili topics" trailing={`${topics.length}/4`} /><p className="mt-2 text-sm text-muted-foreground">Teknoloji, özellik ve kullanım bağlamını anlatan 1-4 topic seç.</p></div>
      <div className="flex flex-wrap gap-3">{AVAILABLE_TOPICS.map((topic) => { const selected = topics.includes(topic); return <button key={topic} type="button" aria-pressed={selected} onClick={() => toggleTopic(topic)} className={`rounded-full border px-4 py-2.5 text-sm font-semibold transition ${selected ? "border-emerald-600 bg-emerald-600 text-white shadow-lg shadow-emerald-500/20" : "bg-background hover:border-emerald-500/50"}`}>{selected && <Check className="mr-1.5 inline h-4 w-4" />}{topic}</button>; })}</div>
      {topics.length === 4 && <p className="text-xs text-muted-foreground">En fazla 4 topic seçebilirsin.</p>}
    </div>
  </div>;
}

function LaunchStep({ versionLabel, setVersionLabel, launchTagline, setLaunchTagline, productTagline, scheduleMode, setScheduleMode, scheduledLaunchAt, setScheduledLaunchAt }: { versionLabel: string; setVersionLabel: (value: string) => void; launchTagline: string; setLaunchTagline: (value: string) => void; productTagline: string; scheduleMode: "asap" | "scheduled"; setScheduleMode: (value: "asap" | "scheduled") => void; scheduledLaunchAt: string; setScheduledLaunchAt: (value: string) => void }) {
  const [minimumLaunchAt] = useState(() => toLocalDateTimeInput(new Date(Date.now() + 10 * 60 * 1000)));

  return <div className="space-y-7">
    <div className="rounded-2xl border border-amber-500/25 bg-amber-500/5 p-5">
      <div className="flex items-start gap-3"><Rocket className="mt-0.5 h-5 w-5 text-amber-600" /><div><h3 className="font-semibold">Ürün ve lansman ayrı kaydedilir</h3><p className="mt-1 text-sm leading-6 text-muted-foreground">Ürün sayfası kalıcıdır. Buradaki sürüm, mesaj ve zaman yalnızca bu lansman dönemine aittir.</p></div></div>
    </div>
    <div className="space-y-3"><FieldLabel icon={Rocket} label="Lansman sürümü / adı" trailing={`${versionLabel.length}/80`} /><Input maxLength={80} value={versionLabel} onChange={(event) => setVersionLabel(event.target.value)} placeholder="İlk Lansman, v2.0, Mobil Uygulama..." className="h-12 rounded-xl" /><div className="flex flex-wrap gap-2">{["İlk Lansman", "v1.0", "Yeni Sürüm"].map((label) => <button key={label} type="button" onClick={() => setVersionLabel(label)} className="rounded-full border px-3 py-1.5 text-xs font-semibold text-muted-foreground transition hover:border-emerald-500 hover:text-emerald-700">{label}</button>)}</div></div>
    <div className="space-y-3"><FieldLabel icon={Sparkles} label="Lansman mesajı" trailing={`${(launchTagline || productTagline).length}/200`} /><Textarea maxLength={200} value={launchTagline} onChange={(event) => setLaunchTagline(event.target.value)} placeholder={productTagline || "Bu sürümde ne yeni?"} className="min-h-28 rounded-xl" /><p className="text-xs text-muted-foreground">Boş bırakırsan ürünün kısa açıklaması kullanılır.</p></div>
    <div className="space-y-3"><FieldLabel icon={CalendarClock} label="Tercih edilen lansman zamanı" /><div className="grid gap-3 sm:grid-cols-2"><button type="button" onClick={() => setScheduleMode("asap")} className={`rounded-2xl border p-4 text-left transition ${scheduleMode === "asap" ? "border-emerald-600 bg-emerald-500/10" : "bg-background"}`}><span className="font-semibold">Onaydan sonra yayınla</span><span className="mt-1 block text-xs leading-5 text-muted-foreground">Admin onayladığında aynı gün lansmana girer.</span></button><button type="button" onClick={() => setScheduleMode("scheduled")} className={`rounded-2xl border p-4 text-left transition ${scheduleMode === "scheduled" ? "border-emerald-600 bg-emerald-500/10" : "bg-background"}`}><span className="font-semibold">Tarih planla</span><span className="mt-1 block text-xs leading-5 text-muted-foreground">Onaylansa bile seçtiğin zamana kadar bekler.</span></button></div>{scheduleMode === "scheduled" && <Input type="datetime-local" min={minimumLaunchAt} value={scheduledLaunchAt} onChange={(event) => setScheduledLaunchAt(event.target.value)} className="h-12 rounded-xl" />}</div>
  </div>;
}

function ProductPreview({ name, tagline, description, websiteUrl, slug, thumbnailUrl, galleryUrls, topics, categories, launchVersionLabel, launchTagline, scheduledLaunchAt }: { name: string; tagline: string; description: string; websiteUrl: string; slug: string; thumbnailUrl: string; galleryUrls: string[]; topics: string[]; categories: string[]; launchVersionLabel: string; launchTagline: string; scheduledLaunchAt: string }) {
  return <div className="space-y-6"><div className="rounded-3xl border bg-background p-5 shadow-sm sm:p-6"><div className="flex gap-4"><div className="relative h-20 w-20 shrink-0 overflow-hidden rounded-2xl border bg-muted">{thumbnailUrl ? <Image src={thumbnailUrl} alt="Ürün logosu" fill sizes="80px" className="object-cover" /> : <ImageIcon className="absolute left-1/2 top-1/2 h-7 w-7 -translate-x-1/2 -translate-y-1/2 text-muted-foreground" />}</div><div className="min-w-0 flex-1"><h3 className="truncate text-xl font-bold">{name || "Ürün adı"}</h3><p className="mt-1 text-muted-foreground">{tagline || "Kısa açıklama"}</p><div className="mt-3 flex flex-wrap gap-2">{categories.map((category) => <span key={category} className="rounded-full bg-violet-500/10 px-2.5 py-1 text-xs font-semibold text-violet-700">{category}</span>)}{topics.map((topic) => <span key={topic} className="rounded-full bg-emerald-500/10 px-2.5 py-1 text-xs font-semibold text-emerald-700">{topic}</span>)}</div></div><div className="flex h-14 w-12 shrink-0 flex-col items-center justify-center rounded-xl border"><span className="text-xs text-muted-foreground">▲</span><strong>0</strong></div></div><div className="mt-5 border-t pt-4 text-xs text-muted-foreground">vitrin.app/{slug}</div></div>
    <div className="rounded-2xl border border-amber-500/25 bg-amber-500/5 p-5"><p className="text-xs font-bold uppercase tracking-[0.16em] text-amber-700">Lansman</p><h4 className="mt-2 text-lg font-bold">{launchVersionLabel}</h4><p className="mt-1 text-sm text-muted-foreground">{launchTagline}</p><p className="mt-3 text-xs font-medium text-muted-foreground">{scheduledLaunchAt ? new Date(scheduledLaunchAt).toLocaleString("tr-TR") : "Admin onayından sonra yayınlanacak"}</p></div>
    {galleryUrls.length > 0 && <div className="grid grid-cols-2 gap-3 sm:grid-cols-3">{galleryUrls.map((url, index) => <div key={url} className="relative aspect-video overflow-hidden rounded-2xl border"><Image src={url} alt={`Galeri ${index + 1}`} fill sizes="300px" className="object-cover" /></div>)}</div>}
    <div className="rounded-2xl border p-5"><h4 className="mb-3 font-semibold">Ürün hikayesi</h4><p className="whitespace-pre-wrap text-sm leading-7 text-muted-foreground">{description}</p>{websiteUrl && <a href={websiteUrl} target="_blank" rel="noreferrer" className="mt-4 inline-flex items-center text-sm font-semibold text-emerald-700"><Globe2 className="mr-2 h-4 w-4" /> Web sitesini aç</a>}</div>
  </div>;
}

function SubmitReview({ name, slug, thumbnailUrl, topics, categories, launchVersionLabel, scheduledLaunchAt }: { name: string; slug: string; thumbnailUrl: string; topics: string[]; categories: string[]; launchVersionLabel: string; scheduledLaunchAt: string }) {
  const checks = [["Ürün bilgileri", Boolean(name && slug)], ["Thumbnail", Boolean(thumbnailUrl)], ["Kategori", categories.length > 0], ["Topics", topics.length > 0], ["Lansman bilgileri", Boolean(launchVersionLabel)], [scheduledLaunchAt ? "Planlı lansman zamanı" : "Onay sonrası yayın", true]] as const;
  return <div className="space-y-6"><div className="rounded-3xl border border-emerald-500/25 bg-emerald-500/5 p-6"><Rocket className="mb-4 h-9 w-9 text-emerald-600" /><h3 className="text-2xl font-bold">Göndermeye hazırsın</h3><p className="mt-2 text-sm leading-6 text-muted-foreground">Ürünün yönetici incelemesine girecek. Yayınlanana kadar Ürünlerim sayfasından durumunu takip edebilirsin.</p></div><ul className="space-y-3">{checks.map(([label, ready]) => <li key={label} className="flex items-center justify-between rounded-2xl border px-4 py-3"><span className="font-medium">{label}</span><span className={`inline-flex items-center gap-1 text-sm font-semibold ${ready ? "text-emerald-600" : "text-destructive"}`}>{ready ? <><CheckCircle2 className="h-4 w-4" /> Hazır</> : "Eksik"}</span></li>)}</ul><p className="text-xs leading-5 text-muted-foreground">“İncelemeye gönder” dediğinde topluluk kurallarına ve içerik politikasına uygun bilgi verdiğini kabul etmiş olursun.</p></div>;
}

function FieldLabel({ icon: Icon, label, trailing }: { icon: typeof Type; label: string; trailing?: string }) { return <div className="flex items-center justify-between gap-3"><span className="inline-flex items-center gap-2 text-sm font-semibold"><Icon className="h-4 w-4 text-muted-foreground" />{label}</span>{trailing && <span className="text-xs text-muted-foreground">{trailing}</span>}</div>; }
function ErrorBox({ message }: { message: string }) { return <div role="alert" className="rounded-xl border border-destructive/20 bg-destructive/10 p-3 text-sm font-medium text-destructive">{message}</div>; }
function SuccessCard({ title, body }: { title: string; body: string }) { return <div className="mx-auto max-w-2xl rounded-3xl border border-emerald-500/25 bg-card p-10 text-center shadow-xl"><CheckCircle2 className="mx-auto mb-5 h-14 w-14 text-emerald-600" /><h2 className="text-3xl font-bold">{title}</h2><p className="mx-auto mt-3 max-w-lg leading-7 text-muted-foreground">{body}</p><Link href="/my-products" className="mt-6 inline-flex font-semibold text-emerald-700 hover:underline">Ürünlerimi görüntüle <ArrowRight className="ml-2 h-4 w-4" /></Link></div>; }
function stepTitle(step: number) { return ["Temel bilgileri gir", "Ürününü anlat", "Görsellerini yükle", "Kategori ve topics seç", "Lansmanı planla", "Son halini kontrol et", "İncelemeye gönder"][step]; }
function isHttpUrl(value: string) { try { const url = new URL(value); return url.protocol === "http:" || url.protocol === "https:"; } catch { return false; } }
function cloudinaryTransform(url: string, transformation: string) { return url.includes("/upload/") ? url.replace("/upload/", `/upload/${transformation}/`) : url; }
function slugify(value: string) { return value.trim().toLocaleLowerCase("tr-TR").replaceAll("ı", "i").replaceAll("ğ", "g").replaceAll("ü", "u").replaceAll("ş", "s").replaceAll("ö", "o").replaceAll("ç", "c").normalize("NFD").replace(/[\u0300-\u036f]/g, "").replace(/[^a-z0-9]+/g, "-").replace(/^-+|-+$/g, "").slice(0, 80); }
function toLocalDateTimeInput(value: Date) { const offset = value.getTimezoneOffset() * 60_000; return new Date(value.getTime() - offset).toISOString().slice(0, 16); }
