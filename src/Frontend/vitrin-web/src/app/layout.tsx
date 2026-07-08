import type { Metadata } from "next";
import { Inter } from "next/font/google";
import "./globals.css";

const inter = Inter({ subsets: ["latin"] });

export const metadata: Metadata = {
  title: "Vitrin - Günün Ürünleri",
  description: "Türkiye'nin teknoloji vitrini. En yeni ürünleri keşfet, oyla, destek ol.",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="tr">
      <body className={`${inter.className} bg-white dark:bg-[#1a1a1a] text-gray-900 dark:text-gray-100 transition-colors duration-200`}>
        {children}
      </body>
    </html>
  );
}
