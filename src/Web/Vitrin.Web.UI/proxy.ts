import { withAuth } from "next-auth/middleware";
import { NextResponse } from "next/server";

export default withAuth(
  function proxy(req) {
    const token = req.nextauth.token;
    const isAuth = !!token;
    const isAuthPage = req.nextUrl.pathname.startsWith("/login") || req.nextUrl.pathname.startsWith("/register");
    const isAdminLogin = req.nextUrl.pathname === "/admin/login";
    const isAdminPage = req.nextUrl.pathname.startsWith("/admin") && !isAdminLogin;

    if (isAuthPage || isAdminLogin) {
      if (isAuth) {
        // If they are logged in and try to go to admin login, redirect them to admin if they are admin, or home if they are not.
        if (isAdminLogin && token.role === "Admin") {
           return NextResponse.redirect(new URL("/admin", req.url));
        }
        return NextResponse.redirect(new URL("/", req.url));
      }
      return null;
    }

    if (req.nextUrl.pathname.startsWith("/submit")) {
      if (!isAuth) {
        return NextResponse.redirect(new URL("/login", req.url));
      }
      return null;
    }

    if (isAdminPage) {
      // If not logged in, redirect to admin login
      if (!isAuth) {
        return NextResponse.redirect(new URL("/admin/login", req.url));
      }
      
      // If logged in but not Admin, redirect to home
      if (token.role !== "Admin") {
        return NextResponse.redirect(new URL("/", req.url));
      }
    }

    return null;
  },
  {
    callbacks: {
      async authorized() {
        // This is a work-around for handling redirect on auth pages.
        // We return true here so that the middleware function above
        // is always called.
        return true;
      },
    },
  }
);

export const config = {
  matcher: ["/admin/:path*", "/login", "/register", "/submit"],
};
