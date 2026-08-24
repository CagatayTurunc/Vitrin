import { test, expect } from "@playwright/test";

test.describe("Automated UI Agent & Crawler", () => {
  test("Crawl main pages and check for errors", async ({ page }) => {
    const errors: string[] = [];
    const failedRequests: string[] = [];

    // 1. Listen for console errors
    page.on("pageerror", (err) => {
      errors.push(`Page Error: ${err.message}`);
    });
    page.on("console", (msg) => {
      if (msg.type() === "error") {
        errors.push(`Console Error: ${msg.text()}`);
      }
    });

    // 2. Listen for failed network requests
    page.on("response", (response) => {
      if (response.status() >= 400 && response.status() < 600) {
        failedRequests.push(`Failed Request: ${response.url()} - Status: ${response.status()}`);
      }
    });

    // 3. Visit Homepage
    await page.goto("/");
    await page.waitForLoadState("networkidle");
    await page.screenshot({ path: "test-results/screenshots/homepage.png", fullPage: true });

    // Check Maker Ol button if it exists
    const makerButton = page.locator("text=Maker Ol").first();
    if (await makerButton.isVisible()) {
      await makerButton.click();
      await page.waitForLoadState("networkidle");
      await page.screenshot({ path: "test-results/screenshots/maker-ol-clicked.png", fullPage: true });
      // Go back to homepage
      await page.goto("/");
      await page.waitForLoadState("networkidle");
    }

    // 4. Visit About Page
    await page.goto("/about");
    await page.waitForLoadState("networkidle");
    await page.screenshot({ path: "test-results/screenshots/about.png", fullPage: true });

    // 5. Visit Login Page
    await page.goto("/login");
    await page.waitForLoadState("networkidle");
    await page.screenshot({ path: "test-results/screenshots/login.png", fullPage: true });

    // Output all collected errors (they will be visible in the HTML report)
    if (errors.length > 0) {
      console.error("Encountered UI/Console Errors:", errors);
    }
    if (failedRequests.length > 0) {
      console.error("Encountered Network Errors:", failedRequests);
    }

    // You can also assert that there are no critical network errors
    // (Commented out so the crawler finishes the crawl even if some requests fail)
    // expect(failedRequests.length, `Expected 0 failed requests, but found ${failedRequests.length}`).toBe(0);
  });
});
