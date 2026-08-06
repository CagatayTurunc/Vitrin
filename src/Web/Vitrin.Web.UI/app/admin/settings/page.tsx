import Link from "next/link";
import { Bell, Flag, LockKeyhole, ShieldCheck, SlidersHorizontal } from "lucide-react";

const settings = [
  { title: "Feature flags", description: "Özellikleri kullanıcı, rol veya yüzde bazında kontrollü aç.", href: "/admin/feature-flags", icon: Flag },
  { title: "KVKK yönetimi", description: "Silme talepleri, saklama süreleri ve dışa aktarımları yönet.", href: "/admin/kvkk", icon: ShieldCheck },
  { title: "Moderasyon", description: "Rapor, yaptırım, itiraz ve denetim kayıtlarını incele.", href: "/admin/moderation", icon: LockKeyhole },
  { title: "Bildirim tercihleri", description: "Kendi admin hesabının e-posta ve uygulama içi bildirimlerini düzenle.", href: "/settings", icon: Bell },
];

export default function AdminSettingsPage() {
  return <div className="space-y-7"><div><div className="flex items-center gap-2 text-emerald-700"><SlidersHorizontal className="h-5 w-5" /><span className="text-sm font-bold uppercase tracking-[0.15em]">Kontrol merkezi</span></div><h1 className="mt-2 text-3xl font-bold">Admin ayarları</h1><p className="mt-1 text-muted-foreground">Operasyonel ayarlar ilgili güvenli yönetim ekranlarına ayrılmıştır.</p></div><div className="grid gap-4 md:grid-cols-2">{settings.map((setting) => <Link key={setting.href} href={setting.href} className="group rounded-3xl border bg-card p-6 transition hover:border-emerald-500/50 hover:shadow-md"><setting.icon className="h-6 w-6 text-emerald-600" /><h2 className="mt-4 text-xl font-bold group-hover:text-emerald-700">{setting.title}</h2><p className="mt-2 text-sm leading-6 text-muted-foreground">{setting.description}</p></Link>)}</div></div>;
}
