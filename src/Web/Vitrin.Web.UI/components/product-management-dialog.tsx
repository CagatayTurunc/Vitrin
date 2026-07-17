"use client";

import { useCallback, useMemo, useState } from "react";
import { CalendarClock, History, Settings2, Trash2, UserRoundCheck, Users } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { getApiProblemMessage } from "@/lib/errors";

export interface ManageableProduct {
  id: string;
  name: string;
  status: number;
  scheduledLaunchAt: string | null;
  isOwner: boolean;
  teamRole: number | null;
}

interface Revision {
  id: string;
  revisionNumber: number;
  changedByUsername: string;
  changeType: string;
  summary: string | null;
  name: string;
  tagline: string;
  description: string;
  status: number;
  scheduledLaunchAt: string | null;
  createdAt: string;
}

interface TeamMember {
  userId: string;
  role: number;
  addedAt: string;
}

interface TeamResponse {
  ownerUserId: string;
  members: TeamMember[];
}

interface UserProfile {
  id: string;
  username: string;
  fullName: string;
  avatarUrl?: string | null;
}

interface Props {
  product: ManageableProduct;
  accessToken: string;
  onUpdated: () => Promise<void> | void;
}

type Section = "schedule" | "team" | "history";

const STATUS_NAMES: Record<number, string> = {
  0: "Taslak",
  1: "İncelemede",
  2: "Yayında",
  3: "Reddedildi",
  4: "Arşivlendi",
  5: "Planlandı",
};

function toLocalInputValue(iso: string | null) {
  if (!iso) return "";
  const date = new Date(iso);
  const offset = date.getTimezoneOffset() * 60_000;
  return new Date(date.getTime() - offset).toISOString().slice(0, 16);
}

export function ProductManagementDialog({ product, accessToken, onUpdated }: Props) {
  const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? "";
  const [open, setOpen] = useState(false);
  const [section, setSection] = useState<Section>("schedule");
  const [revisions, setRevisions] = useState<Revision[]>([]);
  const [team, setTeam] = useState<TeamResponse | null>(null);
  const [profiles, setProfiles] = useState<Record<string, UserProfile>>({});
  const [scheduledAt, setScheduledAt] = useState(toLocalInputValue(product.scheduledLaunchAt));
  const [minimumSchedule, setMinimumSchedule] = useState("");
  const [memberUsername, setMemberUsername] = useState("");
  const [memberRole, setMemberRole] = useState(1);
  const [isLoading, setIsLoading] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const canEdit = product.isOwner || product.teamRole === 1;

  const headers = useMemo(() => ({ Authorization: `Bearer ${accessToken}` }), [accessToken]);

  const parseFailure = async (response: Response, fallback: string) => {
    const payload: unknown = await response.json().catch(() => null);
    return getApiProblemMessage(payload, fallback);
  };

  const fetchManagementData = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      const [revisionResponse, teamResponse] = await Promise.all([
        fetch(`${apiUrl}/api/products/${product.id}/revisions`, { headers }),
        fetch(`${apiUrl}/api/products/${product.id}/team`, { headers }),
      ]);
      if (!revisionResponse.ok) throw new Error(await parseFailure(revisionResponse, "Geçmiş yüklenemedi."));
      if (!teamResponse.ok) throw new Error(await parseFailure(teamResponse, "Takım yüklenemedi."));

      const revisionData = await revisionResponse.json() as Revision[];
      const teamData = await teamResponse.json() as TeamResponse;
      setRevisions(revisionData);
      setTeam(teamData);

      const userIds = [teamData.ownerUserId, ...teamData.members.map((member) => member.userId)];
      const uniqueIds = [...new Set(userIds)];
      const resolvedProfiles = await Promise.all(uniqueIds.map(async (userId) => {
        const response = await fetch(`${apiUrl}/api/auth/users/${userId}`, { headers });
        if (!response.ok) return null;
        return await response.json() as UserProfile;
      }));
      setProfiles(Object.fromEntries(
        resolvedProfiles.filter((profile): profile is UserProfile => profile !== null).map((profile) => [profile.id, profile]),
      ));
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "Yönetim bilgileri yüklenemedi.");
    } finally {
      setIsLoading(false);
    }
  }, [apiUrl, headers, product.id]);

  const handleOpenChange = (nextOpen: boolean) => {
    setOpen(nextOpen);
    if (!nextOpen) return;
    setScheduledAt(toLocalInputValue(product.scheduledLaunchAt));
    setMinimumSchedule(toLocalInputValue(new Date(Date.now() + 5 * 60_000).toISOString()));
    void fetchManagementData();
  };

  const saveSchedule = async () => {
    setIsSaving(true);
    setError(null);
    try {
      const response = await fetch(`${apiUrl}/api/products/${product.id}/schedule`, {
        method: "POST",
        headers: { ...headers, "Content-Type": "application/json" },
        body: JSON.stringify({ scheduledLaunchAt: scheduledAt ? new Date(scheduledAt).toISOString() : null }),
      });
      if (!response.ok) throw new Error(await parseFailure(response, "Yayın planı kaydedilemedi."));
      await onUpdated();
      await fetchManagementData();
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "Yayın planı kaydedilemedi.");
    } finally {
      setIsSaving(false);
    }
  };

  const addMember = async () => {
    const username = memberUsername.trim().replace(/^@/, "");
    if (!username) return;
    setIsSaving(true);
    setError(null);
    try {
      const profileResponse = await fetch(`${apiUrl}/api/auth/users/by-username/${encodeURIComponent(username)}`, { headers });
      if (!profileResponse.ok) throw new Error("Bu kullanıcı adıyla bir üye bulunamadı.");
      const profile = await profileResponse.json() as UserProfile;
      const response = await fetch(`${apiUrl}/api/products/${product.id}/team`, {
        method: "POST",
        headers: { ...headers, "Content-Type": "application/json" },
        body: JSON.stringify({ memberUserId: profile.id, role: memberRole }),
      });
      if (!response.ok) throw new Error(await parseFailure(response, "Takım üyesi eklenemedi."));
      setMemberUsername("");
      await fetchManagementData();
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "Takım üyesi eklenemedi.");
    } finally {
      setIsSaving(false);
    }
  };

  const updateMember = async (memberUserId: string, role: number) => {
    setIsSaving(true);
    setError(null);
    try {
      const response = await fetch(`${apiUrl}/api/products/${product.id}/team`, {
        method: "POST",
        headers: { ...headers, "Content-Type": "application/json" },
        body: JSON.stringify({ memberUserId, role }),
      });
      if (!response.ok) throw new Error(await parseFailure(response, "Rol güncellenemedi."));
      await fetchManagementData();
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "Rol güncellenemedi.");
    } finally {
      setIsSaving(false);
    }
  };

  const removeMember = async (memberUserId: string) => {
    setIsSaving(true);
    setError(null);
    try {
      const response = await fetch(`${apiUrl}/api/products/${product.id}/team/${memberUserId}`, {
        method: "DELETE",
        headers,
      });
      if (!response.ok) throw new Error(await parseFailure(response, "Takım üyesi çıkarılamadı."));
      await fetchManagementData();
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "Takım üyesi çıkarılamadı.");
    } finally {
      setIsSaving(false);
    }
  };

  const transferOwnership = async (newOwnerUserId: string) => {
    if (!window.confirm("Ürün sahipliğini bu takım üyesine devretmek istediğinize emin misiniz?")) return;
    setIsSaving(true);
    setError(null);
    try {
      const response = await fetch(`${apiUrl}/api/products/${product.id}/ownership/transfer`, {
        method: "POST",
        headers: { ...headers, "Content-Type": "application/json" },
        body: JSON.stringify({ newOwnerUserId }),
      });
      if (!response.ok) throw new Error(await parseFailure(response, "Sahiplik devredilemedi."));
      await onUpdated();
      setOpen(false);
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "Sahiplik devredilemedi.");
    } finally {
      setIsSaving(false);
    }
  };

  const profileLabel = (userId: string) => {
    const profile = profiles[userId];
    return profile ? `${profile.fullName} (@${profile.username})` : userId;
  };

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogTrigger asChild>
        <Button variant="outline" size="sm">
          <Settings2 className="mr-1.5 h-3.5 w-3.5" /> Yönet
        </Button>
      </DialogTrigger>
      <DialogContent className="max-h-[88vh] overflow-y-auto sm:max-w-2xl">
        <DialogHeader>
          <DialogTitle>{product.name} yönetimi</DialogTitle>
          <DialogDescription>Yayın zamanı, maker takımı ve ürün değişiklik geçmişi.</DialogDescription>
        </DialogHeader>

        <div className="flex flex-wrap gap-2 border-b pb-3">
          <Button size="sm" variant={section === "schedule" ? "default" : "ghost"} onClick={() => setSection("schedule")}>
            <CalendarClock className="mr-1.5 h-4 w-4" /> Planlı yayın
          </Button>
          <Button size="sm" variant={section === "team" ? "default" : "ghost"} onClick={() => setSection("team")}>
            <Users className="mr-1.5 h-4 w-4" /> Maker takımı
          </Button>
          <Button size="sm" variant={section === "history" ? "default" : "ghost"} onClick={() => setSection("history")}>
            <History className="mr-1.5 h-4 w-4" /> Revision geçmişi
          </Button>
        </div>

        {error && <div className="rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700 dark:border-red-800 dark:bg-red-950/30 dark:text-red-300">{error}</div>}
        {isLoading && <p className="py-8 text-center text-sm text-muted-foreground">Yükleniyor...</p>}

        {!isLoading && section === "schedule" && (
          <div className="space-y-4 py-2">
            <div className="space-y-2">
              <Label htmlFor={`schedule-${product.id}`}>Yayın tarihi ve saati</Label>
              <Input
                id={`schedule-${product.id}`}
                type="datetime-local"
                value={scheduledAt}
                disabled={!canEdit || product.status === 2 || product.status === 4}
                min={minimumSchedule}
                onChange={(event) => setScheduledAt(event.target.value)}
              />
              <p className="text-xs text-muted-foreground">
                İnceleme onaylanırsa ürün bu zamana kadar “Planlandı” durumunda kalır ve zamanı gelince otomatik yayınlanır.
              </p>
            </div>
            <Button onClick={saveSchedule} disabled={isSaving || !canEdit || product.status === 2 || product.status === 4}>
              {isSaving ? "Kaydediliyor..." : "Yayın planını kaydet"}
            </Button>
          </div>
        )}

        {!isLoading && section === "team" && team && (
          <div className="space-y-5 py-2">
            <div className="rounded-xl border bg-muted/30 p-3">
              <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Ürün sahibi</p>
              <p className="mt-1 text-sm font-semibold">{profileLabel(team.ownerUserId)}</p>
            </div>

            {product.isOwner && (
              <div className="grid gap-2 rounded-xl border p-3 sm:grid-cols-[1fr_130px_auto] sm:items-end">
                <div className="space-y-1.5">
                  <Label htmlFor={`member-${product.id}`}>Kullanıcı adı</Label>
                  <Input id={`member-${product.id}`} placeholder="@kullanici" value={memberUsername} onChange={(event) => setMemberUsername(event.target.value)} />
                </div>
                <div className="space-y-1.5">
                  <Label htmlFor={`role-${product.id}`}>Rol</Label>
                  <select id={`role-${product.id}`} value={memberRole} onChange={(event) => setMemberRole(Number(event.target.value))} className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm">
                    <option value={1}>Editör</option>
                    <option value={0}>Görüntüleyici</option>
                  </select>
                </div>
                <Button onClick={addMember} disabled={isSaving || !memberUsername.trim()}>Ekle</Button>
              </div>
            )}

            <div className="space-y-2">
              {team.members.length === 0 ? (
                <p className="rounded-xl border border-dashed p-6 text-center text-sm text-muted-foreground">Henüz takım üyesi yok.</p>
              ) : team.members.map((member) => (
                <div key={member.userId} className="flex flex-wrap items-center justify-between gap-3 rounded-xl border p-3">
                  <div>
                    <p className="text-sm font-medium">{profileLabel(member.userId)}</p>
                    <p className="text-xs text-muted-foreground">{member.role === 1 ? "Düzenleyebilir ve incelemeye gönderebilir" : "Yönetim bilgilerini görüntüleyebilir"}</p>
                  </div>
                  {product.isOwner && (
                    <div className="flex flex-wrap items-center gap-2">
                      <select value={member.role} disabled={isSaving} onChange={(event) => void updateMember(member.userId, Number(event.target.value))} className="h-8 rounded-md border border-input bg-background px-2 text-xs">
                        <option value={1}>Editör</option>
                        <option value={0}>Görüntüleyici</option>
                      </select>
                      <Button size="sm" variant="outline" disabled={isSaving} onClick={() => void transferOwnership(member.userId)}>
                        <UserRoundCheck className="mr-1 h-3.5 w-3.5" /> Sahipliği devret
                      </Button>
                      <Button size="icon" variant="ghost" disabled={isSaving} aria-label="Takımdan çıkar" onClick={() => void removeMember(member.userId)}>
                        <Trash2 className="h-4 w-4 text-red-500" />
                      </Button>
                    </div>
                  )}
                </div>
              ))}
            </div>
          </div>
        )}

        {!isLoading && section === "history" && (
          <div className="space-y-3 py-2">
            {revisions.length === 0 ? (
              <p className="rounded-xl border border-dashed p-6 text-center text-sm text-muted-foreground">Henüz revision kaydı yok.</p>
            ) : revisions.map((revision) => (
              <details key={revision.id} className="group rounded-xl border p-3">
                <summary className="cursor-pointer list-none">
                  <div className="flex items-start justify-between gap-4">
                    <div>
                      <p className="text-sm font-semibold">v{revision.revisionNumber} · {revision.changeType.replaceAll("_", " ")}</p>
                      <p className="mt-0.5 text-xs text-muted-foreground">{revision.changedByUsername} · {new Date(revision.createdAt).toLocaleString("tr-TR")}</p>
                    </div>
                    <span className="rounded-full bg-muted px-2 py-0.5 text-xs">{STATUS_NAMES[revision.status] ?? revision.status}</span>
                  </div>
                  {revision.summary && <p className="mt-2 text-sm text-muted-foreground">{revision.summary}</p>}
                </summary>
                <div className="mt-3 space-y-2 border-t pt-3 text-sm">
                  <p><span className="font-medium">Ürün adı:</span> {revision.name}</p>
                  <p><span className="font-medium">Tagline:</span> {revision.tagline}</p>
                  <p className="whitespace-pre-wrap"><span className="font-medium">Açıklama:</span> {revision.description}</p>
                  {revision.scheduledLaunchAt && <p><span className="font-medium">Plan:</span> {new Date(revision.scheduledLaunchAt).toLocaleString("tr-TR")}</p>}
                </div>
              </details>
            ))}
          </div>
        )}

        <DialogFooter>
          <Button variant="outline" onClick={() => setOpen(false)}>Kapat</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
