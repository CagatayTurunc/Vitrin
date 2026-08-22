import type { Metadata } from "next";

export const metadata: Metadata = {
  title: "Koleksiyonlar — Vitrin",
  description: "Ürün koleksiyonlarını keşfet, kendi listeni oluştur ve toplulukla paylaş. Vitrin'in küratör seçkileri.",
};

export default function CollectionsLayout({ children }: { children: React.ReactNode }) {
  return children;
}
