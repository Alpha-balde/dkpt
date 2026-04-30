// ============================================
// DKPT — Playwright E2E : Health Check
// ============================================
import { test, expect } from '@playwright/test'

test.describe('Health Check', () => {
  test('page accueil est accessible', async ({ page }) => {
    const response = await page.goto('/')
    expect(response?.status()).toBe(200)
  })

  test('API Swagger est accessible', async ({ request }) => {
    const response = await request.get('/swagger/v1/swagger.json')
    expect(response.status()).toBe(200)
    const body = await response.json()
    expect(body.openapi).toBeDefined()
  })

  test('API retourne 401 sans authentification', async ({ request }) => {
    const response = await request.get('/api/Members')
    expect(response.status()).toBe(401)
  })
})
