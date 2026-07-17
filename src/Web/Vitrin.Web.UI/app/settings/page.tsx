import { redirect } from "next/navigation";
import { getServerSession } from "next-auth";
import { authOptions } from "@/lib/auth-options";
import { ProfileSettingsForm } from "@/components/profile-settings-form";
import { AccountModerationStatus } from "@/components/account-moderation-status";

export const metadata = {
  title: "Ayarlar — Vitrin",
  description: "Hesap ve profil ayarlarınız.",
};

export default async function SettingsPage() {
  const session = await getServerSession(authOptions);

  if (!session?.user) {
    redirect("/login");
  }

  // Fetch current user details
  const res = await fetch(process.env.NEXT_PUBLIC_API_URL + "/api/auth/users/me", {
    headers: {
      Authorization: `Bearer ${session.accessToken}`,
    },
    cache: "no-store",
  });

  if (!res.ok) {
    const errorText = await res.text();
    return (
      <div className="container max-w-4xl py-10 mt-16">
        <h1 className="text-2xl font-bold mb-4">Hata</h1>
        <p>Profil bilgileri yüklenemedi. Lütfen daha sonra tekrar deneyin.</p>
        <div className="mt-4 p-4 bg-red-500/10 text-red-500 rounded border border-red-500/20">
          <p>Status: {res.status}</p>
          <p>Message: {errorText}</p>
        </div>
      </div>
    );
  }

  const userProfile = await res.json();

  return (
    <div className="container mx-auto max-w-5xl px-4 md:px-8 py-4 mt-6 md:mt-10 mb-20">
      <div className="mb-10 flex flex-col items-center text-center animate-in fade-in slide-in-from-top-4 duration-500">
        <h1 className="text-4xl md:text-5xl font-extrabold tracking-tight bg-clip-text text-transparent bg-gradient-to-r from-foreground to-foreground/60 pb-1">
          Ayarlar
        </h1>
        <p className="text-muted-foreground mt-3 text-base md:text-lg max-w-xl">
          Hesabınızı, profilinizi ve tercihlerinizi buradan kolayca yönetebilirsiniz.
        </p>
      </div>
      <AccountModerationStatus
        activeBanId={userProfile.activeBanId}
        suspendedUntilUtc={userProfile.suspendedUntilUtc}
        suspensionReason={userProfile.suspensionReason}
        isBanned={userProfile.isBanned}
      />
      <ProfileSettingsForm initialData={userProfile} />
    </div>
  );
}
