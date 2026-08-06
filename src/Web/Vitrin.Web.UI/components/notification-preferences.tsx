"use client";

import { useEffect, useState } from "react";
import { useSession } from "next-auth/react";
import { BellRing, MessageCircle, AtSign, Heart, Users, ShieldCheck, Send, Loader2, Package, BookmarkPlus, Moon } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";

const API_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";

type DigestFrequency = "off" | "daily" | "weekly";

interface NotificationPreferences {
  emailAddress: string;
  inAppEnabled: boolean;
  emailEnabled: boolean;
  digestFrequency: DigestFrequency;
  productUpdatesEnabled: boolean;
  commentsEnabled: boolean;
  mentionsEnabled: boolean;
  reactionsEnabled: boolean;
  socialEnabled: boolean;
  moderationEnabled: boolean;
  quietHoursEnabled: boolean;
  productTrackingEnabled: boolean;
  collectionTrackingEnabled: boolean;
  lastDigestSentAtUtc?: string | null;
}

const defaultPreferences: NotificationPreferences = {
  emailAddress: "",
  inAppEnabled: true,
  emailEnabled: false,
  digestFrequency: "weekly",
  productUpdatesEnabled: true,
  commentsEnabled: true,
  mentionsEnabled: true,
  reactionsEnabled: true,
  socialEnabled: true,
  moderationEnabled: true,
  quietHoursEnabled: false,
  productTrackingEnabled: true,
  collectionTrackingEnabled: true,
};

type BooleanPreferenceKey = {
  [Key in keyof NotificationPreferences]-?: NotificationPreferences[Key] extends boolean ? Key : never
}[keyof NotificationPreferences];

const categoryOptions: Array<{
  key: BooleanPreferenceKey;
  title: string;
  description: string;
  icon: typeof BellRing;
}> = [
  { key: "productUpdatesEnabled", title: "Ürün güncellemeleri", description: "Onay, red, saved search ve topic eşleşmeleri", icon: BellRing },
  { key: "commentsEnabled", title: "Yorumlar", description: "Ürünlerine gelen yorum ve yanıtlar", icon: MessageCircle },
  { key: "mentionsEnabled", title: "Mention'lar", description: "Birisi senden bahsettiğinde", icon: AtSign },
  { key: "reactionsEnabled", title: "Reaksiyonlar", description: "Yorumlarına gelen tepkiler", icon: Heart },
  { key: "socialEnabled", title: "Sosyal", description: "Takip ve topluluk hareketleri", icon: Users },
  { key: "moderationEnabled", title: "Moderasyon", description: "Ban, itiraz ve hesap güvenliği kararları", icon: ShieldCheck },
];

const trackingOptions: Array<{
  key: BooleanPreferenceKey;
  title: string;
  description: string;
  icon: typeof BellRing;
}> = [
  { key: "productTrackingEnabled", title: "Ürün Takibi", description: "Takip ettiğin ürünlerin yeni lansmanları", icon: Package },
  { key: "collectionTrackingEnabled", title: "Koleksiyon Takibi", description: "Takip ettiğin koleksiyonlara eklenen ürünler", icon: BookmarkPlus },
];

function Toggle({ checked, onChange, label }: { checked: boolean; onChange: (checked: boolean) => void; label: string }) {
  return (
    <button
      type="button"
      role="switch"
      aria-checked={checked}
      aria-label={label}
      onClick={() => onChange(!checked)}
      className={`relative h-6 w-11 shrink-0 rounded-full border transition-colors ${
        checked ? "border-emerald-500 bg-emerald-500" : "border-border bg-muted"
      }`}
    >
      <span
        className={`absolute top-0.5 h-4.5 w-4.5 rounded-full bg-white shadow-sm transition-transform ${
          checked ? "translate-x-5" : "translate-x-0.5"
        }`}
      />
    </button>
  );
}

export function NotificationPreferences({ initialEmail }: { initialEmail?: string }) {
  const { data: session } = useSession();
  const [preferences, setPreferences] = useState<NotificationPreferences>({
    ...defaultPreferences,
    emailAddress: initialEmail ?? "",
  });
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [isSending, setIsSending] = useState(false);
  const [status, setStatus] = useState<{ kind: "success" | "error"; text: string } | null>(null);
  const accessToken = session?.accessToken;

  useEffect(() => {
    if (!accessToken) return;

    const controller = new AbortController();

    void (async () => {
      try {
        const response = await fetch(`${API_URL}/api/notifications/preferences`, {
          headers: { Authorization: `Bearer ${accessToken}` },
          cache: "no-store",
          signal: controller.signal,
        });

        if (!response.ok) throw new Error("Bildirim tercihleri alınamadı.");
        const data = await response.json() as NotificationPreferences;
        setPreferences({
          ...defaultPreferences,
          ...data,
          emailAddress: data.emailAddress || initialEmail || "",
        });
      } catch (error) {
        if (!controller.signal.aborted) {
          setStatus({ kind: "error", text: error instanceof Error ? error.message : "Tercihler yüklenemedi." });
        }
      } finally {
        if (!controller.signal.aborted) setIsLoading(false);
      }
    })();

    return () => controller.abort();
  }, [accessToken, initialEmail]);

  const updateBoolean = (key: BooleanPreferenceKey, value: boolean) => {
    setPreferences((current) => ({ ...current, [key]: value }));
  };

  const save = async () => {
    if (!accessToken) return;
    if (preferences.emailEnabled && !preferences.emailAddress.trim()) {
      setStatus({ kind: "error", text: "E-posta özeti için bir e-posta adresi girin." });
      return;
    }

    setIsSaving(true);
    setStatus(null);
    try {
      const response = await fetch(`${API_URL}/api/notifications/preferences`, {
        method: "PUT",
        headers: {
          Authorization: `Bearer ${accessToken}`,
          "Content-Type": "application/json",
        },
        body: JSON.stringify(preferences),
      });

      if (!response.ok) throw new Error("Tercihler kaydedilemedi.");
      const data = await response.json() as NotificationPreferences;
      setPreferences((current) => ({ ...current, ...data, emailAddress: data.emailAddress || current.emailAddress }));
      setStatus({ kind: "success", text: "Bildirim tercihlerin kaydedildi." });
    } catch (error) {
      setStatus({ kind: "error", text: error instanceof Error ? error.message : "Tercihler kaydedilemedi." });
    } finally {
      setIsSaving(false);
    }
  };

  const sendDigestNow = async () => {
    if (!accessToken) return;
    setIsSending(true);
    setStatus(null);
    try {
      const response = await fetch(`${API_URL}/api/notifications/digest/send-now`, {
        method: "POST",
        headers: { Authorization: `Bearer ${accessToken}` },
      });
      if (!response.ok) throw new Error("E-posta özeti gönderilemedi.");
      const data = await response.json() as { notificationCount: number };
      setStatus({
        kind: "success",
        text: data.notificationCount > 0
          ? `${data.notificationCount} bildirimi içeren özet gönderildi.`
          : "Özete eklenecek yeni bildirim bulunamadı.",
      });
    } catch (error) {
      setStatus({ kind: "error", text: error instanceof Error ? error.message : "E-posta özeti gönderilemedi." });
    } finally {
      setIsSending(false);
    }
  };

  return (
    <Card className="mb-8 overflow-hidden border-border/70 bg-gradient-to-br from-card via-card to-emerald-500/5 shadow-sm">
      <CardHeader className="border-b border-border/60">
        <div className="flex items-start justify-between gap-4">
          <div>
            <CardTitle className="flex items-center gap-2 text-xl">
              <BellRing className="h-5 w-5 text-emerald-500" />
              Bildirim merkezi
            </CardTitle>
            <CardDescription className="mt-2">
              Hangi gelişmeleri anında, hangilerini e-posta özeti olarak almak istediğini seç.
            </CardDescription>
          </div>
          {isLoading && <Loader2 className="h-5 w-5 animate-spin text-muted-foreground" />}
        </div>
      </CardHeader>
      <CardContent className="space-y-7 p-6">
        <div className="grid gap-4 md:grid-cols-2">
          <div className="flex items-center justify-between rounded-xl border border-border/70 bg-background/60 p-4">
            <div className="pr-4">
              <p className="font-medium">Anlık bildirimler</p>
              <p className="mt-1 text-xs text-muted-foreground">Yeni bildirimler sayfayı yenilemeden gelir.</p>
            </div>
            <Toggle
              checked={preferences.inAppEnabled}
              onChange={(checked) => updateBoolean("inAppEnabled", checked)}
              label="Anlık bildirimler"
            />
          </div>
          <div className="flex items-center justify-between rounded-xl border border-border/70 bg-background/60 p-4">
            <div className="pr-4">
              <p className="font-medium">E-posta özeti</p>
              <p className="mt-1 text-xs text-muted-foreground">Bildirimleri tek ve düzenli bir e-postada topla.</p>
            </div>
            <Toggle
              checked={preferences.emailEnabled}
              onChange={(checked) => setPreferences((current) => ({
                ...current,
                emailEnabled: checked,
                digestFrequency: checked && current.digestFrequency === "off" ? "weekly" : current.digestFrequency,
              }))}
              label="E-posta özeti"
            />
          </div>
        </div>

        {preferences.emailEnabled && (
          <div className="grid gap-4 rounded-xl border border-emerald-500/20 bg-emerald-500/5 p-4 md:grid-cols-[1fr_180px_auto] md:items-end">
            <label className="space-y-2 text-sm font-medium">
              E-posta adresi
              <Input
                type="email"
                value={preferences.emailAddress}
                onChange={(event) => setPreferences((current) => ({ ...current, emailAddress: event.target.value }))}
                placeholder="sen@ornek.com"
              />
            </label>
            <label className="space-y-2 text-sm font-medium">
              Gönderim sıklığı
              <select
                value={preferences.digestFrequency}
                onChange={(event) => setPreferences((current) => ({ ...current, digestFrequency: event.target.value as DigestFrequency }))}
                className="h-8 w-full rounded-lg border border-input bg-background px-2.5 text-sm outline-none focus:border-ring focus:ring-3 focus:ring-ring/50"
              >
                <option value="off">Kapalı</option>
                <option value="daily">Günlük</option>
                <option value="weekly">Haftalık</option>
              </select>
            </label>
            <Button type="button" variant="outline" onClick={sendDigestNow} disabled={isSending || isLoading}>
              {isSending ? <Loader2 className="animate-spin" /> : <Send />}
              Şimdi gönder
            </Button>
          </div>
        )}

        <div>
          <h3 className="mb-3 text-sm font-semibold uppercase tracking-wider text-muted-foreground">Bildirim türleri</h3>
          <div className="grid gap-3 md:grid-cols-2">
            {categoryOptions.map((option) => {
              const Icon = option.icon;
              return (
                <div key={option.key} className="flex items-center gap-3 rounded-xl border border-border/60 p-3.5 transition-colors hover:bg-muted/30">
                  <div className="rounded-lg bg-muted p-2 text-foreground"><Icon className="h-4 w-4" /></div>
                  <div className="min-w-0 flex-1">
                    <p className="text-sm font-medium">{option.title}</p>
                    <p className="truncate text-xs text-muted-foreground">{option.description}</p>
                  </div>
                  <Toggle
                    checked={preferences[option.key]}
                    onChange={(checked) => updateBoolean(option.key, checked)}
                    label={option.title}
                  />
                </div>
              );
            })}
          </div>
          </div>
        </div>

        <div>
          <h3 className="mb-3 mt-4 text-sm font-semibold uppercase tracking-wider text-muted-foreground">İçerik Takibi</h3>
          <div className="grid gap-3 md:grid-cols-2">
            {trackingOptions.map((option) => {
              const Icon = option.icon;
              return (
                <div key={option.key} className="flex items-center gap-3 rounded-xl border border-border/60 p-3.5 transition-colors hover:bg-muted/30">
                  <div className="rounded-lg bg-muted p-2 text-foreground"><Icon className="h-4 w-4" /></div>
                  <div className="min-w-0 flex-1">
                    <p className="text-sm font-medium">{option.title}</p>
                    <p className="truncate text-xs text-muted-foreground">{option.description}</p>
                  </div>
                  <Toggle
                    checked={preferences[option.key]}
                    onChange={(checked) => updateBoolean(option.key, checked)}
                    label={option.title}
                  />
                </div>
              );
            })}
          </div>
        </div>

        <div>
          <h3 className="mb-3 mt-4 text-sm font-semibold uppercase tracking-wider text-muted-foreground">Rahatsız Etme</h3>
          <div className="flex items-center justify-between rounded-xl border border-border/70 bg-background/60 p-4">
            <div className="flex items-center gap-3">
              <div className="rounded-lg bg-indigo-500/10 p-2 text-indigo-500"><Moon className="h-4 w-4" /></div>
              <div className="pr-4">
                <p className="font-medium">Sessiz Saatler (22:00 - 08:00)</p>
                <p className="mt-1 text-xs text-muted-foreground">Bu saatler arasında bildirim gönderilmez, sabah toplu olarak iletilir.</p>
              </div>
            </div>
            <Toggle
              checked={preferences.quietHoursEnabled}
              onChange={(checked) => updateBoolean("quietHoursEnabled", checked)}
              label="Sessiz Saatler"
            />
          </div>
        </div>

        <div className="flex flex-col gap-3 border-t border-border/60 pt-5 sm:flex-row sm:items-center sm:justify-between">
          <div className={`text-sm ${status?.kind === "error" ? "text-red-500" : "text-emerald-500"}`} role="status">
            {status?.text}
          </div>
          <Button type="button" onClick={save} disabled={isSaving || isLoading} className="sm:min-w-36">
            {isSaving && <Loader2 className="animate-spin" />}
            Tercihleri kaydet
          </Button>
        </div>
      </CardContent>
    </Card>
  );
}
