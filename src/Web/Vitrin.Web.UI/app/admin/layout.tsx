"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { LayoutDashboard, Package, Users, Settings, LogOut, Search } from "lucide-react";
import { Input } from "@/components/ui/input";

export default function AdminLayout({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();

  const navigation = [
    { name: "Dashboard", href: "/admin", icon: LayoutDashboard },
    { name: "Ürünler", href: "/admin/products", icon: Package },
    { name: "Maker Başvuruları", href: "/admin/maker-requests", icon: Users },
    { name: "Kullanıcılar", href: "/admin/users", icon: Users },
    { name: "Ayarlar", href: "/admin/settings", icon: Settings },
  ];

  return (
    <div className="flex min-h-screen bg-muted/20">
      {/* Sidebar */}
      <aside className="fixed inset-y-0 left-0 z-50 w-64 border-r bg-background hidden md:block">
        <div className="flex h-16 items-center border-b px-6">
          <Link href="/admin" className="flex items-center gap-2 font-bold text-xl tracking-tight">
            <span className="h-6 w-6 rounded-md bg-[#007A52] text-white flex items-center justify-center text-xs">V</span>
            Vitrin Admin
          </Link>
        </div>
        <div className="flex flex-col gap-2 p-4">
          <div className="text-xs font-semibold text-muted-foreground uppercase tracking-wider mb-2 px-2">Menu</div>
          {navigation.map((item) => {
            const isActive = pathname === item.href;
            return (
              <Link
                key={item.name}
                href={item.href}
                className={`flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition-colors ${
                  isActive
                    ? "bg-[#007A52]/10 text-[#007A52]"
                    : "text-muted-foreground hover:bg-muted hover:text-foreground"
                }`}
              >
                <item.icon className="h-4 w-4" />
                {item.name}
              </Link>
            );
          })}
        </div>
      </aside>

      {/* Main Content */}
      <main className="flex-1 md:pl-64">
        <header className="sticky top-0 z-40 flex h-16 items-center justify-between border-b bg-background px-6 backdrop-blur-sm">
          <div className="flex flex-1 items-center gap-4">
            <div className="relative w-full max-w-sm">
              <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
              <Input
                type="search"
                placeholder="Ara..."
                className="w-full rounded-lg bg-muted/50 pl-9 border-none h-9"
              />
            </div>
          </div>
          <div className="flex items-center gap-4">
            <Link href="/" className="text-sm font-medium text-muted-foreground hover:text-foreground hidden sm:block">
              Siteye Dön
            </Link>
            <div className="h-8 w-8 rounded-full bg-muted flex items-center justify-center">
              <LogOut className="h-4 w-4 text-muted-foreground" />
            </div>
          </div>
        </header>
        <div className="p-6">{children}</div>
      </main>
    </div>
  );
}
