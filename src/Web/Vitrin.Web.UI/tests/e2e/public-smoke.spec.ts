import { expect, test } from "@playwright/test";

test("@smoke public homepage is reachable", async ({ page }) => {
  const response = await page.goto("/", { waitUntil: "domcontentloaded" });

  expect(response?.ok()).toBe(true);
  await expect(page.locator("h1")).toBeVisible();
  await expect(page.locator('a[href="/login"]').first()).toBeVisible();
});

test("@smoke login form exposes credential and OAuth entry points", async ({ page }) => {
  const response = await page.goto("/login", { waitUntil: "domcontentloaded" });

  expect(response?.ok()).toBe(true);
  await expect(page.getByLabel("E-posta")).toBeVisible();
  await expect(page.locator("#password")).toHaveAttribute("type", "password");
  await expect(page.getByRole("button", { name: "Google" })).toBeVisible();
  await expect(page.getByRole("button", { name: "GitHub" })).toBeVisible();
});
