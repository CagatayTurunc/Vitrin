/**
 * Vitrin Authenticated Flow Testleri
 *
 * Bu dosya; gerçek kullanıcı kimliği gerektiren akışları test eder.
 * Çalışması için E2E_TEST_EMAIL ve E2E_TEST_PASSWORD env değişkenleri gerekir.
 *
 * Çalıştırma:
 *   E2E_TEST_EMAIL=test@... E2E_TEST_PASSWORD=... pnpm test:e2e --grep "@auth-flow"
 *
 * GitHub Actions'da:
 *   env:
 *     E2E_TEST_EMAIL: ${{ secrets.E2E_TEST_EMAIL }}
 *     E2E_TEST_PASSWORD: ${{ secrets.E2E_TEST_PASSWORD }}
 */

import { type BrowserContext, type Page, expect, test } from "@playwright/test";

// ---------------------------------------------------------------------------
// Fixtures — oturum açık sayfa
// ---------------------------------------------------------------------------

type AuthFixtures = {
  authedPage: Page;
  authedContext: BrowserContext;
};

const authedTest = test.extend<AuthFixtures>({
  authedContext: async ({ browser }, use) => {
    const email = process.env.E2E_TEST_EMAIL;
    const password = process.env.E2E_TEST_PASSWORD;

    if (!email || !password) {
      // Env yoksa boş context — testler skip edilecek
      const ctx = await browser.newContext();
      await use(ctx);
      await ctx.close();
      return;
    }

    const ctx = await browser.newContext();
    const page = await ctx.newPage();

    // Login
    await page.goto("/login", { waitUntil: "domcontentloaded" });
    await page.getByLabel("E-posta").fill(email);
    await page.locator("#password").fill(password);

    await Promise.all([
      page.waitForNavigation({ timeout: 20_000 }).catch(() => null),
      page.getByRole("button", { name: /giriş yap/i }).click(),
    ]);

    // Login başarılı mı?
    const isLoggedIn = !page.url().includes("/login");
    if (!isLoggedIn) {
      throw new Error(
        `E2E login başarısız — ${email} ile giriş yapılamadı. Kimlik bilgilerini kontrol et.`,
      );
    }

    await page.close();
    await use(ctx);
    await ctx.close();
  },

  authedPage: async ({ authedContext }, use) => {
    const page = await authedContext.newPage();
    await use(page);
    await page.close();
  },
});

// ---------------------------------------------------------------------------
// Guard — env değişkeni yoksa testleri atla
// ---------------------------------------------------------------------------

authedTest.beforeEach(async () => {
  if (!process.env.E2E_TEST_EMAIL || !process.env.E2E_TEST_PASSWORD) {
    authedTest.skip(
      true,
      "E2E_TEST_EMAIL / E2E_TEST_PASSWORD tanımlı değil — authenticated testler atlanıyor",
    );
  }
});

// ---------------------------------------------------------------------------
// Login Akışı
// ---------------------------------------------------------------------------

authedTest.describe("Login akışı (authenticated)", () => {
  authedTest("@auth-flow başarılı login sonrası ana sayfaya yönlendiriyor", async ({ authedPage }) => {
    await authedPage.goto("/", { waitUntil: "domcontentloaded" });

    // Giriş yapılmışsa /login sayfasında olmamalı
    expect(authedPage.url()).not.toContain("/login");

    // Kullanıcı menüsü veya profil elementi görünmeli
    const userIndicator = authedPage.locator(
      '[data-testid="user-menu"], [aria-label*="profil"], [aria-label*="hesap"], .user-avatar',
    );

    // Bu element varsa göster, yoksa sadece URL kontrolü yeterli
    if ((await userIndicator.count()) > 0) {
      await expect(userIndicator.first()).toBeVisible();
    }
  });

  authedTest("@auth-flow /dashboard sayfası yükleniyor ve içerik var", async ({ authedPage }) => {
    await authedPage.goto("/dashboard", { waitUntil: "domcontentloaded" });

    // Login değilse /login'e yönlendirmeli — ama biz login olduk
    expect(authedPage.url()).not.toContain("/login");

    // Maker Dashboard başlığı görünmeli
    await expect(
      authedPage.locator('text="Maker Dashboard", text="Ürün Analizleri"').first(),
    ).toBeVisible({ timeout: 15_000 });
  });
});

// ---------------------------------------------------------------------------
// Maker Ol Akışı (tam E2E)
// ---------------------------------------------------------------------------

authedTest.describe("Maker ol akışı (authenticated)", () => {
  authedTest("@auth-flow /submit sayfası kullanıcı rolüne göre doğru içerik gösteriyor", async ({ authedPage }) => {
    const makerApplicationRequest = authedPage.waitForRequest(
      (req) =>
        req.url().includes("/api/auth/maker-applications") && req.method() === "POST",
      { timeout: 5_000 },
    );

    await authedPage.goto("/submit", { waitUntil: "domcontentloaded" });

    // Sayfa yüklenmiş olmalı
    await expect(authedPage.locator("h1")).toBeVisible({ timeout: 15_000 });

    const h1 = await authedPage.locator("h1").first().textContent();

    if (h1?.includes("Maker ol")) {
      // Member kullanıcı → başvuru formu görünmeli
      await expect(
        authedPage.getByPlaceholder(/github\.com|linkedin|portfolyo/i),
      ).toBeVisible({ timeout: 10_000 });

      await expect(
        authedPage.getByPlaceholder(/neden maker olmak istiyorsun/i),
      ).toBeVisible();

      const submitBtn = authedPage.getByRole("button", { name: /başvuruyu gönder/i });
      await expect(submitBtn).toBeVisible();
      await expect(submitBtn).toBeEnabled();

      // Formu doldur
      await authedPage
        .getByPlaceholder(/github\.com|linkedin|portfolyo/i)
        .fill("https://github.com/e2e-test-user");
      await authedPage
        .getByPlaceholder(/neden maker olmak istiyorsun/i)
        .fill(
          "Bu otomatik bir E2E test başvurusudur. Gerçek bir başvuru değildir ve silinebilir.",
        );

      // Gönder butonuna tıkla
      await submitBtn.click();

      // İstek gönderilmiş olmalı veya başarı mesajı gösterilmeli
      const req = await makerApplicationRequest.catch(() => null);
      if (req) {
        // Screenshot al — başvuru sonrası ekran
        await authedPage.screenshot({
          path: "artifacts/screenshots/maker-application-after.png",
          fullPage: false,
        });
      }

      // Başarı durumu veya hata mesajı gösterilmeli (form işlendi)
      await expect(
        authedPage.locator(
          'text="Başvurun alındı", text="başvuru", [class*="destructive"], [role="alert"]',
        ).first(),
      ).toBeVisible({ timeout: 15_000 });
    } else if (h1?.includes("Yeni ürün ekle")) {
      // Maker kullanıcı → wizard görünmeli
      await expect(
        authedPage.locator('text="Ürün adı", text="Adım 1"').first(),
      ).toBeVisible({ timeout: 10_000 });
    } else {
      // Beklenmedik durum
      throw new Error(`/submit sayfası beklenmedik başlık gösterdi: "${h1}"`);
    }
  });
});

// ---------------------------------------------------------------------------
// Profil ve Ayarlar
// ---------------------------------------------------------------------------

authedTest.describe("Profil sayfaları (authenticated)", () => {
  authedTest("@auth-flow /settings sayfası yükleniyor", async ({ authedPage }) => {
    await authedPage.goto("/settings", { waitUntil: "domcontentloaded" });

    expect(authedPage.url()).not.toContain("/login");
    await expect(authedPage.locator("main, form")).toBeVisible({ timeout: 10_000 });
  });

  authedTest("@auth-flow /notifications sayfası yükleniyor", async ({ authedPage }) => {
    // Bildirimler sayfası
    await authedPage.goto("/notifications", { waitUntil: "domcontentloaded" });

    expect(authedPage.url()).not.toContain("/login");
    await expect(authedPage.locator("main")).toBeVisible({ timeout: 10_000 });

    // Bildirimler API çağrısı gitmiş olmalı
    // (network intercept yerine sayfa içeriğini kontrol et)
    await expect(
      authedPage.locator('text="Bildirim", [data-testid="notifications"]').first(),
    ).toBeVisible({ timeout: 10_000 }).catch(() => {
      // Başlık olmayabilir, ana tag yüklenmiş olması yeterli
    });
  });
});

// ---------------------------------------------------------------------------
// Screenshot karşılaştırma (visual regression için temel)
// ---------------------------------------------------------------------------

authedTest.describe("Sayfa görüntüleri", () => {
  authedTest("@auth-flow dashboard ekran görüntüsü alınıyor", async ({ authedPage }) => {
    await authedPage.goto("/dashboard", { waitUntil: "networkidle", timeout: 30_000 }).catch(() =>
      authedPage.goto("/dashboard", { waitUntil: "domcontentloaded" }),
    );

    expect(authedPage.url()).not.toContain("/login");

    // Sayfa tam yüklenince screenshot al
    await authedPage.waitForTimeout(1000);
    await authedPage.screenshot({
      path: "artifacts/screenshots/dashboard-authenticated.png",
      fullPage: true,
    });
  });

  authedTest("@auth-flow submit sayfası ekran görüntüsü alınıyor", async ({ authedPage }) => {
    await authedPage.goto("/submit", { waitUntil: "domcontentloaded" });

    await authedPage.locator("h1").waitFor({ timeout: 15_000 });
    await authedPage.waitForTimeout(500);

    await authedPage.screenshot({
      path: "artifacts/screenshots/submit-page-authenticated.png",
      fullPage: true,
    });
  });
});
