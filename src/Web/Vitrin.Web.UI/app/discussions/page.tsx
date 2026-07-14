"use client";

import { MessageCircle, ThumbsUp, Eye, Pin, TrendingUp, Clock } from "lucide-react";

const discussions = [
  {
    id: 1,
    title: "2026'da en çok hangi SaaS kategorisi büyüyor?",
    author: "cagatayy",
    avatarLetter: "C",
    replies: 24,
    views: 312,
    likes: 18,
    pinned: true,
    category: "Genel",
    time: "2 saat önce",
  },
  {
    id: 2,
    title: "Yeni maker'lar için ürün lansmanı checklist'i",
    author: "vitrinadmin",
    avatarLetter: "V",
    replies: 11,
    views: 198,
    likes: 32,
    pinned: true,
    category: "Rehber",
    time: "5 saat önce",
  },
  {
    id: 3,
    title: "Next.js 15 vs Remix: hangisini tercih edersiniz?",
    author: "devuser",
    avatarLetter: "D",
    replies: 41,
    views: 567,
    likes: 45,
    pinned: false,
    category: "Teknoloji",
    time: "1 gün önce",
  },
  {
    id: 4,
    title: "Açık kaynak projenizi nasıl tanıtıyorsunuz?",
    author: "makerx",
    avatarLetter: "M",
    replies: 16,
    views: 234,
    likes: 22,
    pinned: false,
    category: "Maker",
    time: "2 gün önce",
  },
  {
    id: 5,
    title: "Vitrin'de en çok oy alan ürünlerin ortak özellikleri",
    author: "analyst",
    avatarLetter: "A",
    replies: 8,
    views: 145,
    likes: 14,
    pinned: false,
    category: "Analiz",
    time: "3 gün önce",
  },
];

const categories = ["Tümü", "Genel", "Rehber", "Teknoloji", "Maker", "Analiz"];

export default function DiscussionsPage() {
  return (
    <main className="min-h-screen bg-background">
      {/* Header */}
      <div className="border-b border-border bg-muted/20">
        <div className="mx-auto max-w-4xl px-4 py-12">
          <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
            <div>
              <h1 className="text-3xl font-extrabold text-foreground">Tartışmalar</h1>
              <p className="text-muted-foreground mt-1">Toplulukla fikir alışverişi yap</p>
            </div>
            <button className="px-5 py-2.5 rounded-full bg-emerald-500 hover:bg-emerald-600 text-white text-sm font-semibold transition-colors flex items-center gap-2">
              <MessageCircle className="w-4 h-4" /> Yeni Konu Aç
            </button>
          </div>
        </div>
      </div>

      <div className="mx-auto max-w-4xl px-4 py-8">
        {/* Filter */}
        <div className="flex gap-2 flex-wrap mb-8">
          {categories.map((cat) => (
            <button
              key={cat}
              className={`px-4 py-1.5 rounded-full text-sm font-medium border transition-colors ${
                cat === "Tümü"
                  ? "bg-emerald-500 text-white border-emerald-500"
                  : "border-border text-muted-foreground hover:border-emerald-500/30 hover:text-foreground"
              }`}
            >
              {cat}
            </button>
          ))}
        </div>

        {/* List */}
        <div className="space-y-3">
          {discussions.map((d) => (
            <div
              key={d.id}
              className="bg-card border border-border rounded-2xl p-5 hover:border-emerald-500/30 transition-colors group"
            >
              <div className="flex items-start gap-4">
                {/* Avatar */}
                <div className="w-9 h-9 rounded-full bg-emerald-500/10 flex items-center justify-center text-emerald-600 dark:text-emerald-400 font-bold text-sm shrink-0">
                  {d.avatarLetter}
                </div>

                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2 flex-wrap mb-1">
                    {d.pinned && (
                      <span className="flex items-center gap-1 text-xs text-amber-500 font-medium">
                        <Pin className="w-3 h-3" /> Sabitlenmiş
                      </span>
                    )}
                    <span className="text-xs text-emerald-500 bg-emerald-500/10 px-2 py-0.5 rounded-full font-medium">
                      {d.category}
                    </span>
                  </div>

                  <h3 className="font-semibold text-foreground group-hover:text-emerald-500 transition-colors mb-2">
                    {d.title}
                  </h3>

                  <div className="flex items-center gap-4 text-xs text-muted-foreground flex-wrap">
                    <span>@{d.author}</span>
                    <span className="flex items-center gap-1"><Clock className="w-3 h-3" />{d.time}</span>
                    <span className="flex items-center gap-1"><MessageCircle className="w-3 h-3" />{d.replies} yanıt</span>
                    <span className="flex items-center gap-1"><Eye className="w-3 h-3" />{d.views} görüntüleme</span>
                    <span className="flex items-center gap-1"><ThumbsUp className="w-3 h-3" />{d.likes}</span>
                  </div>
                </div>
              </div>
            </div>
          ))}
        </div>

        {/* Trending */}
        <div className="mt-10 bg-card border border-border rounded-2xl p-6">
          <div className="flex items-center gap-2 text-sm font-semibold text-foreground mb-4">
            <TrendingUp className="w-4 h-4 text-emerald-500" /> Trend Konular
          </div>
          <div className="space-y-2">
            {discussions.slice(0, 3).map((d) => (
              <div key={d.id} className="flex items-center justify-between text-sm">
                <span className="text-muted-foreground hover:text-foreground transition-colors cursor-pointer line-clamp-1">
                  {d.title}
                </span>
                <span className="text-xs text-muted-foreground ml-4 shrink-0">{d.replies} yanıt</span>
              </div>
            ))}
          </div>
        </div>
      </div>
    </main>
  );
}
