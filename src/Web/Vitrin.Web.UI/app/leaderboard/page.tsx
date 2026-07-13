'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import Image from 'next/image';
import Link from 'next/link';
import { Trophy, Flame, Star, Medal, Users } from 'lucide-react';
import { Badge } from '@/components/ui/badge';

export default function LeaderboardPage() {
  const [data, setData] = useState<{ topStreaks: any[], topMakers: any[] } | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    const fetchLeaderboard = async () => {
      try {
        const res = await fetch(process.env.NEXT_PUBLIC_API_URL + '/api/auth/leaderboard');
        if (res.ok) {
          const json = await res.json();
          setData(json);
        }
      } catch (err) {
        console.error("Failed to fetch leaderboard", err);
      } finally {
        setIsLoading(false);
      }
    };
    fetchLeaderboard();
  }, []);

  const getRankBadge = (index: number) => {
    if (index === 0) return <Medal className="w-6 h-6 text-yellow-400" fill="currentColor" />;
    if (index === 1) return <Medal className="w-6 h-6 text-slate-300" fill="currentColor" />;
    if (index === 2) return <Medal className="w-6 h-6 text-amber-600" fill="currentColor" />;
    return <span className="w-6 h-6 flex items-center justify-center font-bold text-muted-foreground">{index + 1}</span>;
  };

  const getInitials = (name: string, email: string) => {
    if (name) return name.substring(0, 2).toUpperCase();
    if (email) return email.substring(0, 2).toUpperCase();
    return "U";
  };

  return (
    <div className="min-h-screen bg-background">
      {/* Hero Section */}
      <div className="relative py-20 overflow-hidden border-b border-border/40">
        <div className="absolute inset-0 bg-gradient-to-br from-amber-500/10 via-background to-background" />
        <div className="absolute inset-0 bg-[url('/noise.png')] opacity-20 mix-blend-overlay" />
        <div className="relative mx-auto max-w-5xl px-4 text-center sm:px-6">
          <Badge variant="outline" className="mb-4 bg-background/50 backdrop-blur-md border-amber-500/30 text-amber-500">
            <Trophy className="w-3 h-3 mr-1.5" /> Liderlik Tablosu
          </Badge>
          <h1 className="text-4xl font-extrabold tracking-tight text-foreground sm:text-6xl mb-4">
            Topluluğun <span className="text-transparent bg-clip-text bg-gradient-to-r from-amber-500 to-orange-500">En İyileri</span>
          </h1>
          <p className="mx-auto max-w-2xl text-lg text-muted-foreground">
            Her gün yeni ürünler keşfeden aktif avcılar ve muhteşem ürünler geliştiren en popüler yapımcılar.
          </p>
        </div>
      </div>

      <main className="mx-auto max-w-6xl px-4 py-12 sm:px-6">
        <div className="grid grid-cols-1 md:grid-cols-2 gap-8 lg:gap-12">
          
          {/* Top Streaks (Ateşli Avcılar) */}
          <div className="space-y-6">
            <div className="flex items-center gap-3 border-b border-border/60 pb-4">
              <div className="flex items-center justify-center w-10 h-10 rounded-xl bg-orange-500/10 text-orange-500">
                <Flame className="w-5 h-5" />
              </div>
              <div>
                <h2 className="text-2xl font-bold text-foreground">Ateşli Avcılar</h2>
                <p className="text-sm text-muted-foreground">En uzun seriye sahip kullanıcılar</p>
              </div>
            </div>

            <div className="space-y-3">
              {isLoading ? (
                Array(5).fill(0).map((_, i) => <div key={i} className="h-20 w-full rounded-2xl bg-muted animate-pulse" />)
              ) : data?.topStreaks && data.topStreaks.length > 0 ? (
                data.topStreaks.map((user, idx) => (
                  <Link href={`/profile/${user.username}`} key={user.id} className="flex items-center gap-4 p-4 rounded-2xl bg-card border border-border/50 hover:border-orange-500/30 hover:shadow-md transition-all group">
                    <div className="shrink-0 w-8 text-center flex justify-center">
                      {getRankBadge(idx)}
                    </div>
                    <div className="shrink-0 relative">
                      <div className="h-12 w-12 rounded-full border-2 border-background bg-gradient-to-br from-emerald-400 to-cyan-500 flex items-center justify-center text-white font-bold overflow-hidden shadow-sm">
                        {user.avatarUrl ? (
                          <img src={user.avatarUrl} alt={user.fullName || user.username} className="h-full w-full object-cover" />
                        ) : (
                          getInitials(user.fullName || user.username, "")
                        )}
                      </div>
                    </div>
                    <div className="flex-1 min-w-0">
                      <h3 className="text-base font-bold text-foreground truncate group-hover:text-primary transition-colors">
                        {user.fullName || user.username || 'İsimsiz Kullanıcı'}
                      </h3>
                      <p className="text-sm text-muted-foreground truncate">
                        @{user.username}
                      </p>
                    </div>
                    <div className="shrink-0 flex items-center gap-1.5 rounded-full bg-orange-100 px-3 py-1 text-sm font-semibold text-orange-600 dark:bg-orange-900/30 dark:text-orange-400">
                      <span className="text-base">🔥</span> {user.currentStreak} Gün
                    </div>
                  </Link>
                ))
              ) : (
                <div className="p-8 text-center text-muted-foreground border border-dashed rounded-2xl bg-muted/20">
                  Henüz veri bulunmuyor.
                </div>
              )}
            </div>
          </div>

          {/* Top Makers (Popüler Yapımcılar) */}
          <div className="space-y-6">
            <div className="flex items-center gap-3 border-b border-border/60 pb-4">
              <div className="flex items-center justify-center w-10 h-10 rounded-xl bg-amber-500/10 text-amber-500">
                <Star className="w-5 h-5" />
              </div>
              <div>
                <h2 className="text-2xl font-bold text-foreground">Popüler Yapımcılar</h2>
                <p className="text-sm text-muted-foreground">En çok takip edilen yapımcılar</p>
              </div>
            </div>

            <div className="space-y-3">
              {isLoading ? (
                Array(5).fill(0).map((_, i) => <div key={i} className="h-20 w-full rounded-2xl bg-muted animate-pulse" />)
              ) : data?.topMakers && data.topMakers.length > 0 ? (
                data.topMakers.map((user, idx) => (
                  <Link href={`/profile/${user.username}`} key={user.id} className="flex items-center gap-4 p-4 rounded-2xl bg-card border border-border/50 hover:border-amber-500/30 hover:shadow-md transition-all group">
                    <div className="shrink-0 w-8 text-center flex justify-center">
                      {getRankBadge(idx)}
                    </div>
                    <div className="shrink-0 relative">
                      <div className="h-12 w-12 rounded-full border-2 border-background bg-gradient-to-br from-indigo-400 to-purple-500 flex items-center justify-center text-white font-bold overflow-hidden shadow-sm">
                        {user.avatarUrl ? (
                          <img src={user.avatarUrl} alt={user.fullName || user.username} className="h-full w-full object-cover" />
                        ) : (
                          getInitials(user.fullName || user.username, "")
                        )}
                      </div>
                    </div>
                    <div className="flex-1 min-w-0">
                      <h3 className="text-base font-bold text-foreground truncate group-hover:text-primary transition-colors">
                        {user.fullName || user.username || 'İsimsiz Kullanıcı'}
                      </h3>
                      <p className="text-sm text-muted-foreground truncate">
                        @{user.username}
                      </p>
                    </div>
                    <div className="shrink-0 flex items-center gap-1.5 rounded-full bg-blue-100 px-3 py-1 text-sm font-semibold text-blue-600 dark:bg-blue-900/30 dark:text-blue-400">
                      <Users className="w-4 h-4" /> {user.followerCount}
                    </div>
                  </Link>
                ))
              ) : (
                <div className="p-8 text-center text-muted-foreground border border-dashed rounded-2xl bg-muted/20">
                  Henüz veri bulunmuyor.
                </div>
              )}
            </div>
          </div>

        </div>
      </main>
    </div>
  );
}
