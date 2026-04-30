// ============================================
// DKPT — k6 Smoke Test
// ============================================
// Script de load testing léger pour valider
// la performance post-déploiement.
//
// Exécuté dans le pipeline CD Staging après deploy.
// Portable : fonctionne sur Azure DevOps, GitHub Actions,
// GitLab CI et Bitbucket Pipelines.
//
// Usage local : k6 run --env BASE_URL=http://localhost tests/load/smoke-test.js
// ============================================

import http from 'k6/http'
import { check, sleep } from 'k6'

// ---- Configuration ----
export const options = {
  stages: [
    { duration: '10s', target: 5 },   // Montée progressive à 5 VUs
    { duration: '20s', target: 10 },   // Maintien à 10 VUs
    { duration: '10s', target: 0 },    // Descente progressive
  ],
  thresholds: {
    http_req_duration: ['p(95)<2000'],  // 95% des requêtes < 2s
    http_req_failed: ['rate<0.05'],     // Moins de 5% d'erreurs
  },
}

const BASE_URL = __ENV.BASE_URL || 'http://localhost'

// ---- Scénario principal ----
export default function () {
  // 1. Page d'accueil (Nuxt SSR)
  const home = http.get(`${BASE_URL}/`)
  check(home, {
    'home: status 200': (r) => r.status === 200,
    'home: response < 2s': (r) => r.timings.duration < 2000,
  })

  // 2. Health check API (Swagger)
  const swagger = http.get(`${BASE_URL}/swagger/v1/swagger.json`)
  check(swagger, {
    'swagger: status 200': (r) => r.status === 200,
  })

  // 3. Login API (credentials injectées via variables d'environnement)
  const loginPayload = JSON.stringify({
    email: __ENV.TEST_USER_EMAIL || 'test@dkpt.com',
    password: __ENV.TEST_USER_PASSWORD || '',
  })
  const loginHeaders = { headers: { 'Content-Type': 'application/json' } }
  const login = http.post(`${BASE_URL}/api/Auth/login`, loginPayload, loginHeaders)
  check(login, {
    'login: status 200': (r) => r.status === 200,
    'login: has token': (r) => {
      try { return JSON.parse(r.body).token !== undefined }
      catch { return false }
    },
  })

  // 4. Requêtes authentifiées (si login réussi)
  if (login.status === 200) {
    let token
    try { token = JSON.parse(login.body).token } catch { return }

    const authHeaders = {
      headers: {
        Authorization: `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
    }

    // Liste des membres
    const members = http.get(`${BASE_URL}/api/Members`, authHeaders)
    check(members, {
      'members: status 200': (r) => r.status === 200,
    })

    // Liste des cotisations
    const cotisations = http.get(`${BASE_URL}/api/Cotisations`, authHeaders)
    check(cotisations, {
      'cotisations: status 200': (r) => r.status === 200,
    })
  }

  sleep(1) // Pause entre les itérations
}
