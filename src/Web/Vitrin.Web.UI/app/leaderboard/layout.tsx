import type { Metadata } from "next";

export const metadata: Metadata = {
  title: "Liderlik Tablosu — Vitrin",
  description: "En uzun seriye sahip avcılar ve en çok takip edilen yapımcılar. Türkiye'nin ürün topluluğunda öne çıkanlar.",
};

export default function LeaderboardLayout({ children }: { children: React.ReactNode }) {
  return children;
}
