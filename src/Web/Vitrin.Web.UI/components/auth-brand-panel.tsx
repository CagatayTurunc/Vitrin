"use client";

import { Sparkles, TrendingUp, Users, ArrowUp, Star } from "lucide-react";
import React from "react";

export function AuthBrandPanel() {
  return (
    <div className="relative hidden h-full flex-col bg-[#00A170] p-10 text-white lg:flex overflow-hidden">
      {/* Animated dots background */}
      <div className="absolute inset-0 z-0 opacity-20 bg-[radial-gradient(#ffffff_1px,transparent_1px)] [background-size:24px_24px] pointer-events-none" />

      <div className="relative z-20 flex items-center gap-2 text-xl font-bold tracking-tight">
        <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-white/10 backdrop-blur-md">
          <Sparkles className="h-6 w-6" />
        </div>
        Vitrin
      </div>

      <div className="relative z-20 mt-32 max-w-md">
        {/* Floating Cards Container */}
        <div className="relative h-40 w-full mb-8">
          {/* FinTrack Card */}
          <div className="absolute left-0 top-16 w-64 rounded-2xl bg-white/10 p-4 backdrop-blur-md border border-white/20 shadow-xl animate-float">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-3">
                <div className="flex h-10 w-10 items-center justify-center rounded-full bg-white/20">
                  <TrendingUp className="h-5 w-5" />
                </div>
                <div>
                  <div className="font-semibold text-sm">FinTrack</div>
                  <div className="text-xs text-white/70">Kişisel finans takibi</div>
                </div>
              </div>
              <div className="flex flex-col items-center justify-center rounded-lg bg-white/20 px-2 py-1 text-xs font-bold">
                <ArrowUp className="mb-0.5 h-3 w-3" />
                94
              </div>
            </div>
          </div>

          {/* NotAI Card */}
          <div className="absolute right-0 top-0 w-64 rounded-2xl bg-white/10 p-4 backdrop-blur-md border border-white/20 shadow-xl animate-float-delayed">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-3">
                <div className="flex h-10 w-10 items-center justify-center rounded-full bg-white/20">
                  <Sparkles className="h-5 w-5" />
                </div>
                <div>
                  <div className="font-semibold text-sm">NotAI</div>
                  <div className="text-xs text-white/70">Yapay zeka not de...</div>
                </div>
              </div>
              <div className="flex flex-col items-center justify-center rounded-lg bg-white/20 px-2 py-1 text-xs font-bold">
                <ArrowUp className="mb-0.5 h-3 w-3" />
                128
              </div>
            </div>
          </div>
        </div>

        <h1 className="text-4xl font-bold tracking-tight mb-4">
          Büyümenin başladığı yer.
        </h1>
        <p className="text-lg text-white/80 mb-12">
          En yeni ürünleri keşfet, favorilerine oy ver ve kendi projelerini
          doğru kitleyle buluştur.
        </p>

        {/* Stats */}
        <div className="flex gap-4 mb-12">
          <div className="flex items-center gap-3 rounded-xl bg-white/10 px-4 py-3 backdrop-blur-md border border-white/10">
            <Users className="h-5 w-5 text-white/80" />
            <div>
              <div className="font-bold text-sm">12.4B+</div>
              <div className="text-[10px] text-white/60 uppercase font-semibold">Topluluk</div>
            </div>
          </div>
          <div className="flex items-center gap-3 rounded-xl bg-white/10 px-4 py-3 backdrop-blur-md border border-white/10">
            <TrendingUp className="h-5 w-5 text-white/80" />
            <div>
              <div className="font-bold text-sm">3.200+</div>
              <div className="text-[10px] text-white/60 uppercase font-semibold">Ürün</div>
            </div>
          </div>
          <div className="flex items-center gap-3 rounded-xl bg-white/10 px-4 py-3 backdrop-blur-md border border-white/10">
            <ArrowUp className="h-5 w-5 text-white/80" />
            <div>
              <div className="font-bold text-sm">480B+</div>
              <div className="text-[10px] text-white/60 uppercase font-semibold">Oy</div>
            </div>
          </div>
        </div>

        {/* Testimonial Card */}
        <div className="rounded-2xl bg-white/10 p-6 backdrop-blur-md border border-white/20 mt-auto">
          <div className="flex gap-1 mb-3">
            {[1, 2, 3, 4, 5].map((star) => (
              <Star key={star} className="h-4 w-4 fill-white text-white" />
            ))}
          </div>
          <p className="text-sm text-white/90 mb-4 leading-relaxed">
            "Vitrin sayesinde harika ürünler keşfettim ve kendi projelerimi doğru kitleye ulaştırma şansı buldum."
          </p>
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-full bg-[#008f63] font-bold text-sm">
              N
            </div>
            <div>
              <div className="text-sm font-bold">Nur Aksoy</div>
              <div className="text-xs text-white/70">Ürün Tasarımcısı</div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
