import NextAuth, { NextAuthOptions } from "next-auth";
import GoogleProvider from "next-auth/providers/google";
import GithubProvider from "next-auth/providers/github";
import CredentialsProvider from "next-auth/providers/credentials";

export const authOptions: NextAuthOptions = {
  providers: [
    GoogleProvider({
      clientId: process.env.GOOGLE_CLIENT_ID || "",
      clientSecret: process.env.GOOGLE_CLIENT_SECRET || "",
    }),
    GithubProvider({
      clientId: process.env.GITHUB_CLIENT_ID || "",
      clientSecret: process.env.GITHUB_CLIENT_SECRET || "",
    }),
    CredentialsProvider({
      name: "Credentials",
      credentials: {
        email: { label: "Email", type: "email" },
        password: { label: "Password", type: "password" },
      },
      async authorize(credentials) {
        if (!credentials?.email || !credentials?.password) return null;

        try {
          const apiUrl = process.env.INTERNAL_API_URL ?? process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000";
          console.log("[NextAuth] authorize - apiUrl:", apiUrl);
          console.log("[NextAuth] authorize - email:", credentials.email);
          
          const res = await fetch(`${apiUrl}/api/auth/login`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
              email: credentials.email,
              password: credentials.password,
            }),
          });

          console.log("[NextAuth] authorize - status:", res.status);

          if (!res.ok) {
            const body = await res.text();
            console.log("[NextAuth] authorize - error body:", body);
            return null;
          }

          const token = await res.json();
          console.log("[NextAuth] authorize - token received, length:", token?.length);
          return { id: "1", email: credentials.email, accessToken: token };
        } catch (e) {
          console.error("[NextAuth] authorize - EXCEPTION:", e);
          return null;
        }
      },
    }),
  ],
  callbacks: {
    async jwt({ token, user, account, profile, trigger, session }) {
      if (trigger === "update" && session?.user) {
        token.fullName = session.user.name;
        token.username = session.user.username;
        if (session.user.image !== undefined) {
          token.image = session.user.image;
        }
      }

      // When a user signs in with Google/Github
      if (account && (account.provider === "google" || account.provider === "github")) {
        // We must send their profile to our C# API to register/login and get our custom JWT
        try {
          const provider = account.provider === "google" ? 1 : 2;
          const apiUrl = process.env.INTERNAL_API_URL || process.env.NEXT_PUBLIC_API_URL;
          const res = await fetch(apiUrl + "/api/auth/external-login", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
              email: user.email,
              fullName: user.name,
              avatarUrl: user.image,
              providerId: account.providerAccountId,
              provider: provider,
            }),
          });

          if (res.ok) {
            const jwtToken = await res.json();
            token.accessToken = jwtToken;
          }
        } catch (e) {
          console.error("External login sync failed", e);
        }
      } else if (user) {
        // Credentials login already got the token in the authorize callback
        token.accessToken = (user as any).accessToken;
      }
      
      // Decode the JWT to get the Role
      if (token.accessToken && typeof token.accessToken === "string") {
        try {
          const base64Url = token.accessToken.split('.')[1];
          if (base64Url) {
            const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
            const jsonPayload = decodeURIComponent(atob(base64).split('').map(function(c) {
                return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
            }).join(''));
            const decoded = JSON.parse(jsonPayload);
            token.role = decoded.Role;
            token.id = decoded.sub; // Extract User ID
            // In our JWT, unique_name or name usually maps to JwtRegisteredClaimNames.Name
            token.username = decoded.name || decoded.unique_name || decoded.FullName || "";
            token.fullName = decoded.FullName || "";
            token.image = decoded.AvatarUrl || "";
          }
        } catch (e) {
          console.error("Failed to decode JWT", e);
        }
      }
      
      return token;
    },
    async session({ session, token }) {
      (session as any).accessToken = token.accessToken;
      (session as any).user.role = token.role;
      (session as any).user.id = token.id;
      (session as any).user.username = token.username;
      (session as any).user.fullName = token.fullName;
      (session as any).user.name = token.fullName || token.username || (session as any).user.email;
      (session as any).user.image = token.image;
      return session;
    },
  },
  session: {
    strategy: "jwt",
  },
  pages: {
    signIn: "/login",
  },
};

const handler = NextAuth(authOptions);

export { handler as GET, handler as POST };
