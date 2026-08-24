/**
 * Vitrin Backend Health Check & Endpoint Sağlık Testi
 *
 * Bu dosya 2. Katman'dır (Claude'un önerdiği mimariye göre):
 * - Her mikro servisin /health endpoint'ini kontrol eder
 * - "Frontu var backendi yok" durumunu yakalar
 * - Servis kapalıysa hangi frontend özelliğinin etkilendiğini raporlar
 *
 * Çalıştırma:
 *   pnpm test:e2e --grep "@health"
 *   PLAYWRIGHT_BASE_URL=https://vitrin.it.com pnpm test:e2e --grep "@health"
 */

import { expect, test } from "@playwright/test";

// ---------------------------------------------------------------------------
// Servis → Bağımlı frontend özellikleri haritası
// Bu tablo, bir servis kapalıysa hangi buton/sayfaların etkilendiğini gösterir
// ---------------------------------------------------------------------------
const SERVICES: Array<{
  name: string;
  healthUrl: string;
  port: number;
  affectedFeatures: string[];
}> = [
  {
    name: "Auth Servisi",
    healthUrl: "http://localhost:5104/health",
    port: 5104,
    affectedFeatures: ["Giriş yap", "Kayıt ol", "Maker ol başvurusu", "Şifremi unuttum", "Kullanıcı profili"],
  },
  {
    name: "Product Servisi",
    healthUrl: "http://localhost:5177/health",
    port: 5177,
    affectedFeatures: ["Ürün listesi (ana sayfa)", "Arama", "Kategori sayfaları", "Ürün detay", "Ürün gönder", "Lansmanlar"],
  },
  {
    name: "Voting Servisi",
    healthUrl: "http://localhost:5143/health",
    port: 5143,
    affectedFeatures: ["Upvote butonu", "Oy sayacı", "Günlük sıralama"],
  },
  {
    name: "Comment Servisi",
    healthUrl: "http://localhost:5100/health",
    port: 5100,
    affectedFeatures: ["Yorum yap", "Yorum listesi", "Yorum tepkileri"],
  },
  {
    name: "Notification Servisi",
    healthUrl: "http://localhost:5101/health",
    port: 5101,
    affectedFeatures: ["Bildirimler", "Bildirim sayacı", "Bülten aboneliği"],
  },
  {
    name: "Analytics Servisi",
    healthUrl: "http://localhost:5102/health",
    port: 5102,
    affectedFeatures: ["Maker dashboard", "Ürün istatistikleri", "View sayacı"],
  },
  {
    name: "AI Servisi",
    healthUrl: "http://localhost:5103/health",
    port: 5103,
    affectedFeatures: ["AI ürün analizi", "AI öneriler"],
  },
  {
    name: "API Gateway",
    healthUrl: "http://localhost:5000/health",
    port: 5000,
    affectedFeatures: ["TÜM API çağrıları — gateway kapalıysa hiçbir şey çalışmaz"],
  },
];

// ---------------------------------------------------------------------------
// Shared state — hangi servisler kapalı
// ---------------------------------------------------------------------------
const downServices: string[] = [];

// ---------------------------------------------------------------------------
// Health check testleri
// ---------------------------------------------------------------------------

test.describe("Mikro servis sağlık kontrolleri", () => {
  // Her servis için test üret
  for (const service of SERVICES) {
    test(`@health ${service.name} ayakta mı?`, async ({ request }) => {
      const resp = await request
        .get(service.healthUrl, { timeout: 8_000 })
        .catch(() => null);

      if (!resp) {
        downServices.push(service.name);
        // Testi başarısız işaretle ama tüm suite devam etsin
        expect
          .soft(resp, [
            `❌ ${service.name} KAPALI (port ${service.port})`,
            `   Etkilenen özellikler:`,
            ...service.affectedFeatures.map((f) => `     • ${f}`),
          ].join("\n"))
          .not.toBeNull();
        return;
      }

      // /health 404 dönebilir ama bağlantı kurulabiliyorsa servis ayakta
      const isUp = resp.status() < 500;

      if (!isUp) downServices.push(service.name);

      expect.soft(isUp, [
        `❌ ${service.name} SAĞLIKSIZ — HTTP ${resp.status()} (port ${service.port})`,
        `   Etkilenen özellikler:`,
        ...service.affectedFeatures.map((f) => `     • ${f}`),
      ].join("\n")).toBe(true);
    });
  }
});

// ---------------------------------------------------------------------------
// Frontend → Backend eşleştirme testleri
// "Frontu var backendi yok" durumunu yakala
// ---------------------------------------------------------------------------

test.describe("Frontend-Backend bağlantı testleri", () => {
  test("@health Ana sayfa ürün listesi — backend bağlantısı", async ({ page }) => {
    const responses: Array<{ url: string; status: number }> = [];

    // Tüm API çağrılarını dinle
    page.on("response", (resp) => {
      if (resp.url().includes("/api/")) {
        responses.push({ url: resp.url(), status: resp.status() });
      }
    });

    await page.goto("/", { waitUntil: "networkidle", timeout: 30_000 }).catch(() =>
      page.goto("/", { waitUntil: "domcontentloaded" }),
    );

    const productApiCalls = responses.filter((r) => r.url.includes("/api/products"));
    const failedCalls = responses.filter((r) => r.status >= 500);

    if (productApiCalls.length === 0) {
      // Ana sayfa API çağrısı göndermiyorsa SSR ile yükleniyor olabilir — bu normal
      console.log("Ana sayfa SSR ile yükleniyor — client API çağrısı yok");
    } else {
      // API çağrısı varsa 5xx olmamalı
      expect.soft(
        failedCalls.map((r) => `${r.url} → ${r.status}`),
        "Ana sayfada 5xx API hataları var",
      ).toHaveLength(0);
    }
  });

  test("@health Login sayfası — auth backend bağlantısı", async ({ page }) => {
    // Login form render edilmeli (auth servisi bağlantısı zorunlu değil, client-side)
    await page.goto("/login", { waitUntil: "domcontentloaded" });

    const loginBtn = page.getByRole("button", { name: /giriş yap/i });
    await expect(loginBtn, "Login butonu render edilmeli").toBeVisible();
    await expect(loginBtn, "Login butonu disabled olmamalı").toBeEnabled();

    // Login denemesi — backend yanıt veriyor mu?
    const loginResponse = page.waitForResponse(
      (resp) =>
        (resp.url().includes("/api/auth/login") ||
          resp.url().includes("credentials")) &&
        resp.status() >= 400,
      { timeout: 15_000 },
    );

    await page.getByLabel("E-posta").fill("health_check@test.com");
    await page.locator("#password").fill("HealthCheck123!");
    await loginBtn.click();

    const resp = await loginResponse.catch(() => null);

    if (!resp) {
      // İstek gitmedi — auth servisi kapalı olabilir
      expect.soft(
        resp,
        "❌ Login butonu tıklandı ama auth API'ye istek gitmedi — Auth servisi kapalı olabilir",
      ).not.toBeNull();
    } else {
      // 400/401 → servis çalışıyor, kimlik yanlış (beklenen)
      // 500/502/503 → servis hatalı
      expect.soft(
        resp.status(),
        `Auth servisi login'de ${resp.status()} döndürdü — 5xx servis hatası`,
      ).toBeLessThan(500);
    }
  });

  test("@health Upvote butonu — voting backend bağlantısı", async ({ page, request }) => {
    // Voting endpoint'inin erişilebilir olup olmadığını kontrol et
    const resp = await request
      .post("/api/votes", {
        data: { productId: "health-check-probe" },
        timeout: 10_000,
      })
      .catch(() => null);

    if (!resp) {
      expect.soft(
        resp,
        "❌ Voting API yanıt vermiyor — Upvote butonu çalışmıyor olabilir",
      ).not.toBeNull();
      return;
    }

    // 401 → servis çalışıyor (token gerekli)
    // 400 → servis çalışıyor (geçersiz ID)
    // 5xx → servis hatalı
    expect.soft(
      resp.status() < 500,
      `❌ Voting servisi ${resp.status()} döndürdü — Upvote butonu çalışmıyor`,
    ).toBe(true);
  });

  test("@health Maker ol butonu — auth/maker backend bağlantısı", async ({ request }) => {
    const resp = await request
      .post("/api/auth/maker-applications", {
        data: { portfolioUrl: "https://github.com/test", reason: "health check probe" },
        timeout: 10_000,
      })
      .catch(() => null);

    if (!resp) {
      expect.soft(
        resp,
        "❌ Maker applications API yanıt vermiyor — 'Maker ol' butonu çalışmıyor",
      ).not.toBeNull();
      return;
    }

    // 401 → çalışıyor, auth gerekli
    // 400 → çalışıyor, veri hatalı
    // 5xx → servis hatalı
    expect.soft(
      resp.status() < 500,
      `❌ Maker applications API ${resp.status()} döndürdü — 'Maker ol' butonu çalışmıyor`,
    ).toBe(true);
  });

  test("@health Yorum yap — comment backend bağlantısı", async ({ request }) => {
    const resp = await request
      .post("/api/comments", {
        data: { productId: "health-check", content: "probe" },
        timeout: 10_000,
      })
      .catch(() => null);

    if (!resp) {
      expect.soft(
        resp,
        "❌ Comment API yanıt vermiyor — Yorum yap butonu çalışmıyor olabilir",
      ).not.toBeNull();
      return;
    }

    expect.soft(
      resp.status() < 500,
      `❌ Comment servisi ${resp.status()} döndürdü`,
    ).toBe(true);
  });
});

// ---------------------------------------------------------------------------
// Özet rapor — tüm testler bittikten sonra
// ---------------------------------------------------------------------------

test.afterAll(async () => {
  if (downServices.length > 0) {
    console.log("\n⚠️  KAPALI SERVİSLER:");
    for (const name of downServices) {
      const svc = SERVICES.find((s) => s.name === name);
      if (svc) {
        console.log(`  ❌ ${name}`);
        console.log(`     Etkilenen özellikler: ${svc.affectedFeatures.join(", ")}`);
      }
    }
    console.log('\nÇözüm: "pnpm start-dev" veya "docker compose up -d" çalıştır\n');
  } else {
    console.log("\n✅ Tüm servisler çalışıyor\n");
  }
});
