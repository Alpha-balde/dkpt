<script setup lang="ts">
import type { User, PagedResult } from '~/types'

const { apiFetch } = useApi()

const { data, refresh } = await useAsyncData('users', () =>
  apiFetch<PagedResult<User>>('/Users?page=1&pageSize=50')
)

const roleColors: Record<string, string> = {
  Admin: 'error',
  Secretaire: 'info',
  Tresorier: 'warning',
  Lecteur: 'neutral'
}
</script>

<template>
  <div class="space-y-6">
    <div>
      <h1 class="text-2xl font-bold text-(--ui-text-highlighted)">Utilisateurs</h1>
      <p class="text-sm text-(--ui-text-muted)">Gestion des comptes et rôles</p>
    </div>

    <UCard :ui="{ body: 'p-0' }">
      <div class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead>
            <tr class="border-b border-(--ui-border)">
              <th class="px-4 py-3 text-left text-xs font-medium text-(--ui-text-muted) uppercase">Email</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-(--ui-text-muted) uppercase">Rôle</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-(--ui-text-muted) uppercase">Créé le</th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="u in data?.items"
              :key="u.id"
              class="border-b border-(--ui-border)"
            >
              <td class="px-4 py-3 font-medium text-(--ui-text-highlighted)">{{ u.email }}</td>
              <td class="px-4 py-3">
                <UBadge :label="u.role" :color="(roleColors[u.role] as any) || 'neutral'" variant="subtle" size="sm" />
              </td>
              <td class="px-4 py-3 text-(--ui-text-muted)">{{ new Date(u.createdAt).toLocaleDateString('fr-FR') }}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </UCard>
  </div>
</template>
