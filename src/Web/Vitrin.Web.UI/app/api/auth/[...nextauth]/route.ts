import { authOptions } from "@/lib/auth-options";
import NextAuth from "next-auth";
import { type NextRequest, NextResponse } from "next/server";

// NextAuth'un handle ettiği path segment'leri
const NEXTAUTH_SEGMENTS = new Set([
  "callback",
  "signin",
  "signout",
  "session",
  "csrf",
  "providers",
  "error",
  "_log",
]);

const handler = NextAuth(authOptions);

function isNextAuthPath(segments: string[]): boolean {
  // [...nextauth] param: ["callback", "google"] veya ["session"] gibi
  return segments.length > 0 && NEXTAUTH_SEGMENTS.has(segments[0]);
}

async function proxyToGateway(req: NextRequest): Promise<NextResponse> {
  const gatewayUrl =
    process.env.INTERNAL_API_URL ??
    process.env.NEXT_PUBLIC_API_URL ??
    "http://localhost:5000";

  const targetUrl = new URL(req.nextUrl.pathname + req.nextUrl.search, gatewayUrl);

  const headers = new Headers(req.headers);
  headers.delete("host");

  const body = req.method !== "GET" && req.method !== "HEAD"
    ? await req.arrayBuffer()
    : undefined;

  const response = await fetch(targetUrl.toString(), {
    method: req.method,
    headers,
    body: body ? Buffer.from(body) : undefined,
  });

  const responseBody = await response.arrayBuffer();
  return new NextResponse(responseBody, {
    status: response.status,
    headers: response.headers,
  });
}

export async function GET(
  req: NextRequest,
  context: { params: Promise<{ nextauth: string[] }> }
) {
  const { nextauth } = await context.params;
  if (!isNextAuthPath(nextauth)) return proxyToGateway(req);
  return handler(req, context as Parameters<typeof handler>[1]);
}

export async function POST(
  req: NextRequest,
  context: { params: Promise<{ nextauth: string[] }> }
) {
  const { nextauth } = await context.params;
  if (!isNextAuthPath(nextauth)) return proxyToGateway(req);
  return handler(req, context as Parameters<typeof handler>[1]);
}
