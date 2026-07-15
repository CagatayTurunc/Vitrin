"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { ProductRow } from "@/components/product-row";
import { Product, ProductApiModel } from "@/core/domain/product.types";
import { User, Calendar, Loader2, Package, Shield, ShieldAlert, Edit, Link as LinkIcon, Globe } from "lucide-react";
import Image from "next/image";
import Link from "next/link";
import { useSession } from "next-auth/react";
import { FollowersModal } from "@/components/followers-modal";
import type { UserProfile } from "@/core/domain/user.types";

export default function ProfilePage() {
  const params = useParams();
  const username = params.username as string;

  const { data: session } = useSession();
  const [user, setUser] = useState<UserProfile | null>(null);
  const [products, setProducts] = useState<Product[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isFollowersModalOpen, setIsFollowersModalOpen] = useState(false);
  const [isFollowingModalOpen, setIsFollowingModalOpen] = useState(false);

  const fetchUserProfile = async (uname: string) => {
    setIsLoading(true);
    try {
      // 1. Kullanıcı bilgilerini çek
      const userRes = await fetch(process.env.NEXT_PUBLIC_API_URL + `/api/auth/users/by-username/${uname}`);
      if (!userRes.ok) {
        if (userRes.status === 404) {
          setError("Kullanıcı bulunamadı.");
        } else {
          setError("Bir hata oluştu.");
        }
        setIsLoading(false);
        return;
      }

      const userData = await userRes.json() as UserProfile;
      setUser(userData);

      // 2. Kullanıcının paylaştığı ürünleri çek
      if (userData && userData.id) {
        const prodRes = await fetch(process.env.NEXT_PUBLIC_API_URL + `/api/products/maker/${userData.id}`);
        if (prodRes.ok) {
          const prodData = await prodRes.json() as ProductApiModel[];
          const mappedProducts: Product[] = prodData.map((p: ProductApiModel, index: number) => ({
            id: p.id,
            rank: index + 1,
            name: p.name,
            slug: p.slug,
            description: p.tagline || p.description,
            publishedAt: p.publishedAt,
            image: p.thumbnailUrl || '/products/notai.png',
            topics: p.topics || [],
            votes: p.upvotes || 0,
          }));
          setProducts(mappedProducts);
        }
      }
    } catch (err) {
      console.error(err);
      setError("Bağlantı hatası oluştu.");
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    // Client-side profile data is synchronized after the network request resolves.
    // eslint-disable-next-line react-hooks/set-state-in-effect
    if (username) void fetchUserProfile(username);
  }, [username]);

  if (isLoading) {
    return (
      <div className="flex min-h-[60vh] flex-col items-center justify-center text-muted-foreground">
        <Loader2 className="h-10 w-10 animate-spin mb-4 text-primary" />
        <p>Profil yükleniyor...</p>
      </div>
    );
  }

  if (error || !user) {
    return (
      <div className="flex min-h-[60vh] flex-col items-center justify-center text-center">
        <User className="h-16 w-16 mb-4 opacity-20" />
        <h1 className="text-3xl font-bold tracking-tight text-foreground mb-2">Eyvah!</h1>
        <p className="text-muted-foreground">{error || "Kullanıcı bulunamadı."}</p>
      </div>
    );
  }

  const getRoleBadge = (role: number | string) => {
    const normalizedRole = typeof role === "string"
      ? role.toLowerCase()
      : role;

    switch (normalizedRole) {
      case "maker":
      case 1:
        return (
          <span className="inline-flex items-center gap-1.5 rounded-full bg-purple-100 px-3 py-1 text-sm font-semibold text-purple-700 dark:bg-purple-900/30 dark:text-purple-400">
            <Shield className="h-4 w-4" /> Maker
          </span>
        );
      case "admin":
      case 2:
        return (
          <span className="inline-flex items-center gap-1.5 rounded-full bg-red-100 px-3 py-1 text-sm font-semibold text-red-700 dark:bg-red-900/30 dark:text-red-400">
            <ShieldAlert className="h-4 w-4" /> Admin
          </span>
        );
      default:
        return (
          <span className="inline-flex items-center gap-1.5 rounded-full bg-blue-100 px-3 py-1 text-sm font-semibold text-blue-700 dark:bg-blue-900/30 dark:text-blue-400">
            <User className="h-4 w-4" /> Üye
          </span>
        );
    }
  };

  const handleFollowToggle = async () => {
    if (!session?.accessToken) return;
    try {
      const method = user.isFollowing ? "DELETE" : "POST";
      const res = await fetch(process.env.NEXT_PUBLIC_API_URL + `/api/auth/users/${user.username}/follow`, {
        method,
        headers: {
          "Authorization": `Bearer ${session.accessToken}`
        }
      });
      
      if (res.ok) {
        setUser((current) => current ? {
          ...current,
          isFollowing: !current.isFollowing,
          followerCount: current.isFollowing
            ? Math.max(0, current.followerCount - 1)
            : current.followerCount + 1,
        } : current);
      }
    } catch (err) {
      console.error(err);
    }
  };

  return (
    <main className="mx-auto w-full max-w-4xl px-4 py-8 sm:px-6 sm:py-12 min-h-screen">
      {/* Profil Başlığı */}
      <div className="rounded-3xl border border-border bg-card p-6 shadow-sm sm:p-10 mb-10 flex flex-col sm:flex-row items-center sm:items-start gap-6 text-center sm:text-left relative overflow-hidden">
        {/* Dekoratif Arka Plan */}
        <div className="absolute -right-20 -top-20 h-64 w-64 rounded-full bg-primary/5 blur-3xl" />
        
        <div className="relative flex h-28 w-28 shrink-0 items-center justify-center rounded-full bg-gradient-to-br from-primary to-primary/60 text-4xl font-bold text-white shadow-lg ring-4 ring-background overflow-hidden">
          {user.avatarUrl ? (
            <Image src={user.avatarUrl} alt={user.username} width={112} height={112} className="object-cover w-full h-full" />
          ) : (
            user.fullName?.[0]?.toUpperCase() || user.username?.[0]?.toUpperCase()
          )}
        </div>
        
        <div className="relative flex-1 space-y-4">
          <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
            <div>
              <h1 className="text-3xl font-extrabold tracking-tight text-foreground sm:text-4xl">
                {user.fullName || "İsimsiz Kullanıcı"}
              </h1>
              <p className="text-lg text-muted-foreground mt-1 font-medium flex items-center gap-2 justify-center sm:justify-start">
                @{user.username}
                {user.headline && <span className="hidden sm:inline-block w-1.5 h-1.5 rounded-full bg-muted-foreground/30"></span>}
                {user.headline && <span className="text-sm">{user.headline}</span>}
              </p>
            </div>
            
            {session?.user?.username === user.username ? (
              <Link href="/settings" className="inline-flex items-center gap-2 rounded-full border bg-background px-4 py-2 text-sm font-medium hover:bg-muted transition-colors whitespace-nowrap">
                <Edit className="w-4 h-4" /> Profili Düzenle
              </Link>
            ) : session?.user ? (
              <button 
                onClick={handleFollowToggle}
                className={`inline-flex items-center gap-2 rounded-full px-5 py-2 text-sm font-semibold transition-colors whitespace-nowrap ${
                  user.isFollowing 
                    ? "bg-muted text-foreground hover:bg-red-500/10 hover:text-red-500 border border-transparent hover:border-red-500/20" 
                    : "bg-green-500 text-white hover:bg-green-600 shadow-sm"
                }`}
              >
                {user.isFollowing ? "Takipten Çık" : "Takip Et"}
              </button>
            ) : null}
          </div>
          
          {user.about && (
            <p className="text-muted-foreground max-w-2xl">{user.about}</p>
          )}

          <div className="flex flex-wrap items-center justify-center sm:justify-start gap-4 pt-2">
            {getRoleBadge(user.role)}
            
            <div className="flex items-center gap-3 text-sm font-medium">
              <button onClick={() => setIsFollowersModalOpen(true)} className="hover:underline focus:outline-none">
                <span className="text-foreground font-semibold">{user.followerCount || 0}</span> <span className="text-muted-foreground font-normal">Takipçi</span>
              </button>
              <span className="text-muted-foreground/30">•</span>
              <button onClick={() => setIsFollowingModalOpen(true)} className="hover:underline focus:outline-none">
                <span className="text-foreground font-semibold">{user.followingCount || 0}</span> <span className="text-muted-foreground font-normal">Takip Edilen</span>
              </button>
            </div>

            <FollowersModal isOpen={isFollowersModalOpen} onClose={() => setIsFollowersModalOpen(false)} username={user.username} type="followers" />
            <FollowersModal isOpen={isFollowingModalOpen} onClose={() => setIsFollowingModalOpen(false)} username={user.username} type="following" />

            <span className="inline-flex items-center gap-1.5 text-sm font-medium text-muted-foreground ml-2 border-l pl-4 border-border">
              <Calendar className="h-4 w-4" />
              {new Date(user.createdAt).toLocaleDateString('tr-TR', { month: 'long', year: 'numeric' })} katıldı
            </span>

            {(user.githubUrl || user.linkedInUrl || user.websiteUrl) && (
              <div className="flex items-center gap-3 border-l pl-4 ml-2">
                {user.githubUrl && (
                  <a href={`https://github.com/${user.githubUrl}`} target="_blank" rel="noreferrer" className="text-muted-foreground hover:text-foreground">
                    <LinkIcon className="w-5 h-5" />
                  </a>
                )}
                {user.linkedInUrl && (
                  <a href={`https://linkedin.com/in/${user.linkedInUrl}`} target="_blank" rel="noreferrer" className="text-muted-foreground hover:text-blue-500">
                    <LinkIcon className="w-5 h-5" />
                  </a>
                )}
                {user.websiteUrl && (
                  <a href={user.websiteUrl} target="_blank" rel="noreferrer" className="text-muted-foreground hover:text-foreground">
                    <Globe className="w-5 h-5" />
                  </a>
                )}
              </div>
            )}
            
            {/* Gamification: Streak & Badges */}
            {((user.currentStreak ?? 0) > 0 || (user.badges && user.badges.length > 0)) && (
              <div className="flex items-center gap-3 w-full sm:w-auto mt-4 sm:mt-0 sm:border-l sm:pl-4">
                {(user.currentStreak ?? 0) > 0 && (
                  <div className="flex items-center gap-1.5 rounded-full bg-orange-100 px-3 py-1 text-sm font-semibold text-orange-600 dark:bg-orange-900/30 dark:text-orange-400" title="Her gün oy verme serisi">
                    <span className="text-base">🔥</span> {user.currentStreak} Gün
                  </div>
                )}
                {user.badges && user.badges.map((badge, idx) => (
                  <div key={idx} className="flex items-center justify-center w-8 h-8 rounded-full bg-amber-100 text-amber-600 dark:bg-amber-900/30 dark:text-amber-400 shadow-sm border border-amber-200/50 dark:border-amber-700/50 cursor-default transition-transform hover:scale-110" title={badge.name}>
                    {badge.icon === 'Flame' ? '🔥' : badge.icon === 'Award' ? '🏆' : badge.icon === 'Star' ? '🌟' : '🎖️'}
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Paylaşılan Ürünler */}
      <div className="space-y-6">
        <div className="flex items-center gap-2 pb-2 border-b">
          <Package className="h-6 w-6 text-primary" />
          <h2 className="text-2xl font-bold tracking-tight text-foreground">
            Paylaştığı Ürünler
          </h2>
          <span className="ml-auto rounded-full bg-muted px-3 py-0.5 text-sm font-medium">
            {products.length}
          </span>
        </div>

        {products.length > 0 ? (
          <div className="rounded-3xl border border-border bg-card p-2 shadow-sm sm:p-3 flex flex-col divide-y divide-border/60">
            {products.map((product) => (
              <ProductRow key={product.id} product={product} />
            ))}
          </div>
        ) : (
          <div className="flex flex-col items-center justify-center py-16 text-center text-muted-foreground rounded-3xl border border-dashed border-border bg-muted/30">
            <Package className="h-12 w-12 mb-4 opacity-20" />
            <h3 className="text-lg font-semibold text-foreground mb-1">Henüz ürün paylaşmamış</h3>
            <p className="text-sm">@{user.username} tarafından paylaşılan bir ürün bulunmuyor.</p>
          </div>
        )}
      </div>
    </main>
  );
}
