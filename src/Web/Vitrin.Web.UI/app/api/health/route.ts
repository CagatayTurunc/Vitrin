import { NextResponse } from "next/server";

/**
 * GET /api/health
 *
 * Deploy pipeline'ının smoke test öncesi çağırdığı health check endpoint'i.
 * Container içinden `curl http://localhost:3000/api/health` ile kontrol edilir.
 *
 * Yanıt formatı backend servislerle uyumlu tutulmuştur: { "status": "healthy" }
 */
export async function GET() {
  return NextResponse.json(
    { status: "healthy", service: "vitrin-web", timestamp: new Date().toISOString() },
    { status: 200 }
  );
}
