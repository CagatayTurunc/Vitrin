"use client";

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Package, Users, Eye, Activity } from "lucide-react";

export default function AdminDashboard() {
  const stats = [
    { title: "Toplam Ürün", value: "0", icon: Package, desc: "+0 dünden beri" },
    { title: "Toplam Kullanıcı", value: "1", icon: Users, desc: "+1 dünden beri" },
    { title: "Görüntülenme", value: "12,234", icon: Eye, desc: "+19% geçen aya göre" },
    { title: "Aktif Oturum", value: "573", icon: Activity, desc: "+201 şu an aktif" },
  ];

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Dashboard</h1>
        <p className="text-muted-foreground">Platformun genel istatistikleri ve özet görünümü.</p>
      </div>

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        {stats.map((stat, i) => (
          <Card key={i}>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">{stat.title}</CardTitle>
              <stat.icon className="h-4 w-4 text-muted-foreground" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{stat.value}</div>
              <p className="text-xs text-muted-foreground">{stat.desc}</p>
            </CardContent>
          </Card>
        ))}
      </div>

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-7">
        <Card className="col-span-4">
          <CardHeader>
            <CardTitle>Genel Bakış</CardTitle>
            <CardDescription>Son 30 günlük ürün ekleme ve onay metrikleri.</CardDescription>
          </CardHeader>
          <CardContent className="h-[300px] flex items-center justify-center text-muted-foreground border-t bg-muted/10 rounded-b-xl">
            Grafik verileri henüz hazır değil.
          </CardContent>
        </Card>
        
        <Card className="col-span-3">
          <CardHeader>
            <CardTitle>Son Kullanıcılar</CardTitle>
            <CardDescription>Sisteme yeni kayıt olan kullanıcılar.</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="space-y-8">
              <div className="flex items-center">
                <div className="ml-4 space-y-1">
                  <p className="text-sm font-medium leading-none">Test User</p>
                  <p className="text-sm text-muted-foreground">test@example.com</p>
                </div>
                <div className="ml-auto font-medium text-xs text-muted-foreground">Şimdi</div>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
