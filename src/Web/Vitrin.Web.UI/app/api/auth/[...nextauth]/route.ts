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
          const res = await fetch(process.env.NEXT_PUBLIC_API_URL + "/api/auth/login", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
              email: credentials.email,
              password: credentials.password,
            }),
          });

          if (!res.ok) {
            return null;
          }

          const token = await res.json(); // The JWT token from our C# API

          // We return an object that NextAuth saves. Since we only care about our C# token:
          return { id: "1", email: credentials.email, accessToken: token };
        } catch (e) {
          return null;
        }
      },
    }),
  ],
  callbacks: {
    async jwt({ token, user, account, profile }) {
      // When a user signs in with Google/Github
      if (account && (account.provider === "google" || account.provider === "github")) {
        // We must send their profile to our C# API to register/login and get our custom JWT
        try {
          const provider = account.provider === "google" ? 1 : 2;
          const res = await fetch(process.env.NEXT_PUBLIC_API_URL + "/api/auth/external-login", {
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
