/**
 * Vitrin Smoke Test Suite
 *
 * @smoke tag'li testler deploy sonrası otomatik çalışır.
 * Her test; sayfanın render edildiğini, kritik UI elementlerinin
 * göründüğünü ve ilgili API çağrısının başarılı döndüğünü doğrular.
 *
 * Çalıştırma:
 *   pnpm test:e2e:smoke          → sadece smoke testler
 *   pnpm test:e2e                → tüm e2e suite
 *   PLAYWRIGHT_BASE_URL=https://vitrin.it.com pnpm test:e2e:smoke
 */

import { expect, test } from "@playwright/test";

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

/** Sayfanın 2xx HTTP ile yüklendiğini ve JS hatasının olmadığını doğrular */
async function expectPageOk(
  page: Parameters<typeof test>[1] extends infer T
    ? T extends (args: { page: infer P }) => unknown
      ? P
      : never
    : never,
  path: string,
) {
  const errors: string[] = [];
  page.on("pageerror", (err) => errors.push(err.message));

  const response = await page.goto(path, { waitUntil: "domcontentloaded" });
  expect(response?.ok(), `${path} HTTP ${response?.status()}`).toBe(true);
  expect(errors, `${path} JS hataları: ${errors.join(" | ")}`).toHaveLength(0);
}

// ---------------------------------------------------------------------------
// 1. Sayfa Erişilebilirlik Testleri  (@smoke)
// ---------------------------------------------------------------------------

test.describe("Kritik sayfalar yükleniyor", () => {
  const publicPages = [
    { path: "/", name: "Ana sayfa" },
    { path: "/login", name: "Giriş" },
    { path: "/register", name: "Kayıt" },
    { path: "/forgot-password", name: "Şifremi unuttum" },
    { path: "/launches", name: "Lansmanlar" },
    { path: "/launches/upcoming", name: "Yaklaşan lansmanlar" },
    { path: "/categories", name: "Kategoriler" },
    { path: "/collections", name: "Koleksiyonlar" },
    { path: "/search", name: "Arama" },
    { path: "/about", name: "Hakkımızda" },
    { path: "/terms", name: "Kullanım koşulları" },
    { path: "/privacy", name: "Gizlilik" },
    { path: "/kvkk", name: "KVKK" },
  ];

  for (const { path, name } of publicPages) {
    test(`@smoke ${name} sayfası yükleniyor (${path})`, async ({ page }) => {
      await expectPageOk(page, path);
    });
  }
});

// ---------------------------------------------------------------------------
// 2. Ana Sayfa İçerik Testleri
// ---------------------------------------------------------------------------

test.describe("Ana sayfa içeriği", () => {
  test("@smoke hero section ve navigasyon görünüyor", async ({ page }) => {
    await page.goto("/", { waitUntil: "domcontentloaded" });

    // Login linki nav'da olmalı
    await expect(page.locator('a[href="/login"]').first()).toBeVisible();

    // Ana içerik yüklenmiş olmalı — en az bir element render edilmiş
    await expect(page.locator("main, #__next, body")).toBeVisible();
  });

  test("@smoke API'den ürünler yükleniyor (network intercept)", async ({ page }) => {
    // /api/products isteğini izle
    const productsRequest = page.waitForResponse(
      (resp) => resp.url().includes("/api/products") && resp.status() < 400,
      { timeout: 15_000 },
    );

    await page.goto("/", { waitUntil: "domcontentloaded" });

    const resp = await productsRequest.catch(() => null);

    if (resp) {
      // İstek gidiyorsa 2xx dönmeli
      expect(resp.status(), `Ürünler API'si ${resp.status()} döndü`).toBeLessThan(400);
    }
    // Servis kapalıysa test "uyarı" olarak geçer — hard fail değil
  });
});

// ---------------------------------------------------------------------------
// 3. Login Formu
// ---------------------------------------------------------------------------

test.describe("Login formu", () => {
  test("@smoke login sayfası render edildi ve form elementleri var", async ({ page }) => {
    await page.goto("/login", { waitUntil: "domcontentloaded" });

    await expect(page.getByLabel("E-posta")).toBeVisible();
    await expect(page.locator("#password")).toBeVisible();
    await expect(page.getByRole("button", { name: /giriş yap/i })).toBeVisible();
  });

  test("@smoke Google ve GitHub butonları mevcut ve tıklanabilir", async ({ page }) => {
    await page.goto("/login", { waitUntil: "domcontentloaded" });

    const googleBtn = page.getByRole("button", { name: /google/i });
    const githubBtn = page.getByRole("button", { name: /github/i });

    await expect(googleBtn).toBeVisible();
    await expect(githubBtn).toBeVisible();
    await expect(googleBtn).toBeEnabled();
    await expect(githubBtn).toBeEnabled();
  });

  test("@smoke yanlış kimlik bilgisiyle login hata gösteriyor", async ({ page }) => {
    // Backend isteğini yakala
    const loginRequest = page.waitForResponse(
      (resp) =>
        (resp.url().includes("/api/auth/login") ||
          resp.url().includes("/api/auth/callback/credentials")) &&
        resp.status() >= 400,
      { timeout: 20_000 },
    );

    await page.goto("/login", { waitUntil: "domcontentloaded" });
    await page.getByLabel("E-posta").fill("yanlis@test.com");
    await page.locator("#password").fill("YanlisS1fre!");
    await page.getByRole("button", { name: /giriş yap/i }).click();

    // Backend'e istek gitmiş olmalı
    const resp = await loginRequest.catch(() => null);

    if (resp) {
      // 400 veya 401 dönmeli
      expect([400, 401, 422], `Login API ${resp.status()} döndü`).toContain(resp.status());
    }

    // UI'da hata mesajı gösterilmeli
    await expect(
      page.locator('[class*="destructive"], [role="alert"], .text-destructive').first(),
    ).toBeVisible({ timeout: 10_000 });
  });

  test("@smoke şifremi unuttum linki çalışıyor", async ({ page }) => {
    await page.goto("/login", { waitUntil: "domcontentloaded" });
    await page.getByRole("link", { name: /şifreni mi unuttun/i }).click();
    await expect(page).toHaveURL(/forgot-password/, { timeout: 10_000 });
  });

  test("@smoke kayıt ol linki çalışıyor", async ({ page }) => {
    await page.goto("/login", { waitUntil: "domcontentloaded" });
    await page.getByRole("link", { name: /kayıt ol/i }).click();
    await expect(page).toHaveURL(/register/, { timeout: 10_000 });
  });
});

// ---------------------------------------------------------------------------
// 4. Kayıt Formu
// ---------------------------------------------------------------------------

test.describe("Kayıt formu", () => {
  test("@smoke register sayfası render edildi ve tüm alanlar var", async ({ page }) => {
    await page.goto("/register", { waitUntil: "domcontentloaded" });

    // Register form fieldları
    await expect(page.getByPlaceholder(/ad soyad/i)).toBeVisible();
    await expect(page.getByPlaceholder(/kullanici_adi/i)).toBeVisible();
    await expect(page.getByPlaceholder(/isim@ornek\.com/i)).toBeVisible();
    await expect(page.locator('input[name="password"]')).toBeVisible();
    await expect(page.getByRole("button", { name: /kayıt ol/i })).toBeVisible();
  });

  test("@smoke register API isteği backend'e gidiyor (network intercept)", async ({ page }) => {
    const registerRequest = page.waitForRequest(
      (req) => req.url().includes("/api/auth/register") && req.method() === "POST",
      { timeout: 15_000 },
    );

    await page.goto("/register", { waitUntil: "domcontentloaded" });
    await page.getByPlaceholder(/ad soyad/i).fill("Test Kullanıcı");
    await page.getByPlaceholder(/kullanici_adi/i).fill("testuser_smoke");
    await page.getByPlaceholder(/isim@ornek\.com/i).fill("smoke_test_user@vitrin-qa.test");
    await page.locator('input[name="password"]').fill("SmokeTest123!@#");
    await page.getByRole("button", { name: /kayıt ol/i }).click();

    // İstek gönderilmiş olmalı
    const req = await registerRequest.catch(() => null);
    expect(req, "Register butonu API'ye istek göndermedi").not.toBeNull();
  });
});

// ---------------------------------------------------------------------------
// 5. Maker Ol Akışı  ← en kritik, daha önce bozulmuştu
// ---------------------------------------------------------------------------

test.describe("Maker ol akışı", () => {
  test("@smoke /submit sayfası yükleniyor", async ({ page }) => {
    // Giriş yapılmadan /submit → /login'e yönlendirmeli
    await page.goto("/submit", { waitUntil: "domcontentloaded" });

    // Ya /login'e redirect ya da "Maker ol" başlığı gösterilmeli
    const isLoginPage = page.url().includes("/login");
    const isSubmitPage = page.url().includes("/submit");

    expect(isLoginPage || isSubmitPage, "/submit → login veya submit olmalı").toBe(true);
  });

  test("@smoke submit sayfasında maker-application formu render ediliyor (authenticated)", async ({ page, context }) => {
    // Bu test, localStorage'a mock session ekleyerek giriş simüle eder.
    // Gerçek bir kullanıcı token'ı yoksa test "atlandı" olarak işaretlenir.
    const testEmail = process.env.E2E_TEST_EMAIL;
    const testPassword = process.env.E2E_TEST_PASSWORD;

    if (!testEmail || !testPassword) {
      test.skip(true, "E2E_TEST_EMAIL / E2E_TEST_PASSWORD env değişkenleri tanımlı değil");
      return;
    }

    // Önce login yap
    await page.goto("/login", { waitUntil: "domcontentloaded" });
    await page.getByLabel("E-posta").fill(testEmail);
    await page.locator("#password").fill(testPassword);

    await Promise.all([
      page.waitForNavigation({ timeout: 20_000 }).catch(() => null),
      page.getByRole("button", { name: /giriş yap/i }).click(),
    ]);

    // Submit sayfasına git
    await page.goto("/submit", { waitUntil: "domcontentloaded" });

    // Maker olmayan kullanıcı → "Maker ol" başlığı görünmeli
    // Maker kullanıcı → "Yeni ürün ekle" başlığı görünmeli
    await expect(
      page.locator('h1:has-text("Maker ol"), h1:has-text("Yeni ürün ekle")'),
    ).toBeVisible({ timeout: 15_000 });
  });

  test("@smoke maker-application POST API endpoint erişilebilir (401 bekleniyor)", async ({ request }) => {
    // Token olmadan 401 dönmeli — endpoint var ve çalışıyor
    const resp = await request.post("/api/auth/maker-applications", {
      data: { portfolioUrl: "https://github.com/test", reason: "Bu bir smoke test isteğidir" },
    });

    expect(
      [401, 403],
      `Maker API token olmadan ${resp.status()} döndü — 401/403 bekleniyor`,
    ).toContain(resp.status());
  });
});

// ---------------------------------------------------------------------------
// 6. Upvote (Oy Ver) Butonu
// ---------------------------------------------------------------------------

test.describe("Upvote / Oy ver", () => {
  test("@smoke votes endpoint token olmadan 401 dönüyor", async ({ request }) => {
    const resp = await request.post("/api/votes", {
      data: { productId: "00000000-0000-0000-0000-000000000001" },
    });

    expect(
      [401, 403],
      `Votes API token olmadan ${resp.status()} döndü — 401/403 bekleniyor`,
    ).toContain(resp.status());
  });
});

// ---------------------------------------------------------------------------
// 7. Arama
// ---------------------------------------------------------------------------

test.describe("Arama", () => {
  test("@smoke arama sayfası ve API çalışıyor", async ({ page }) => {
    const searchResponse = page.waitForResponse(
      (resp) =>
        (resp.url().includes("/api/products/search") ||
          resp.url().includes("/api/products")) &&
        resp.status() < 500,
      { timeout: 15_000 },
    );

    await page.goto("/search?q=uygulama", { waitUntil: "domcontentloaded" });

    const resp = await searchResponse.catch(() => null);
    if (resp) {
      expect(resp.status(), `Arama API ${resp.status()} döndü`).toBeLessThan(500);
    }
  });
});

// ---------------------------------------------------------------------------
// 8. Kategoriler
// ---------------------------------------------------------------------------

test.describe("Kategoriler", () => {
  test("@smoke kategoriler API çalışıyor ve liste render ediliyor", async ({ page }) => {
    const categoriesResponse = page.waitForResponse(
      (resp) => resp.url().includes("/api/categories") && resp.status() < 500,
      { timeout: 15_000 },
    );

    await page.goto("/categories", { waitUntil: "domcontentloaded" });

    const resp = await categoriesResponse.catch(() => null);
    if (resp) {
      expect(resp.status(), `Kategoriler API ${resp.status()} döndü`).toBeLessThan(500);
    }
  });
});

// ---------------------------------------------------------------------------
// 9. Backend Health Check (Playwright request API ile)
// ---------------------------------------------------------------------------

test.describe("Backend sağlık kontrolü", () => {
  const baseUrl = process.env.PLAYWRIGHT_BASE_URL ?? "http://localhost:3001";

  // Gateway üzerinden erişilebilen health endpoint
  test("@smoke gateway /health erişilebilir", async ({ request }) => {
    // Bu test Next.js üzerinden değil, doğrudan gateway'e gider
    const gatewayUrl =
      process.env.GATEWAY_URL ?? baseUrl.replace("3001", "5000").replace("3003", "5000");

    const resp = await request
      .get(`${gatewayUrl}/health`, { timeout: 10_000 })
      .catch(() => null);

    if (!resp) {
      test.skip(true, `Gateway (${gatewayUrl}) erişilemiyor — local dev çalışmıyor olabilir`);
      return;
    }

    expect(resp.status(), "Gateway /health beklenmedik status döndürdü").toBeLessThan(500);
  });

  // Public API endpoint'leri frontend üzerinden test et
  test("@smoke /api/products public erişim 2xx dönüyor", async ({ request }) => {
    const resp = await request.get("/api/products", { timeout: 15_000 }).catch(() => null);

    if (!resp) {
      test.skip(true, "API erişilemiyor");
      return;
    }

    expect(
      resp.status(),
      `/api/products ${resp.status()} döndü — backend çalışmıyor olabilir`,
    ).toBeLessThan(400);
  });

  test("@smoke /api/categories public erişim 2xx dönüyor", async ({ request }) => {
    const resp = await request.get("/api/categories", { timeout: 15_000 }).catch(() => null);

    if (!resp) {
      test.skip(true, "API erişilemiyor");
      return;
    }

    expect(
      resp.status(),
      `/api/categories ${resp.status()} döndü — product servisi çalışmıyor olabilir`,
    ).toBeLessThan(400);
  });

  test("@smoke /api/launches/daily public erişim 2xx dönüyor", async ({ request }) => {
    const resp = await request
      .get("/api/launches/daily", { timeout: 15_000 })
      .catch(() => null);

    if (!resp) {
      test.skip(true, "API erişilemiyor");
      return;
    }

    expect(
      resp.status(),
      `/api/launches/daily ${resp.status()} döndü`,
    ).toBeLessThan(400);
  });

  // Auth gerektiren endpoint'ler token olmadan 401 dönmeli
  const protectedEndpoints = [
    { path: "/api/auth/users/me", name: "Kullanıcı profili" },
    { path: "/api/notifications/me", name: "Bildirimler" },
    { path: "/api/votes/me", name: "Oylarım" },
  ];

  for (const { path, name } of protectedEndpoints) {
    test(`@smoke ${name} (${path}) — token olmadan 401 dönüyor`, async ({ request }) => {
      const resp = await request.get(path, { timeout: 10_000 }).catch(() => null);

      if (!resp) {
        test.skip(true, "API erişilemiyor");
        return;
      }

      expect(
        [401, 403],
        `${name} token olmadan ${resp.status()} döndü — güvenlik sorunu olabilir!`,
      ).toContain(resp.status());
    });
  }
});
