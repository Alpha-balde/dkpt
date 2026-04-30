// ============================================
// DKPT — Playwright E2E : Authentification
// ============================================
import { test, expect } from '@playwright/test'

test.describe('Authentification', () => {
  test('page login est accessible', async ({ page }) => {
    await page.goto('/login')
    await expect(page).toHaveTitle(/DKPT/i)
    await expect(page.locator('input[type="email"], input[name="email"]')).toBeVisible()
    await expect(page.locator('input[type="password"]')).toBeVisible()
  })

  test('login avec credentials invalides échoue', async ({ page }) => {
    await page.goto('/login')
    await page.fill('input[type="email"], input[name="email"]', 'fake@test.com')
    await page.fill('input[type="password"]', 'wrongpassword')
    await page.click('button[type="submit"]')
    // Doit rester sur la page login
    await expect(page).toHaveURL(/login/)
  })

  test('accès non-authentifié redirige vers login', async ({ page }) => {
    await page.goto('/dashboard')
    await expect(page).toHaveURL(/login/)
  })
})
