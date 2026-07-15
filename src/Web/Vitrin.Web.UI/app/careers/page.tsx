import { MapPin, ArrowRight, Sparkles } from "lucide-react";
import Link from "next/link";

export const metadata = {
  title: "Kariyer — Vitrin",
  description: "Vitrin ekibine katıl. Açık pozisyonlar ve başvuru formu.",
};

const openings = [
  {
    title: "Senior Frontend Geliştirici",
    type: "Tam Zamanlı",
    location: "İstanbul / Remote",
    dept: "Mühendislik",
  },
  {
    title: "Backend Geliştirici (.NET)",
    type: "Tam Zamanlı",
    location: "Remote",
    dept: "Mühendislik",
  },
  {
    title: "Ürün Tasarımcısı",
    type: "Tam Zamanlı",
    location: "İstanbul / Remote",
    dept: "Tasarım",
  },
  {
    title: "İçerik & Topluluk Yöneticisi",
    type: "Part-time",
    location: "Remote",
    dept: "Topluluk",
  },
];

const perks = [
  "Tam remote veya hibrit çalışma",
  "Esnek çalışma saatleri",
  "Ekip retreatları",
  "Kitap & kurs bütçesi",
  "Modern ekipman desteği",
  "Hisse senedi opsiyonu",
];

export default function CareersPage() {
  return (
    <main className="min-h-screen bg-background">
      {/* Hero */}
      <div className="relative border-b border-border overflow-hidden">
        <div className="absolute inset-0 bg-gradient-to-br from-emerald-500/5 to-transparent pointer-events-none" />
        <div className="mx-auto max-w-4xl px-4 py-20 text-center">
          <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-emerald-500/10 border border-emerald-500/20 text-emerald-600 dark:text-emerald-400 text-sm font-medium mb-6">
            <Sparkles className="w-4 h-4" />
            {openings.length} Açık Pozisyon
          </div>
          <h1 className="text-4xl sm:text-5xl font-extrabold text-foreground mb-4">
            Vitrin&apos;de Kariyer
          </h1>
          <p className="text-lg text-muted-foreground max-w-xl mx-auto">
            Türkiye&apos;nin ürün ekosistemini inşa eden küçük, tutkulu ekibe katıl.
          </p>
        </div>
      </div>

      <div className="mx-auto max-w-4xl px-4 py-16">
        {/* Perks */}
        <div className="bg-card border border-border rounded-3xl p-8 mb-12">
          <h2 className="text-xl font-bold text-foreground mb-6">Neden Vitrin?</h2>
          <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-4">
            {perks.map((perk) => (
              <div key={perk} className="flex items-center gap-3 text-sm text-muted-foreground">
                <div className="w-5 h-5 rounded-full bg-emerald-500/10 flex items-center justify-center shrink-0">
                  <div className="w-2 h-2 rounded-full bg-emerald-500" />
                </div>
                {perk}
              </div>
            ))}
          </div>
        </div>

        {/* Openings */}
        <h2 className="text-xl font-bold text-foreground mb-6">Açık Pozisyonlar</h2>
        <div className="space-y-4">
          {openings.map((job) => (
            <div
              key={job.title}
              className="bg-card border border-border rounded-2xl p-6 flex flex-col sm:flex-row sm:items-center justify-between gap-4 hover:border-emerald-500/30 transition-colors group"
            >
              <div>
                <div className="flex items-center gap-2 mb-1">
                  <span className="text-xs font-medium text-emerald-500 bg-emerald-500/10 px-2 py-0.5 rounded-full">
                    {job.dept}
                  </span>
                  <span className="text-xs text-muted-foreground">{job.type}</span>
                </div>
                <h3 className="font-bold text-foreground group-hover:text-emerald-500 transition-colors">
                  {job.title}
                </h3>
                <div className="flex items-center gap-1 text-xs text-muted-foreground mt-1">
                  <MapPin className="w-3 h-3" />
                  {job.location}
                </div>
              </div>
              <Link
                href={`/contact`}
                className="flex items-center gap-2 text-sm font-semibold text-emerald-500 hover:gap-3 transition-all shrink-0"
              >
                Başvur <ArrowRight className="w-4 h-4" />
              </Link>
            </div>
          ))}
        </div>

        {/* CTA */}
        <div className="mt-12 text-center text-muted-foreground text-sm">
          Uygun pozisyon bulamadın?{" "}
          <Link href="/contact" className="text-emerald-500 hover:underline font-medium">
            Açık başvuru gönder
          </Link>
        </div>
      </div>
    </main>
  );
}
