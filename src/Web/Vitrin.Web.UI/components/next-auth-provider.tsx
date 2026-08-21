"use client";

import { SessionProvider, signOut, useSession } from "next-auth/react";
import { useEffect } from "react";

function TokenExpiryGuard({ children }: { children: React.ReactNode }) {
  const { data: session } = useSession();

  useEffect(() => {
    if (session?.error === "TokenExpired") {
      void signOut({ callbackUrl: "/login" });
    }
  }, [session?.error]);

  return <>{children}</>;
}

export default function NextAuthProvider({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <SessionProvider>
      <TokenExpiryGuard>{children}</TokenExpiryGuard>
    </SessionProvider>
  );
}
