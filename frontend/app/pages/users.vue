<script setup lang="ts">
import type { User, PagedResult } from '~/types'

const { apiFetch } = useApi()
const toast = useToast()

const { data, refresh } = await useAsyncData('users', () =>
  apiFetch<PagedResult<User>>('/Users?page=1&pageSize=50')
)

const roleBadgeClass: Record<string, string> = {
  Admin: 'bg-red-100 text-red-800',
  Secretaire: 'bg-blue-100 text-blue-800',
  Tresorier: 'bg-green-100 text-green-800',
  Lecteur: 'bg-gray-100 text-gray-800'
}

const roleStats = computed(() => {
  const users = data.value?.items || []
  return [
    { label: 'Total', value: users.length, icon: 'i-lucide-users', bg: 'bg-white', text: 'text-gray-900', iconColor: 'text-gray-500' },
    { label: 'Admins', value: users.filter(u => u.role === 'Admin').length, icon: 'i-lucide-shield', bg: 'bg-red-50', text: 'text-red-700', iconColor: 'text-red-500' },
    { label: 'Secrétaires', value: users.filter(u => u.role === 'Secretaire').length, icon: 'i-lucide-file-text', bg: 'bg-blue-50', text: 'text-blue-700', iconColor: 'text-blue-500' },
    { label: 'Trésoriers', value: users.filter(u => u.role === 'Tresorier').length, icon: 'i-lucide-wallet', bg: 'bg-green-50', text: 'text-green-700', iconColor: 'text-green-500' },
    { label: 'Lecteurs', value: users.filter(u => u.role === 'Lecteur').length, icon: 'i-lucide-eye', bg: 'bg-gray-50', text: 'text-gray-700', iconColor: 'text-gray-500' }
  ]
})

const roleDescriptions: Record<string, string> = {
  Admin: 'Accès complet : gestion des membres, paiements, utilisateurs et paramètres.',
  Secretaire: 'Peut gérer les membres (créer, modifier). Peut voir les paiements.',
  Tresorier: 'Peut gérer les paiements (créer). Peut voir les membres.',
  Lecteur: 'Accès en lecture seule à toutes les données.'
}

async function deleteUser(user: User) {
  if (!confirm(`Supprimer l'utilisateur ${user.email} ?`)) return
  try {
    await apiFetch(`/Users/${user.id}`, { method: 'DELETE' })
    toast.add({ title: 'Utilisateur supprimé', color: 'success' })
    refresh()
  } catch {
    toast.add({ title: 'Erreur de suppression', color: 'error' })
  }
}
</script>

<template>
  <div class="space-y-6">
    <!-- Header -->
    <div class="flex items-center gap-3">
      <UIcon name="i-lucide-user-cog" class="w-8 h-8 text-gray-400" />
      <div>
        <h1 class="text-3xl font-bold text-gray-900">Utilisateurs</h1>
        <p class="text-sm text-gray-500 mt-1">Gestion des comptes et rôles</p>
      </div>
    </div>

    <!-- Role Stats Cards -->
    <div class="grid grid-cols-2 lg:grid-cols-5 gap-4">
      <div
        v-for="stat in roleStats"
        :key="stat.label"
        :class="[stat.bg, 'rounded-xl p-4 border border-gray-100 shadow-sm']"
      >
        <div class="flex items-center gap-3">
          <UIcon :name="stat.icon" :class="[stat.iconColor, 'w-5 h-5']" />
          <div>
            <p class="text-xs font-medium text-gray-500 uppercase">{{ stat.label }}</p>
            <p :class="[stat.text, 'text-2xl font-bold']">{{ stat.value }}</p>
          </div>
        </div>
      </div>
    </div>

    <!-- Table -->
    <div class="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden">
      <div class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead>
            <tr class="bg-gray-50 border-b border-gray-200">
              <th class="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase tracking-wider">Email</th>
              <th class="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase tracking-wider">Rôle</th>
              <th class="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase tracking-wider">Créé le</th>
              <th class="px-4 py-3 text-right text-xs font-semibold text-gray-600 uppercase tracking-wider">Actions</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-100">
            <tr
              v-for="u in data?.items"
              :key="u.id"
              class="hover:bg-gray-50 transition-colors"
            >
              <td class="px-4 py-3 font-medium text-gray-900">{{ u.email }}</td>
              <td class="px-4 py-3">
                <span :class="[roleBadgeClass[u.role] || 'bg-gray-100 text-gray-800', 'inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium']">
                  {{ u.role }}
                </span>
              </td>
              <td class="px-4 py-3 text-gray-600">{{ new Date(u.createdAt).toLocaleDateString('fr-FR') }}</td>
              <td class="px-4 py-3">
                <div class="flex items-center justify-end gap-1">
                  <button
                    class="h-8 w-8 inline-flex items-center justify-center rounded-lg text-gray-500 hover:text-blue-600 hover:bg-blue-50 transition-colors"
                    title="Voir"
                  >
                    <UIcon name="i-lucide-eye" class="w-4 h-4" />
                  </button>
                  <button
                    class="h-8 w-8 inline-flex items-center justify-center rounded-lg text-gray-500 hover:text-blue-600 hover:bg-blue-50 transition-colors"
                    title="Modifier"
                  >
                    <UIcon name="i-lucide-pencil" class="w-4 h-4" />
                  </button>
                  <button
                    class="h-8 w-8 inline-flex items-center justify-center rounded-lg text-gray-500 hover:text-red-600 hover:bg-red-50 transition-colors"
                    title="Supprimer"
                    @click="deleteUser(u)"
                  >
                    <UIcon name="i-lucide-trash-2" class="w-4 h-4" />
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Roles Info Box -->
    <div class="bg-blue-50 border border-blue-200 rounded-xl p-6">
      <h3 class="text-sm font-semibold text-blue-900 mb-3">Description des rôles</h3>
      <div class="space-y-2">
        <div v-for="(desc, role) in roleDescriptions" :key="role" class="flex items-start gap-2">
          <span :class="[roleBadgeClass[role], 'inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium shrink-0 mt-0.5']">
            {{ role }}
          </span>
          <p class="text-sm text-blue-800">{{ desc }}</p>
        </div>
      </div>
    </div>
  </div>
</template>
