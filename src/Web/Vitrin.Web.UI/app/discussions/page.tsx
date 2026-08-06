import { MessageCircle } from "lucide-react";
import { CommunityFeed } from "@/components/community-feed";

export const metadata = { title: "Tartışmalar — Vitrin", description: "Türkiye ürün ekosisteminin maker, teknik ve geri bildirim tartışmaları." };

export default function DiscussionsPage() {
  return <main className="mx-auto min-h-screen w-full max-w-5xl px-4 py-10 sm:px-6">
    <section className="mb-8 overflow-hidden rounded-[2rem] border bg-card p-8"><div className="mb-4 inline-flex rounded-2xl bg-primary/10 p-3"><MessageCircle className="h-7 w-7 text-primary" /></div><h1 className="text-4xl font-black tracking-tight">Topluluk tartışmaları</h1><p className="mt-3 max-w-2xl text-muted-foreground">Fikir sor, deneyim paylaş, teknik bir konuyu çöz veya yeni iş birlikleri kur.</p></section>
    <CommunityFeed />
  </main>;
}
