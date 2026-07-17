'use client';

import { useState, useEffect } from 'react';
import { useSession } from 'next-auth/react';
import { useRouter } from 'next/navigation';
import { useProductStore } from '@/core/application/useProductStore';
import { ProductRow } from '@/components/product-row';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Sparkles, Heart, Box, CheckCircle2, User as UserIcon, CalendarDays, Settings, LayoutList } from 'lucide-react';
import Link from 'next/link';
import { FollowersModal } from '@/components/followers-modal';
import Image from 'next/image';
import type { UserProfile } from '@/core/domain/user.types';

function getRoleString(role: unknown): string {
  if (role === 0) return 'Member';
  if (role === 1) return 'Maker';
  if (role === 2) return 'Admin';
  return typeof role === 'string' && role ? role : 'Kullanıcı';
}

export default function ProfilePage() {
  const { data: session, status } = useSession();
  const router = useRouter();
  const [activeTab, setActiveTab] = useState<'my-products' | 'upvoted'>('my-products');
  
  const { makerProducts, upvotedProducts, fetchMakerProducts, fetchUpvotedProducts, isLoading } = useProductStore();
  const [profileData, setProfileData] = useState<UserProfile | null>(null);
  const [isFollowersModalOpen, setIsFollowersModalOpen] = useState(false);
  const [isFollowingModalOpen, setIsFollowingModalOpen] = useState(false);

  useEffect(() => {
    if (status === 'unauthenticated') {
      router.push('/login');
    }
  }, [status, router]);

  useEffect(() => {
    if (session?.user) {
      const user = session.user;
      if (user.id) {
        fetchMakerProducts(user.id);
      }
      if (session.accessToken) {
        fetchUpvotedProducts(session.accessToken as string);
        
        // Kendi profil detaylarımızı çekiyoruz (takipçi vs)
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
    }
  }, [session, fetchMakerProducts, fetchUpvotedProducts]);

  if (status === 'loading' || !session?.user) {
    return <div className="min-h-screen flex items-center justify-center">Yükleniyor...</div>;
  }

  const user = session.user;
  
  const currentRole = profileData?.role !== undefined ? getRoleString(profileData.role) : getRoleString(user.role);
  const isMaker = currentRole === 'Maker' || currentRole === 'Admin';
  const avatarUrl = profileData?.avatarUrl ?? user.image;
  
  // Avatar baş harfi
  const initials = user.name ? user.name.substring(0, 2).toUpperCase() : user.email?.substring(0, 2).toUpperCase();

  return (
    <div className="min-h-screen bg-background pb-24">
      {/* Profil Header Kutusu */}
      <div className="border-b border-border bg-card/50 backdrop-blur-md">
        <div className="mx-auto max-w-4xl px-4 py-12 sm:px-6">
          <div className="flex flex-col md:flex-row gap-8 items-center md:items-start">
            {/* Avatar */}
            <div className="shrink-0 relative">
              <div className="h-32 w-32 rounded-full border-4 border-background bg-gradient-to-br from-emerald-400 to-cyan-500 flex items-center justify-center text-white text-4xl font-bold shadow-2xl overflow-hidden">
                {avatarUrl ? (
                  <Image src={avatarUrl} alt={user.name ?? user.username ?? 'Profil'} fill sizes="128px" className="object-cover" />
                ) : (
                  initials
                )}
              </div>
              {isMaker && (
                <div className="absolute -bottom-2 -right-2 bg-emerald-500 text-white rounded-full p-1.5 border-4 border-background shadow-lg" title="Doğrulanmış Maker">
                  <CheckCircle2 className="w-5 h-5" />
                </div>
              )}
            </div>

            {/* Kullanıcı Bilgileri */}
            <div className="flex-1 text-center md:text-left space-y-3">
              <div className="flex flex-col md:flex-row md:items-center gap-3">
                <h1 className="text-3xl font-extrabold text-foreground">
                  {profileData?.fullName || profileData?.username || user.name || user.username || user.email || 'İsimsiz Kullanıcı'}
                </h1>
                <div className="flex gap-2 justify-center md:justify-start">
                  <Badge variant={isMaker ? "default" : "secondary"} className={isMaker ? "bg-emerald-500/10 text-emerald-500 hover:bg-emerald-500/20" : ""}>
                    {currentRole}
                  </Badge>
                </div>
              </div>
              
              {profileData?.headline && (
                <p className="text-foreground font-medium text-lg">{profileData.headline}</p>
              )}
              
              {profileData?.about && (
                <p className="text-muted-foreground text-sm max-w-2xl text-center md:text-left">{profileData.about}</p>
              )}

              <div className="flex flex-col md:flex-row gap-2 md:gap-4 items-center md:items-start text-muted-foreground text-sm">
                <p>{user.email}</p>
                {profileData?.websiteUrl && (
                  <>
                    <span className="hidden md:inline">•</span>
                    <a href={profileData.websiteUrl} target="_blank" rel="noopener noreferrer" className="text-primary hover:underline">
                      {profileData.websiteUrl.replace(/^https?:\/\//, '')}
                    </a>
                  </>
                )}
              </div>
              
              <div className="flex flex-wrap items-center justify-center md:justify-start gap-4 pt-2">
                <div className="flex items-center gap-3 text-sm font-medium">
                  <button onClick={() => setIsFollowersModalOpen(true)} className="hover:underline focus:outline-none">
                    <span className="text-foreground font-semibold">{profileData?.followerCount || 0}</span> <span className="text-muted-foreground font-normal">Takipçi</span>
                  </button>
                  <span className="text-muted-foreground/30">•</span>
                  <button onClick={() => setIsFollowingModalOpen(true)} className="hover:underline focus:outline-none">
                    <span className="text-foreground font-semibold">{profileData?.followingCount || 0}</span> <span className="text-muted-foreground font-normal">Takip Edilen</span>
                  </button>
                </div>
                
                <FollowersModal isOpen={isFollowersModalOpen} onClose={() => setIsFollowersModalOpen(false)} username={user.username ?? ''} type="followers" />
                <FollowersModal isOpen={isFollowingModalOpen} onClose={() => setIsFollowingModalOpen(false)} username={user.username ?? ''} type="following" />
                
                <div className="flex items-center gap-1.5 text-sm font-medium text-muted-foreground ml-2 border-l pl-4 border-border">
                  <CalendarDays className="w-4 h-4" />
                  <span>Temmuz 2026&apos;da katıldı</span>
                </div>
                
                {/* Gamification: Streak & Badges */}
                {((profileData?.currentStreak ?? 0) > 0 || (profileData?.badges && profileData.badges.length > 0)) && (
                  <div className="flex items-center gap-3 ml-2 border-l pl-4 border-border">
                    {(profileData?.currentStreak ?? 0) > 0 && (
                      <div className="flex items-center gap-1.5 rounded-full bg-orange-100 px-3 py-1 text-sm font-semibold text-orange-600 dark:bg-orange-900/30 dark:text-orange-400" title="Her gün oy verme serisi">
                        <span className="text-base">🔥</span> {profileData?.currentStreak} Gün
                      </div>
                    )}
                    {profileData?.badges?.map((badge, idx) => (
                      <div key={idx} className="flex items-center justify-center w-8 h-8 rounded-full bg-amber-100 text-amber-600 dark:bg-amber-900/30 dark:text-amber-400 shadow-sm border border-amber-200/50 dark:border-amber-700/50 cursor-default transition-transform hover:scale-110" title={badge.name}>
                        {badge.icon === 'Flame' ? '🔥' : badge.icon === 'Award' ? '🏆' : badge.icon === 'Star' ? '🌟' : '🎖️'}
                      </div>
                    ))}
                  </div>
                )}
              </div>
            </div>
            
            {/* Aksiyonlar */}
            <div className="shrink-0 flex flex-col gap-2">
              <Button variant="outline" className="rounded-full shadow-sm" onClick={() => router.push(`/profile/${user.username}`)}>
                <UserIcon className="w-4 h-4 mr-2" />
                Herkese Açık Profil
              </Button>
              <Button variant="ghost" className="rounded-full shadow-sm text-muted-foreground" onClick={() => router.push('/settings')}>
                <Settings className="w-4 h-4 mr-2" />
                Ayarlar
              </Button>
            </div>
          </div>
        </div>
      </div>

      <main className="mx-auto max-w-4xl px-4 py-8 sm:px-6">
        {/* Tab Menü */}
        <div className="flex gap-6 border-b border-border/60 mb-8 overflow-x-auto no-scrollbar">
          <button
            onClick={() => setActiveTab('my-products')}
            className={`flex items-center gap-2 pb-4 text-sm font-semibold transition-colors border-b-2 whitespace-nowrap ${
              activeTab === 'my-products'
                ? 'border-primary text-foreground'
                : 'border-transparent text-muted-foreground hover:text-foreground'
            }`}
          >
            <Box className="w-4 h-4" />
            Ürünlerim
            <span className="ml-1.5 rounded-full bg-muted px-2 py-0.5 text-xs">{makerProducts.length}</span>
          </button>
          
          <button
            onClick={() => setActiveTab('upvoted')}
            className={`flex items-center gap-2 pb-4 text-sm font-semibold transition-colors border-b-2 whitespace-nowrap ${
              activeTab === 'upvoted'
                ? 'border-primary text-foreground'
                : 'border-transparent text-muted-foreground hover:text-foreground'
            }`}
          >
            <Heart className="w-4 h-4" />
            Oyladıklarım
            <span className="ml-1.5 rounded-full bg-muted px-2 py-0.5 text-xs">{upvotedProducts.length}</span>
          </button>
        </div>

        {/* Tab İçerikleri */}
        {isLoading ? (
          <div className="py-12 text-center text-muted-foreground">Yükleniyor...</div>
        ) : (
          <div className="space-y-4">
            {activeTab === 'my-products' && (
              <>
                <div className="flex justify-end mb-4">
                  <Link href="/my-products">
                    <Button variant="outline" size="sm" className="rounded-full text-xs gap-1.5">
                      <LayoutList className="w-3.5 h-3.5" />
                      Tüm Ürünlerimi Yönet
                    </Button>
                  </Link>
                </div>
                {makerProducts.length === 0 ? (
                  <div className="text-center py-16 px-4 bg-muted/30 rounded-3xl border border-border border-dashed">
                    <Box className="w-12 h-12 text-muted-foreground mx-auto mb-4 opacity-50" />
                    <h3 className="text-lg font-bold text-foreground mb-2">Henüz bir ürün eklemedin</h3>
                    <p className="text-muted-foreground max-w-md mx-auto mb-6">Harika projeni Vitrin topluluğuyla paylaşmanın tam zamanı!</p>
                    <Button onClick={() => router.push('/submit')} className="rounded-full bg-primary text-primary-foreground">
                      <Sparkles className="w-4 h-4 mr-2" />
                      İlk Ürününü Ekle
                    </Button>
                  </div>
                ) : (
                  makerProducts.map((product) => (
                    <ProductRow key={product.id} product={product} />
                  ))
                )}
              </>
            )}

            {activeTab === 'upvoted' && (
              <>
                {upvotedProducts.length === 0 ? (
                  <div className="text-center py-16 px-4 bg-muted/30 rounded-3xl border border-border border-dashed">
                    <Heart className="w-12 h-12 text-muted-foreground mx-auto mb-4 opacity-50" />
                    <h3 className="text-lg font-bold text-foreground mb-2">Henüz bir ürün oylamadın</h3>
                    <p className="text-muted-foreground max-w-md mx-auto mb-6">Anasayfaya giderek ilgini çeken ürünlere destek olabilirsin.</p>
                    <Button onClick={() => router.push('/')} variant="outline" className="rounded-full">
                      Ürünleri Keşfet
                    </Button>
                  </div>
                ) : (
                  upvotedProducts.map((product) => (
                    <ProductRow key={product.id} product={product} />
                  ))
                )}
              </>
            )}
          </div>
        )}
      </main>
    </div>
  );
}
