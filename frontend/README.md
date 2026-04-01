# DKPT Frontend — Nuxt 4 + Nuxt UI v4

Interface web de gestion de l'association DKPT.

## Stack

- **Framework** : Nuxt 4.4
- **UI** : Nuxt UI v4 (125+ composants, basé sur Reka UI)
- **CSS** : TailwindCSS v4
- **Icônes** : Lucide Icons
- **Auth** : JWT stocké en cookie

## Structure

```
app/
├── pages/                      → Routes (file-based routing)
│   ├── login.vue               → Connexion (layout: auth)
│   ├── dashboard.vue           → Tableau de bord KPIs
│   ├── members/
│   │   ├── index.vue           → Liste membres (recherche + pagination)
│   │   └── [id].vue            → Détail membre + historique paiements
│   ├── payments.vue            → Liste paiements
│   ├── cotisations.vue         → Montants cotisations par année
│   ├── arrieres.vue            → Membres en retard (à implémenter)
│   ├── users.vue               → Gestion utilisateurs (Admin)
│   └── settings.vue            → Paramètres (Admin)
│
├── composables/
│   ├── useAuth.ts              → Login, logout, token, rôles, permissions
│   └── useApi.ts               → $fetch authentifié avec auto-logout 401
│
├── layouts/
│   ├── default.vue             → Sidebar + navigation + dark mode
│   └── auth.vue                → Layout minimal (login)
│
├── middleware/
│   └── auth.global.ts          → Redirige vers /login si non connecté
│
├── types/
│   └── index.ts                → Interfaces TS (Member, Payment, User, etc.)
│
└── assets/css/
    └── main.css                → Imports Tailwind + Nuxt UI + thème
```

## Composables

### `useAuth()`
```typescript
const {
  token,              // Ref<string | null> — JWT cookie
  user,               // Ref<{ email, role }> — Utilisateur courant
  isAuthenticated,    // Computed<boolean>
  isAdmin,            // Computed<boolean>
  canManageMembers,   // Computed<boolean> — Admin ou Secrétaire
  canManagePayments,  // Computed<boolean> — Admin ou Trésorier
  login(credentials), // async (email, password) => boolean
  logout()            // Supprime token + redirige /login
} = useAuth()
```

### `useApi()`
```typescript
const { apiFetch } = useApi()

// Ajoute automatiquement le header Authorization: Bearer {token}
// Appelle logout() automatiquement sur 401
const data = await apiFetch<PagedResult<Member>>('/Members?page=1&pageSize=20')
```

## Configuration API

Dans `nuxt.config.ts` :
```typescript
runtimeConfig: {
  public: {
    apiBase: process.env.NUXT_PUBLIC_API_BASE || 'http://localhost:5244/api'
  }
}
```

Variable d'environnement : `NUXT_PUBLIC_API_BASE`

## Commandes

```bash
npm install          # Installer les dépendances
npm run dev          # Serveur de développement (port 3000)
npm run build        # Build production
npm run preview      # Prévisualiser le build
npm run lint         # ESLint
npm run typecheck    # Vérification TypeScript
```

## Conventions

- **Pages** : Une page par fichier dans `app/pages/`, routing automatique
- **Layouts** : `definePageMeta({ layout: 'auth' })` pour changer de layout
- **Icônes** : Préfixe `i-lucide-` (ex: `i-lucide-users`)
- **Couleurs Nuxt UI** : Variables CSS `--ui-text-highlighted`, `--ui-text-muted`, `--ui-border`, `--ui-bg-elevated`
- **Composants Nuxt UI** : `UCard`, `UButton`, `UInput`, `UBadge`, `UModal`, `UPagination`, `USwitch`, etc.
