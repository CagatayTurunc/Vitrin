import { defineConfig, devices } from "@playwright/test";

const baseURL = process.env.PLAYWRIGHT_BASE_URL ?? "http://127.0.0.1:3000";
const isCI = Boolean(process.env.CI);

export default defineConfig({
  testDir: "./tests/e2e",
  timeout: 120_000,
  fullyParallel: true,
  forbidOnly: isCI,
  // CI'da 2 retry, local'de 0 — flaky test'leri ayırt et
  retries: isCI ? 2 : 0,
  workers: 1,
  outputDir: "../../../artifacts/playwright-results",
  reporter: [
    ["list"],
    [
      "html",
      {
        outputFolder: "../../../artifacts/playwright-report",
        open: "never",
      },
    ],
    // CI'da GitHub Actions summary'e yaz
    ...(isCI ? ([["github"]] as const) : []),
  ],
  use: {
    baseURL,
    actionTimeout: 15_000,
    navigationTimeout: 90_000,
    trace: "on",
    screenshot: "on",
    video: isCI ? "retain-on-failure" : "off",
    // Tüm API isteklerinde timeout — yavaş backend'i yakala
    extraHTTPHeaders: {
      "x-e2e-test": "true",
    },
  },
  projects: [
    {
      name: "chromium",
      use: { ...devices["Desktop Chrome"] },
    },
    // Sadece smoke testler için mobile viewport — opsiyonel
    // {
    //   name: "mobile-chrome",
    //   use: { ...devices["Pixel 5"] },
    //   grep: /@smoke/,
    // },
  ],
});
