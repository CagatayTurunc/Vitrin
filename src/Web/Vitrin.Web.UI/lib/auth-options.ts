import type { NextAuthOptions } from "next-auth";
import CredentialsProvider from "next-auth/providers/credentials";
import GithubProvider from "next-auth/providers/github";
import GoogleProvider from "next-auth/providers/google";

interface AccessTokenClaims {
  sub?: string;
  Role?: string;
  name?: string;
  unique_name?: string;
  FullName?: string;
  AvatarUrl?: string;
  exp?: number;
}

function isTokenExpired(accessToken: string): boolean {
  try {
    const payload = accessToken.split(".")[1];
    if (!payload) return true;
    const claims = JSON.parse(
      Buffer.from(payload, "base64url").toString("utf8"),
    ) as AccessTokenClaims;
    if (!claims.exp) return false;
    // 30 saniyelik tolerans
    return Date.now() / 1000 > claims.exp - 30;
  } catch {
    return true;
  }
}

function getApiUrl() {
  return process.env.INTERNAL_API_URL
    ?? process.env.NEXT_PUBLIC_API_URL
    ?? "http://localhost:5000";
}

function decodeAccessToken(accessToken: string): AccessTokenClaims | null {
  try {
    const payload = accessToken.split(".")[1];
    if (!payload) return null;

    return JSON.parse(
      Buffer.from(payload, "base64url").toString("utf8"),
    ) as AccessTokenClaims;
  } catch {
    return null;
  }
}

export const authOptions: NextAuthOptions = {
  providers: [
    GoogleProvider({
      clientId: process.env.GOOGLE_CLIENT_ID ?? "",
      clientSecret: process.env.GOOGLE_CLIENT_SECRET ?? "",
    }),
    GithubProvider({
      clientId: process.env.GITHUB_CLIENT_ID ?? "",
      clientSecret: process.env.GITHUB_CLIENT_SECRET ?? "",
      authorization: { params: { scope: "read:user user:email" } },
    }),
    CredentialsProvider({
      name: "Credentials",
      credentials: {
        email: { label: "Email", type: "email" },
        password: { label: "Password", type: "password" },
      },
      async authorize(credentials) {
        if (!credentials?.email || !credentials.password) return null;

        try {
          const response = await fetch(`${getApiUrl()}/api/auth/login`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
              email: credentials.email,
              password: credentials.password,
            }),
          });

          if (!response.ok) {
            // Backend hata mesajını al ve Error olarak fırlat
            // Bu sayede NextAuth hata sayfasına yönlendirmek yerine
            // login form result.error'da mesajı yakalar
            let errorMessage = "E-posta veya şifre hatalı.";
            try {
              const data = await response.json() as { detail?: string; title?: string };
              if (data.detail) errorMessage = data.detail;
            } catch { /* json parse hatası — varsayılan mesajı kullan */ }
            throw new Error(errorMessage);
          }

          const accessToken: unknown = await response.json();
          if (typeof accessToken !== "string") return null;

          return {
            id: "credentials-user",
            email: credentials.email,
            accessToken,
          };
        } catch (error) {
          if (error instanceof Error) throw error; // hata mesajını ilet
          console.error("Credentials login failed", error);
          return null;
        }
      },
    }),
  ],
  callbacks: {
    async jwt({ token, user, account, trigger, session }) {
      if (trigger === "update" && session?.user) {
        token.fullName = session.user.name ?? undefined;
        token.username = session.user.username;
        if (session.user.image !== undefined) token.image = session.user.image;
      }

      if (account && (account.provider === "google" || account.provider === "github")) {
        const providerToken = account.provider === "google"
          ? account.id_token
          : account.access_token;
        if (!providerToken) {
          throw new Error("External provider did not return a verification token.");
        }

        const response = await fetch(`${getApiUrl()}/api/auth/external-login`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            provider: account.provider === "google" ? 1 : 2,
            providerToken,
          }),
        });

        if (!response.ok) {
          throw new Error("External identity verification failed.");
        }

        const accessToken: unknown = await response.json();
        if (typeof accessToken !== "string") {
          throw new Error("Auth service returned an invalid access token.");
        }
        token.accessToken = accessToken;
      } else if (user?.accessToken) {
        token.accessToken = user.accessToken;
      }

      if (typeof token.accessToken === "string") {
        // Token süresi dolduysa temizle — bir sonraki session çağrısında
        // accessToken undefined olacak ve frontend logout yönlendirecek
        if (isTokenExpired(token.accessToken)) {
          token.accessToken = undefined;
          return token;
        }
        const claims = decodeAccessToken(token.accessToken);
        if (claims) {
          token.role = claims.Role;
          token.id = claims.sub;
          token.username = claims.name ?? claims.unique_name ?? claims.FullName ?? "";
          token.fullName = claims.FullName ?? "";
          token.image = claims.AvatarUrl ?? "";
        }
      }

      return token;
    },
    async session({ session, token }) {
      session.accessToken = token.accessToken;
      // Token süresi dolmuşsa kullanıcıyı oturumsuz say
      if (!token.accessToken) {
        session.error = "TokenExpired";
      }
      session.user.id = token.id ?? "";
      session.user.role = token.role;
      session.user.username = token.username;
      session.user.fullName = token.fullName;
      session.user.name = token.fullName
        ?? token.username
        ?? session.user.email
        ?? null;
      session.user.image = token.image ?? null;
      return session;
    },
  },
  session: {
    strategy: "jwt",
    maxAge: 7 * 24 * 60 * 60, // 7 gün — backend JWT ile senkron
  },
  pages: {
    signIn: "/login",
    error: "/login",
  },
};
