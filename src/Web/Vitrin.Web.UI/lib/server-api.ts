const publicApiUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000";

export function getServerApiUrl() {
  return process.env.INTERNAL_API_URL ?? publicApiUrl;
}

export async function serverApiFetch<T>(
  path: string,
  options: { revalidate?: number; tags?: string[]; cache?: RequestCache } = {},
): Promise<T | null> {
  try {
    const response = await fetch(`${getServerApiUrl()}/api${path}`, {
      headers: { Accept: "application/json" },
      cache: options.cache,
      next: options.cache === "no-store" ? undefined : {
        revalidate: options.revalidate ?? 60,
        tags: options.tags,
      },
    });

    if (!response.ok) return null;
    return response.json() as Promise<T>;
  } catch {
    // API build zamanında erişilemez olabilir (prerender); null döndür
    return null;
  }
}
