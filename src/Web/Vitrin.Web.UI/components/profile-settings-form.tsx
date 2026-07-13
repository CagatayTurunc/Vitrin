"use client";

import { useState, useEffect } from "react";
import { useSession } from "next-auth/react";
import { useRouter } from "next/navigation";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Label } from "@/components/ui/label";
import { useToast } from "@/components/ui/use-toast";
import { Loader2, CheckCircle2, UserCircle2, ShieldCheck, Globe, User, Search, Bell } from "lucide-react";
import Image from "next/image";

interface ProfileSettingsFormProps {
  initialData: {
    id: string;
    username: string;
    fullName: string;
    avatarUrl?: string;
    headline?: string;
    about?: string;
    websiteUrl?: string;
    githubUrl?: string;
    linkedInUrl?: string;
    role: number;
    createdAt: string;
  };
}

type TabType = "settings" | "details" | "followedProducts" | "followers" | "following" | "verification";

export function ProfileSettingsForm({ initialData }: ProfileSettingsFormProps) {
  const { data: session, update } = useSession();
  const router = useRouter();
  const { toast } = useToast();

  const [activeTab, setActiveTab] = useState<TabType>("followedProducts");
  const [isLoading, setIsLoading] = useState(false);
  const [followers, setFollowers] = useState<any[]>([]);
  const [following, setFollowing] = useState<any[]>([]);
  const [isFollowDataLoading, setIsFollowDataLoading] = useState(false);

  useEffect(() => {
    if (activeTab === "followers") {
      setIsFollowDataLoading(true);
      fetch(process.env.NEXT_PUBLIC_API_URL + `/api/auth/users/${initialData.username}/followers`)
        .then(res => res.json())
        .then(data => setFollowers(data || []))
        .catch(err => console.error(err))
        .finally(() => setIsFollowDataLoading(false));
    } else if (activeTab === "following") {
      setIsFollowDataLoading(true);
      fetch(process.env.NEXT_PUBLIC_API_URL + `/api/auth/users/${initialData.username}/following`)
        .then(res => res.json())
        .then(data => setFollowing(data || []))
        .catch(err => console.error(err))
        .finally(() => setIsFollowDataLoading(false));
    }
  }, [activeTab, initialData.username]);

  const [formData, setFormData] = useState({
    fullName: initialData.fullName || "",
    username: initialData.username || "",
    headline: initialData.headline || "",
    about: initialData.about || "",
    avatarUrl: initialData.avatarUrl || "",
    websiteUrl: initialData.websiteUrl || "",
    githubUrl: initialData.githubUrl || "",
    linkedInUrl: initialData.linkedInUrl || "",
  });

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    setFormData({ ...formData, [e.target.name]: e.target.value });
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);

    try {
      const res = await fetch(process.env.NEXT_PUBLIC_API_URL + "/api/auth/users/me", {
        method: "PUT",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${(session as any)?.accessToken}`,
        },
        body: JSON.stringify(formData),
      });

      if (res.ok) {
        toast({
          title: "Başarılı",
          description: "Profiliniz başarıyla güncellendi.",
        });
        
        if (formData.username !== initialData.username || formData.fullName !== initialData.fullName) {
          await update({
            ...session,
            user: {
              ...session?.user,
              name: formData.fullName,
              username: formData.username
            }
          });
        }
        
        router.refresh();
      } else {
        const errorText = await res.text();
        toast({
          title: "Hata",
          description: errorText || "Profil güncellenirken bir hata oluştu.",
          variant: "destructive",
        });
      }
    } catch (error: any) {
      toast({
        title: "Bağlantı Hatası",
        description: error.message,
        variant: "destructive",
      });
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="w-full">
      {/* Horizontal Tabs */}
      <div className="flex items-center gap-6 border-b border-border/40 overflow-x-auto pb-[-1px] mb-8 no-scrollbar">
        <button
          onClick={() => setActiveTab("settings")}
          className={`pb-3 text-sm font-semibold transition-colors whitespace-nowrap relative ${
            activeTab === "settings"
              ? "text-foreground"
              : "text-muted-foreground hover:text-foreground"
          }`}
        >
          Ayarlar
          {activeTab === "settings" && (
            <span className="absolute bottom-0 left-0 w-full h-[2px] bg-green-500 rounded-t-full" />
          )}
        </button>
        <button
          onClick={() => setActiveTab("details")}
          className={`pb-3 text-sm font-semibold transition-colors whitespace-nowrap relative ${
            activeTab === "details"
              ? "text-foreground"
              : "text-muted-foreground hover:text-foreground"
          }`}
        >
          Hesap Detayları
          {activeTab === "details" && (
            <span className="absolute bottom-0 left-0 w-full h-[2px] bg-green-500 rounded-t-full" />
          )}
        </button>
        <button
          onClick={() => setActiveTab("followedProducts")}
          className={`pb-3 text-sm font-semibold transition-colors whitespace-nowrap relative ${
            activeTab === "followedProducts"
              ? "text-foreground"
              : "text-muted-foreground hover:text-foreground"
          }`}
        >
          Takip Edilen Ürünler
          {activeTab === "followedProducts" && (
            <span className="absolute bottom-0 left-0 w-full h-[2px] bg-green-500 rounded-t-full" />
          )}
        </button>
        <button
          onClick={() => setActiveTab("followers")}
          className={`pb-3 text-sm font-semibold transition-colors whitespace-nowrap relative ${
            activeTab === "followers"
              ? "text-foreground"
              : "text-muted-foreground hover:text-foreground"
          }`}
        >
          Takipçiler
          {activeTab === "followers" && (
            <span className="absolute bottom-0 left-0 w-full h-[2px] bg-green-500 rounded-t-full" />
          )}
        </button>
        <button
          onClick={() => setActiveTab("following")}
          className={`pb-3 text-sm font-semibold transition-colors whitespace-nowrap relative ${
            activeTab === "following"
              ? "text-foreground"
              : "text-muted-foreground hover:text-foreground"
          }`}
        >
          Takip Edilenler
          {activeTab === "following" && (
            <span className="absolute bottom-0 left-0 w-full h-[2px] bg-green-500 rounded-t-full" />
          )}
        </button>
        <button
          onClick={() => setActiveTab("verification")}
          className={`pb-3 text-sm font-semibold transition-colors whitespace-nowrap relative ${
            activeTab === "verification"
              ? "text-foreground"
              : "text-muted-foreground hover:text-foreground"
          }`}
        >
          Doğrulama
          {activeTab === "verification" && (
            <span className="absolute bottom-0 left-0 w-full h-[2px] bg-green-500 rounded-t-full" />
          )}
        </button>
      </div>

      {/* Content */}
      <div className="w-full animate-in fade-in slide-in-from-bottom-2 duration-300">
        
        {/* Ayarlar Placeholder */}
        {activeTab === "settings" && (
          <div className="space-y-8 max-w-3xl animate-in fade-in slide-in-from-bottom-2 duration-300">
            <div>
              <h2 className="text-[28px] font-bold tracking-tight">Genel Ayarlar</h2>
              <p className="text-[15px] text-muted-foreground mt-2">
                Uygulama tercihlerinizi, şifrenizi ve bildirim ayarlarınızı yönetin.
              </p>
            </div>

            {/* Password Change */}
            <div className="space-y-4 pt-4">
              <h3 className="font-semibold text-lg tracking-tight border-b border-border/40 pb-2">Şifre Değiştir</h3>
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 max-w-xl">
                <div className="space-y-2 sm:col-span-2">
                  <Label htmlFor="currentPassword" className="text-sm">Mevcut Şifre</Label>
                  <Input id="currentPassword" type="password" placeholder="••••••••" className="bg-background" />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="newPassword" className="text-sm">Yeni Şifre</Label>
                  <Input id="newPassword" type="password" placeholder="••••••••" className="bg-background" />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="confirmPassword" className="text-sm">Yeni Şifre (Tekrar)</Label>
                  <Input id="confirmPassword" type="password" placeholder="••••••••" className="bg-background" />
                </div>
              </div>
              <Button variant="outline" className="mt-4 font-medium px-6 rounded-full">Şifreyi Güncelle</Button>
            </div>

            {/* Notifications */}
            <div className="space-y-4 pt-6">
              <h3 className="font-semibold text-lg tracking-tight border-b border-border/40 pb-2">Bildirim Tercihleri</h3>
              
              <div className="flex items-center justify-between gap-4 p-5 border border-border/50 rounded-2xl bg-muted/20 hover:bg-muted/40 transition-colors">
                <div className="space-y-1">
                  <Label className="text-base cursor-pointer">Ürün Güncellemeleri</Label>
                  <p className="text-sm text-muted-foreground">Takip ettiğiniz ürünlerle ilgili önemli gelişmeleri e-posta ile alın.</p>
                </div>
                {/* Mock Switch Active */}
                <div className="relative inline-flex h-6 w-11 shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors duration-200 ease-in-out focus:outline-none bg-green-500">
                  <span className="pointer-events-none inline-block h-5 w-5 translate-x-5 transform rounded-full bg-white shadow ring-0 transition duration-200 ease-in-out" />
                </div>
              </div>

              <div className="flex items-center justify-between gap-4 p-5 border border-border/50 rounded-2xl bg-muted/20 hover:bg-muted/40 transition-colors">
                <div className="space-y-1">
                  <Label className="text-base cursor-pointer">Haftalık Bülten</Label>
                  <p className="text-sm text-muted-foreground">Haftanın en iyi ürünlerinin derlendiği bültenimizi alın.</p>
                </div>
                {/* Mock Switch Inactive */}
                <div className="relative inline-flex h-6 w-11 shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors duration-200 ease-in-out focus:outline-none bg-muted-foreground/30">
                  <span className="pointer-events-none inline-block h-5 w-5 translate-x-0 transform rounded-full bg-white shadow ring-0 transition duration-200 ease-in-out" />
                </div>
              </div>
            </div>

            {/* Danger Zone */}
            <div className="space-y-4 pt-6">
              <h3 className="font-semibold text-lg tracking-tight text-red-500 border-b border-border/40 pb-2">Tehlikeli Bölge</h3>
              <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4 p-5 border border-red-500/30 rounded-2xl bg-red-500/5 hover:bg-red-500/10 transition-colors">
                <div className="space-y-1">
                  <h4 className="font-semibold text-red-600 dark:text-red-400">Hesabı Kalıcı Olarak Sil</h4>
                  <p className="text-sm text-muted-foreground">Hesabınızı ve tüm verilerinizi kalıcı olarak siler. Bu işlem geri alınamaz.</p>
                </div>
                <Button variant="destructive" className="shrink-0 rounded-full font-medium px-6">Hesabımı Sil</Button>
              </div>
            </div>
            
          </div>
        )}

        {/* Hesap Detayları */}
        {activeTab === "details" && (
          <form onSubmit={handleSubmit} className="space-y-8 max-w-3xl">
            <h1 className="text-2xl font-bold tracking-tight mb-6">Hesap Detayları</h1>

            <div className="flex flex-col sm:flex-row gap-6 items-start sm:items-center">
              <div className="h-24 w-24 rounded-full bg-muted flex items-center justify-center overflow-hidden border shrink-0">
                {formData.avatarUrl ? (
                  <Image src={formData.avatarUrl} alt="Avatar" width={96} height={96} className="object-cover h-full w-full" />
                ) : (
                  <UserCircle2 className="h-12 w-12 text-muted-foreground" />
                )}
              </div>
              <div className="space-y-2 flex-1 w-full max-w-md">
                <Label htmlFor="avatarUrl" className="font-semibold text-sm">Profil Fotoğrafı URL</Label>
                <Input
                  id="avatarUrl"
                  name="avatarUrl"
                  value={formData.avatarUrl}
                  onChange={handleChange}
                  placeholder="https://example.com/avatar.jpg"
                />
                <p className="text-xs text-muted-foreground">Profil fotoğrafınızın URL'sini girin (Şimdilik lokal yükleme aktif değil).</p>
              </div>
            </div>

            <div className="space-y-6 pt-4">
              <div className="space-y-2 max-w-md">
                <Label htmlFor="fullName" className="font-semibold text-sm">Ad Soyad</Label>
                <Input
                  id="fullName"
                  name="fullName"
                  value={formData.fullName}
                  onChange={handleChange}
                  required
                />
              </div>

              <div className="space-y-2 max-w-md">
                <Label htmlFor="username" className="font-semibold text-sm">Kullanıcı Adı</Label>
                <Input
                  id="username"
                  name="username"
                  value={formData.username}
                  onChange={handleChange}
                  required
                />
              </div>

              <div className="space-y-2 max-w-xl">
                <Label htmlFor="headline" className="font-semibold text-sm">Kısa Başlık</Label>
                <Input
                  id="headline"
                  name="headline"
                  value={formData.headline}
                  onChange={handleChange}
                  placeholder="Kendinizi anlatan kısa bir başlık"
                />
              </div>

              <div className="space-y-2 max-w-xl">
                <Label htmlFor="about" className="font-semibold text-sm">Hakkında</Label>
                <Textarea
                  id="about"
                  name="about"
                  value={formData.about}
                  onChange={handleChange}
                  placeholder="Topluluğa kendinizden, hedeflerinizden ve tutkularınızdan bahsedin."
                  rows={4}
                  className="resize-none"
                />
              </div>
            </div>

            <div className="space-y-4 pt-6 border-t border-border/40 max-w-xl">
              <h3 className="font-semibold text-base">Sosyal Bağlantılar</h3>
              <div className="space-y-2">
                <Label htmlFor="websiteUrl" className="flex items-center gap-2 text-sm"><Globe className="w-4 h-4 text-muted-foreground" /> Kişisel Web Sitesi</Label>
                <Input id="websiteUrl" name="websiteUrl" value={formData.websiteUrl} onChange={handleChange} placeholder="https://..." />
              </div>
            </div>

            <div className="pt-4 pb-12">
              <Button type="submit" disabled={isLoading} className="px-6 rounded-full font-medium shadow-sm hover:shadow-md transition-shadow">
                {isLoading ? <Loader2 className="w-4 h-4 animate-spin mr-2" /> : null}
                Değişiklikleri Kaydet
              </Button>
            </div>
          </form>
        )}

        {/* Takip Edilen Ürünler */}
        {activeTab === "followedProducts" && (
          <div className="w-full">
            <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-8">
              <h2 className="text-[26px] font-bold tracking-tight">Takip Edilen Ürünler</h2>
              <div className="relative max-w-sm w-full sm:w-[300px]">
                <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground" />
                <Input placeholder="Search" className="pl-9 bg-transparent border-border/60 focus-visible:ring-1 focus-visible:ring-border h-10" />
              </div>
            </div>

            <div className="flex flex-col space-y-6">
              {/* Sample Product Row */}
              <div className="flex items-center justify-between gap-4 py-2 border-b border-border/20 pb-6">
                <div className="flex items-center gap-4 flex-1">
                  <div className="w-14 h-14 rounded-full border border-border/40 bg-muted flex items-center justify-center shrink-0">
                    <span className="text-xl font-bold text-muted-foreground">P</span>
                  </div>
                  <div className="flex-1">
                    <h3 className="text-[17px] font-bold leading-tight flex items-center flex-wrap gap-1">
                      Perfai Security <span className="text-muted-foreground font-normal mx-1">—</span> <span className="text-muted-foreground font-normal text-[15px]">Find & fix live vulnerabilities in Vibe Apps with 1-prompt.</span>
                    </h3>
                  </div>
                </div>
                <div className="flex items-center gap-4 shrink-0">
                  <button className="text-muted-foreground hover:text-foreground transition-colors p-2 rounded-full hover:bg-muted/50">
                    <Bell className="w-5 h-5" />
                  </button>
                  <Button variant="default" className="bg-green-500 hover:bg-green-600 text-white rounded-full font-semibold px-5 h-10">
                    Following
                  </Button>
                </div>
              </div>

              {/* Sample Product Row 2 */}
              <div className="flex items-center justify-between gap-4 py-2 border-b border-border/20 pb-6">
                <div className="flex items-center gap-4 flex-1">
                  <div className="w-14 h-14 rounded-full border border-border/40 bg-muted flex items-center justify-center shrink-0">
                    <span className="text-xl font-bold text-muted-foreground">V</span>
                  </div>
                  <div className="flex-1">
                    <h3 className="text-[17px] font-bold leading-tight flex items-center flex-wrap gap-1">
                      Vitrin App <span className="text-muted-foreground font-normal mx-1">—</span> <span className="text-muted-foreground font-normal text-[15px]">The best way to showcase and discover new products daily.</span>
                    </h3>
                  </div>
                </div>
                <div className="flex items-center gap-4 shrink-0">
                  <button className="text-muted-foreground hover:text-foreground transition-colors p-2 rounded-full hover:bg-muted/50">
                    <Bell className="w-5 h-5" />
                  </button>
                  <Button variant="default" className="bg-green-500 hover:bg-green-600 text-white rounded-full font-semibold px-5 h-10">
                    Following
                  </Button>
                </div>
              </div>
            </div>
          </div>
        )}

        {/* Takipçiler */}
        {activeTab === "followers" && (
          <div className="w-full">
            <h2 className="text-[26px] font-bold tracking-tight mb-8">Takipçileriniz</h2>
            
            {isFollowDataLoading ? (
              <div className="py-12 flex justify-center"><Loader2 className="w-8 h-8 animate-spin text-muted-foreground" /></div>
            ) : followers.length === 0 ? (
              <div className="text-center py-16 px-4 bg-muted/30 rounded-3xl border border-border border-dashed">
                <h3 className="text-lg font-bold text-foreground mb-2">Henüz takipçiniz yok</h3>
                <p className="text-muted-foreground">İnsanlar sizi takip ettikçe burada görünecekler.</p>
              </div>
            ) : (
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                {followers.map(f => (
                  <div key={f.id} className="flex items-center gap-4 p-4 border border-border/40 rounded-2xl bg-card hover:bg-muted/10 transition-colors cursor-pointer" onClick={() => router.push(`/profile/${f.username}`)}>
                    <div className="h-12 w-12 rounded-full bg-muted flex items-center justify-center overflow-hidden shrink-0">
                      {f.avatarUrl ? <img src={f.avatarUrl} alt={f.username} className="h-full w-full object-cover" /> : <UserCircle2 className="w-6 h-6 text-muted-foreground" />}
                    </div>
                    <div className="flex-1 overflow-hidden">
                      <h4 className="font-semibold text-[15px] truncate">{f.fullName || f.username}</h4>
                      <p className="text-sm text-muted-foreground truncate">@{f.username}</p>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        )}

        {/* Takip Edilenler */}
        {activeTab === "following" && (
          <div className="w-full">
            <h2 className="text-[26px] font-bold tracking-tight mb-8">Takip Ettikleriniz</h2>
            
            {isFollowDataLoading ? (
              <div className="py-12 flex justify-center"><Loader2 className="w-8 h-8 animate-spin text-muted-foreground" /></div>
            ) : following.length === 0 ? (
              <div className="text-center py-16 px-4 bg-muted/30 rounded-3xl border border-border border-dashed">
                <h3 className="text-lg font-bold text-foreground mb-2">Henüz kimseyi takip etmiyorsunuz</h3>
                <p className="text-muted-foreground">Toplulukta ilginizi çeken kişileri takip ederek başlayın.</p>
              </div>
            ) : (
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                {following.map(f => (
                  <div key={f.id} className="flex items-center gap-4 p-4 border border-border/40 rounded-2xl bg-card hover:bg-muted/10 transition-colors cursor-pointer" onClick={() => router.push(`/profile/${f.username}`)}>
                    <div className="h-12 w-12 rounded-full bg-muted flex items-center justify-center overflow-hidden shrink-0">
                      {f.avatarUrl ? <img src={f.avatarUrl} alt={f.username} className="h-full w-full object-cover" /> : <UserCircle2 className="w-6 h-6 text-muted-foreground" />}
                    </div>
                    <div className="flex-1 overflow-hidden">
                      <h4 className="font-semibold text-[15px] truncate">{f.fullName || f.username}</h4>
                      <p className="text-sm text-muted-foreground truncate">@{f.username}</p>
                    </div>
                    <Button variant="outline" className="shrink-0 rounded-full h-8 text-xs font-semibold px-4" onClick={(e) => { e.stopPropagation(); /* handle unfollow here optionally */ }}>Takip ediliyor</Button>
                  </div>
                ))}
              </div>
            )}
          </div>
        )}

        {/* Doğrulama */}
        {activeTab === "verification" && (
          <form onSubmit={handleSubmit} className="space-y-8 animate-in fade-in slide-in-from-bottom-2 duration-300">
            <div className="border-b border-border/50 pb-6">
              <h2 className="text-2xl font-bold tracking-tight flex items-center gap-2">
                Hesabınızı Doğrulayın <CheckCircle2 className="w-6 h-6 text-green-500 fill-green-500/20" />
              </h2>
              <div className="text-sm text-muted-foreground mt-3 space-y-2">
                <p>
                  Hesabınızı doğrulamak, topluluğun güvenilirliğini ve özgünlüğünü korumamıza yardımcı olur. Katkılarınız (ör. oylar ve yorumlar) daha fazla ağırlık ve görünürlük taşıyacaktır.
                </p>
                <p>
                  Doğrulanmış kullanıcılarımızın topluluğun ilgi çekici ve saygıdeğer üyeleri olmasını istiyoruz.
                </p>
              </div>
            </div>

            <div className="space-y-6">
              <div className="p-5 border border-border/50 rounded-2xl bg-muted/20 hover:bg-muted/40 transition-colors space-y-4">
                <div className="flex items-start gap-4">
                  <div className="mt-1 p-2 bg-primary/10 rounded-full shrink-0">
                    <ShieldCheck className="w-5 h-5 text-primary" />
                  </div>
                  <div className="space-y-1 w-full">
                    <h4 className="font-semibold">E-postanızı Doğrulayın</h4>
                    <p className="text-sm text-muted-foreground">E-posta adresinizi gönderin. (Önerilen)</p>
                    <div className="flex flex-col sm:flex-row gap-3 pt-3">
                      <Input disabled value={initialData.id ? "OAuth ile doğrulandı" : ""} placeholder="adiniz@ornek.com" className="bg-background max-w-sm" />
                      <Button disabled variant="outline" className="w-full sm:w-auto font-medium">Ekle</Button>
                    </div>
                  </div>
                </div>
              </div>

              <div className="p-5 border border-border/50 rounded-2xl bg-muted/20 hover:bg-muted/40 transition-colors space-y-4">
                <div className="flex items-start gap-4">
                  <div className="mt-1 p-2 bg-foreground/5 rounded-full shrink-0">
                    <svg
                      xmlns="http://www.w3.org/2000/svg"
                      width="20"
                      height="20"
                      viewBox="0 0 24 24"
                      fill="none"
                      stroke="currentColor"
                      strokeWidth="2"
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      className="text-foreground"
                    >
                      <path d="M15 22v-4a4.8 4.8 0 0 0-1-3.5c3 0 6-2 6-5.5.08-1.25-.27-2.48-1-3.5.28-1.15.28-2.35 0-3.5 0 0-1 0-3 1.5-2.64-.5-5.36-.5-8 0C6 2 5 2 5 2c-.3 1.15-.3 2.35 0 3.5A5.403 5.403 0 0 0 4 9c0 3.5 3 5.5 6 5.5-.39.49-.68 1.05-.85 1.65-.17.6-.22 1.23-.15 1.85v4" />
                      <path d="M9 18c-4.51 2-5-2-7-2" />
                    </svg>
                  </div>
                  <div className="space-y-1 w-full">
                    <h4 className="font-semibold">GitHub Profili</h4>
                    <p className="text-sm text-muted-foreground mb-4">GitHub profilinizle giriş yapın. (Önerilen)</p>
                    <Label htmlFor="githubUrl" className="sr-only">GitHub Kullanıcı Adı/URL</Label>
                    <div className="flex gap-2 max-w-sm">
                      <Input id="githubUrl" name="githubUrl" value={formData.githubUrl} onChange={handleChange} placeholder="örn. cagatayturunc" className="bg-background" />
                    </div>
                  </div>
                </div>
              </div>

              <div className="p-5 border border-border/50 rounded-2xl bg-muted/20 hover:bg-muted/40 transition-colors space-y-4">
                <div className="flex items-start gap-4">
                  <div className="mt-1 p-2 bg-blue-500/10 rounded-full shrink-0">
                    <svg
                      xmlns="http://www.w3.org/2000/svg"
                      width="20"
                      height="20"
                      viewBox="0 0 24 24"
                      fill="none"
                      stroke="currentColor"
                      strokeWidth="2"
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      className="text-blue-500"
                    >
                      <path d="M16 8a6 6 0 0 1 6 6v7h-4v-7a2 2 0 0 0-2-2 2 2 0 0 0-2 2v7h-4v-7a6 6 0 0 1 6-6z" />
                      <rect width="4" height="12" x="2" y="9" />
                      <circle cx="4" cy="4" r="2" />
                    </svg>
                  </div>
                  <div className="space-y-1 w-full">
                    <h4 className="font-semibold">LinkedIn Profili</h4>
                    <p className="text-sm text-muted-foreground mb-4">LinkedIn profilinizin URL'sini gönderin. (Önerilen)</p>
                    <Label htmlFor="linkedInUrl" className="sr-only">LinkedIn Kullanıcı Adı/URL</Label>
                    <div className="flex gap-2 max-w-sm">
                      <Input id="linkedInUrl" name="linkedInUrl" value={formData.linkedInUrl} onChange={handleChange} placeholder="örn. in/cagatayturunc" className="bg-background" />
                    </div>
                  </div>
                </div>
              </div>
            </div>

            <div className="pt-6 flex justify-end">
              <Button type="submit" disabled={isLoading} className="px-8 py-6 rounded-full font-semibold shadow-md hover:shadow-lg transition-all duration-300">
                {isLoading ? <Loader2 className="w-5 h-5 animate-spin mr-2" /> : null}
                Değişiklikleri Kaydet
              </Button>
            </div>
          </form>
        )}
      </div>
    </div>
  );
}

