"use client";

import { useState, useEffect } from "react";
import Link from "next/link";
import { Flame, Medal, Trophy } from "lucide-react";

export function LeaderboardWidget() {
  const [makers, setMakers] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetch(process.env.NEXT_PUBLIC_API_URL + '/api/auth/leaderboard')
      .then(res => res.json())
      .then(data => {
        setMakers(data.topMakers || []);
        setLoading(false);
      })
      .catch(err => {
        console.error(err);
        setLoading(false);
      });
  }, []);

  return (
    <div className="rounded-xl border bg-card text-card-foreground shadow-sm">
      <div className="flex flex-col space-y-1.5 p-6 pb-4">
        <div className="flex items-center gap-2">
          <Trophy className="h-5 w-5 text-yellow-500" />
          <h3 className="font-semibold leading-none tracking-tight">Liderlik Tablosu</h3>
        </div>
        <p className="text-sm text-muted-foreground">
          Topluluğun en aktif ve başarılı üyeleri.
        </p>
      </div>
      <div className="p-6 pt-0">
        {loading ? (
          <div className="space-y-4">
            {[1, 2, 3].map((i) => (
              <div key={i} className="flex items-center gap-3">
                <div className="h-8 w-8 rounded-full bg-muted animate-pulse" />
                <div className="space-y-2 flex-1">
                  <div className="h-4 w-20 bg-muted animate-pulse rounded" />
                  <div className="h-3 w-12 bg-muted animate-pulse rounded" />
                </div>
              </div>
            ))}
          </div>
        ) : (
          <div className="space-y-4">
            {makers.slice(0, 5).map((maker, index) => (
              <Link href={`/profile/${maker.username}`} key={maker.id} className="flex items-center gap-3 group">
                <div className="relative flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-muted overflow-hidden">
                  {maker.profilePictureUrl ? (
                    <img src={maker.profilePictureUrl} alt={maker.username} className="h-full w-full object-cover" />
                  ) : (
                    <span className="text-xs font-medium uppercase text-muted-foreground">{maker.username.substring(0, 2)}</span>
                  )}
                </div>
                <div className="flex-1 overflow-hidden">
                  <p className="truncate text-sm font-medium leading-none group-hover:underline">
                    {maker.firstName} {maker.lastName}
                  </p>
                  <p className="truncate text-xs text-muted-foreground mt-1">
                    @{maker.username}
                  </p>
                </div>
                <div className="flex items-center gap-2">
                  <div className="flex items-center text-xs font-semibold text-orange-500 bg-orange-500/10 px-1.5 py-0.5 rounded-full">
                    <Flame className="h-3 w-3 mr-1" />
                    {maker.streakCount}
                  </div>
                </div>
              </Link>
            ))}
          </div>
        )}
        <div className="mt-6 border-t pt-4">
          <Link href="/leaderboard" className="text-sm text-primary hover:underline font-medium w-full text-center block">
            Tüm tabloyu gör &rarr;
          </Link>
        </div>
      </div>
    </div>
  );
}
