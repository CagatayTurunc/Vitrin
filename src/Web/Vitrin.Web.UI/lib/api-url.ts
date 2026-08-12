/**
 * Returns the public API base URL for client-side fetch calls.
 *
 * In Docker / production NEXT_PUBLIC_API_URL is intentionally set to ""
 * so that browser requests go to the same origin and are caught by the
 * Next.js rewrite rules in next.config.mjs which proxy them to the gateway.
 *
 * In local dev (no Docker) it falls back to http://localhost:5000.
 */
export function getApiUrl(): string {
  const env = process.env.NEXT_PUBLIC_API_URL;
  // Treat empty string the same as undefined — use relative origin
  if (!env) return '';
  return env;
}
