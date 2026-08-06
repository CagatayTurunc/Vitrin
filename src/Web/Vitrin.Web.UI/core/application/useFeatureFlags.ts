/**
 * useFeatureFlags — Lightweight feature flag & A/B test hook.
 *
 * Endpoint: GET /api/feature-flags
 * Response: [{ key: string; variantPayload?: string; isActive: boolean }]
 *
 * Usage:
 *   const { isEnabled, getVariant } = useFeatureFlags();
 *   if (isEnabled("new-checkout")) { ... }
 *   const variant = getVariant<{ headline: string }>("hero-ab-test");
 */

import { useEffect, useState } from "react";
import { useSession } from "next-auth/react";

const API_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";

interface FlagEntry {
  key: string;
  variantPayload?: string | null;
  isActive: boolean;
}

interface FeatureFlagStore {
  flags: Map<string, FlagEntry>;
  isLoaded: boolean;
  isEnabled: (key: string) => boolean;
  getVariant: <T = Record<string, unknown>>(key: string) => T | null;
}

let cachedFlags: Map<string, FlagEntry> | null = null;
let fetchPromise: Promise<Map<string, FlagEntry>> | null = null;

async function fetchFlags(token?: string): Promise<Map<string, FlagEntry>> {
  if (cachedFlags) return cachedFlags;
  if (fetchPromise) return fetchPromise;

  fetchPromise = (async () => {
    try {
      const headers: HeadersInit = token ? { Authorization: `Bearer ${token}` } : {};
      const res = await fetch(`${API_URL}/api/feature-flags`, { headers, cache: "no-store" });
      if (!res.ok) return new Map();
      const data = (await res.json()) as FlagEntry[];
      const map = new Map(data.map((f) => [f.key, f]));
      cachedFlags = map;
      // TTL: invalidate after 5 minutes
      setTimeout(() => { cachedFlags = null; fetchPromise = null; }, 5 * 60 * 1000);
      return map;
    } catch {
      return new Map();
    } finally {
      fetchPromise = null;
    }
  })();

  return fetchPromise;
}

export function useFeatureFlags(): FeatureFlagStore {
  const { data: session } = useSession();
  const [flags, setFlags] = useState<Map<string, FlagEntry>>(cachedFlags ?? new Map());
  const [isLoaded, setIsLoaded] = useState(!!cachedFlags);

  useEffect(() => {
    void fetchFlags(session?.accessToken).then((map) => {
      setFlags(map);
      setIsLoaded(true);
    });
  }, [session?.accessToken]);

  return {
    flags,
    isLoaded,
    isEnabled: (key: string) => flags.get(key)?.isActive === true,
    getVariant: <T,>(key: string): T | null => {
      const entry = flags.get(key);
      if (!entry?.variantPayload) return null;
      try {
        return JSON.parse(entry.variantPayload) as T;
      } catch {
        return null;
      }
    },
  };
}

/** Invalidate flag cache (call after admin updates) */
export function invalidateFeatureFlagCache() {
  cachedFlags = null;
  fetchPromise = null;
}
