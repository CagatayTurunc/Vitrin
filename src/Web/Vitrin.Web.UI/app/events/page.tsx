import { Calendar, MapPin, Users, ArrowRight, Clock } from "lucide-react";

export const metadata = {
  title: "Etkinlikler — Vitrin",
  description: "Vitrin topluluğunun düzenlediği etkinlikler, buluşmalar ve online söyleşiler.",
};

const upcoming = [
  {
    id: 1,
    title: "Vitrin Maker Buluşması #3",
    date: "20 Temmuz 2026",
    time: "14:00 - 17:00",
    location: "İstanbul, Levent",
    type: "Yüz yüze",
    attendees: 48,
    desc: "İstanbul'daki maker'ları bir araya getiren aylık buluşmamız. Ürün sunumları, networking ve sohbet.",
  },
  {
    id: 2,
    title: "Online: Ürün Lansmanı Masterclass",
    date: "25 Temmuz 2026",
    time: "20:00 - 21:30",
    location: "Online (Zoom)",
    type: "Online",
    attendees: 120,
    desc: "Vitrin'de başarılı lansmanlar yapan maker'lardan ipuçları. Canlı Q&A seansı dahil.",
  },
  {
    id: 3,
    title: "Ankara Tech Buluşması",
    date: "2 Ağustos 2026",
    time: "13:00 - 16:00",
    location: "Ankara, Çankaya",
    type: "Yüz yüze",
    attendees: 32,
    desc: "Ankara'daki teknoloji meraklıları ve maker'lar için özel buluşma.",
  },
];

const past = [
  {
    title: "Vitrin Maker Buluşması #2",
    date: "15 Haziran 2026",
    location: "İstanbul",
    attendees: 42,
  },
  {
    title: "Online: SaaS Fikirleri Workshopu",
    date: "28 Mayıs 2026",
    location: "Online",
    attendees: 89,
  },
];

export default function EventsPage() {
  return (
    <main className="min-h-screen bg-background">
      {/* Header */}
      <div className="border-b border-border bg-muted/20">
        <div className="mx-auto max-w-4xl px-4 py-16">
          <h1 className="text-4xl font-extrabold text-foreground mb-3">Etkinlikler</h1>
          <p className="text-muted-foreground max-w-xl">
            Topluluğumuzla buluş, fikir paylaş ve maker ağını genişlet.
          </p>
        </div>
      </div>

      <div className="mx-auto max-w-4xl px-4 py-12">
        {/* Upcoming */}
        <h2 className="text-xl font-bold text-foreground mb-6 flex items-center gap-2">
          <Calendar className="w-5 h-5 text-emerald-500" /> Yaklaşan Etkinlikler
        </h2>

        <div className="space-y-4 mb-14">
          {upcoming.map((ev) => (
            <div
              key={ev.id}
              className="bg-card border border-border rounded-2xl p-6 hover:border-emerald-500/30 transition-colors group"
            >
              <div className="flex flex-col sm:flex-row gap-4 sm:items-start">
                {/* Date badge */}
                <div className="shrink-0 w-14 h-14 rounded-2xl bg-emerald-500/10 flex flex-col items-center justify-center text-emerald-600 dark:text-emerald-400 font-bold">
                  <span className="text-xs uppercase leading-tight">
                    {ev.date.split(" ")[1].slice(0, 3)}
                  </span>
                  <span className="text-xl leading-tight">{ev.date.split(" ")[0]}</span>
                </div>

                <div className="flex-1">
                  <div className="flex items-start justify-between gap-2 flex-wrap mb-1">
                    <h3 className="font-bold text-foreground group-hover:text-emerald-500 transition-colors">
                      {ev.title}
                    </h3>
                    <span
                      className={`text-xs font-medium px-2 py-0.5 rounded-full ${
                        ev.type === "Online"
                          ? "bg-blue-500/10 text-blue-500"
                          : "bg-emerald-500/10 text-emerald-500"
                      }`}
                    >
                      {ev.type}
                    </span>
                  </div>
                  <p className="text-sm text-muted-foreground mb-3">{ev.desc}</p>
                  <div className="flex items-center gap-4 text-xs text-muted-foreground flex-wrap">
                    <span className="flex items-center gap-1"><Clock className="w-3 h-3" />{ev.time}</span>
                    <span className="flex items-center gap-1"><MapPin className="w-3 h-3" />{ev.location}</span>
                    <span className="flex items-center gap-1"><Users className="w-3 h-3" />{ev.attendees} katılımcı</span>
                  </div>
                </div>

                <button className="shrink-0 self-end sm:self-center px-4 py-2 rounded-full bg-emerald-500 hover:bg-emerald-600 text-white text-sm font-semibold transition-colors flex items-center gap-1">
                  Katıl <ArrowRight className="w-3 h-3" />
                </button>
              </div>
            </div>
          ))}
        </div>

        {/* Past */}
        <h2 className="text-xl font-bold text-foreground mb-6 flex items-center gap-2">
          <Clock className="w-5 h-5 text-muted-foreground" /> Geçmiş Etkinlikler
        </h2>

        <div className="space-y-3">
          {past.map((ev) => (
            <div
              key={ev.title}
              className="flex items-center justify-between bg-card border border-border rounded-xl p-4 opacity-70"
            >
              <div>
                <p className="font-medium text-foreground text-sm">{ev.title}</p>
                <div className="flex gap-3 text-xs text-muted-foreground mt-0.5">
                  <span>{ev.date}</span>
                  <span>•</span>
                  <span>{ev.location}</span>
                  <span>•</span>
                  <span>{ev.attendees} katılımcı</span>
                </div>
              </div>
              <span className="text-xs text-muted-foreground">Tamamlandı</span>
            </div>
          ))}
        </div>
      </div>
    </main>
  );
}
