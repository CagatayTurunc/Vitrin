import type { Metadata } from "next";

export const metadata: Metadata = {
  title: "Keşfet — Vitrin",
  description: "Kategori, konu ve etkileşim eşikleriyle filtrele; trend, oy ve yorum sıralamasına göre yeni ürünler keşfet.",
};

export default function DiscoverLayout({ children }: { children: React.ReactNode }) {
  return children;
}
