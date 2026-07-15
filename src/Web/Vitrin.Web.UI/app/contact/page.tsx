"use client";

import { useState } from "react";
import { AtSign, BriefcaseBusiness, Code2, Mail, MessageSquare, MapPin, Send } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";

export default function ContactPage() {
  const [form, setForm] = useState({ name: "", email: "", subject: "", message: "" });
  const [sent, setSent] = useState(false);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setSent(true);
  };

  return (
    <main className="min-h-screen bg-background">
      <div className="mx-auto max-w-5xl px-4 py-16 sm:py-24">
        {/* Header */}
        <div className="text-center mb-16">
          <h1 className="text-4xl sm:text-5xl font-extrabold text-foreground mb-4">İletişim</h1>
          <p className="text-lg text-muted-foreground max-w-xl mx-auto">
            Soru, öneri veya iş birliği için bize ulaşın. En kısa sürede geri döneceğiz.
          </p>
        </div>

        <div className="grid md:grid-cols-5 gap-12">
          {/* Info */}
          <div className="md:col-span-2 space-y-8">
            <div>
              <h2 className="text-lg font-bold text-foreground mb-6">Bize Ulaşın</h2>
              <div className="space-y-5">
                <div className="flex items-start gap-4">
                  <div className="w-10 h-10 rounded-xl bg-emerald-500/10 flex items-center justify-center shrink-0">
                    <Mail className="w-5 h-5 text-emerald-500" />
                  </div>
                  <div>
                    <p className="text-sm font-medium text-foreground">E-posta</p>
                    <p className="text-sm text-muted-foreground">hello@vitrin.app</p>
                  </div>
                </div>
                <div className="flex items-start gap-4">
                  <div className="w-10 h-10 rounded-xl bg-emerald-500/10 flex items-center justify-center shrink-0">
                    <MessageSquare className="w-5 h-5 text-emerald-500" />
                  </div>
                  <div>
                    <p className="text-sm font-medium text-foreground">Destek</p>
                    <p className="text-sm text-muted-foreground">support@vitrin.app</p>
                  </div>
                </div>
                <div className="flex items-start gap-4">
                  <div className="w-10 h-10 rounded-xl bg-emerald-500/10 flex items-center justify-center shrink-0">
                    <MapPin className="w-5 h-5 text-emerald-500" />
                  </div>
                  <div>
                    <p className="text-sm font-medium text-foreground">Konum</p>
                    <p className="text-sm text-muted-foreground">İstanbul, Türkiye</p>
                  </div>
                </div>
              </div>
            </div>

            <div>
              <h3 className="text-sm font-semibold text-foreground mb-4">Sosyal Medya</h3>
              <div className="flex gap-3">
                {[
                  { icon: AtSign, label: "Twitter", href: "#" },
                  { icon: Code2, label: "GitHub", href: "#" },
                  { icon: BriefcaseBusiness, label: "LinkedIn", href: "#" },
                ].map(({ icon: Icon, label, href }) => (
                  <a
                    key={label}
                    href={href}
                    target="_blank"
                    rel="noreferrer"
                    className="w-10 h-10 rounded-xl border border-border flex items-center justify-center text-muted-foreground hover:text-emerald-500 hover:border-emerald-500/30 transition-colors"
                  >
                    <Icon className="w-4 h-4" />
                  </a>
                ))}
              </div>
            </div>
          </div>

          {/* Form */}
          <div className="md:col-span-3">
            {sent ? (
              <div className="h-full flex flex-col items-center justify-center text-center p-12 bg-card border border-emerald-500/20 rounded-3xl">
                <div className="w-16 h-16 rounded-full bg-emerald-500/10 flex items-center justify-center mb-4">
                  <Send className="w-7 h-7 text-emerald-500" />
                </div>
                <h3 className="text-xl font-bold text-foreground mb-2">Mesajınız İletildi!</h3>
                <p className="text-muted-foreground">En kısa sürede size geri döneceğiz.</p>
              </div>
            ) : (
              <div className="bg-card border border-border rounded-3xl p-8">
                <form onSubmit={handleSubmit} className="space-y-5">
                  <div className="grid sm:grid-cols-2 gap-5">
                    <div className="space-y-2">
                      <label className="text-sm font-medium text-foreground">Ad Soyad</label>
                      <Input
                        required
                        placeholder="Adınız"
                        value={form.name}
                        onChange={(e) => setForm({ ...form, name: e.target.value })}
                        className="bg-background"
                      />
                    </div>
                    <div className="space-y-2">
                      <label className="text-sm font-medium text-foreground">E-posta</label>
                      <Input
                        required
                        type="email"
                        placeholder="eposta@ornek.com"
                        value={form.email}
                        onChange={(e) => setForm({ ...form, email: e.target.value })}
                        className="bg-background"
                      />
                    </div>
                  </div>
                  <div className="space-y-2">
                    <label className="text-sm font-medium text-foreground">Konu</label>
                    <Input
                      required
                      placeholder="Mesajınızın konusu"
                      value={form.subject}
                      onChange={(e) => setForm({ ...form, subject: e.target.value })}
                      className="bg-background"
                    />
                  </div>
                  <div className="space-y-2">
                    <label className="text-sm font-medium text-foreground">Mesaj</label>
                    <Textarea
                      required
                      rows={5}
                      placeholder="Mesajınızı buraya yazın..."
                      value={form.message}
                      onChange={(e) => setForm({ ...form, message: e.target.value })}
                      className="bg-background resize-none"
                    />
                  </div>
                  <Button
                    type="submit"
                    className="w-full rounded-full bg-emerald-500 hover:bg-emerald-600 text-white font-semibold h-12"
                  >
                    Gönder <Send className="ml-2 w-4 h-4" />
                  </Button>
                </form>
              </div>
            )}
          </div>
        </div>
      </div>
    </main>
  );
}
