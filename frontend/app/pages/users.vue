<script setup lang="ts">
import type { User, PagedResult } from '~/types'

const { apiFetch } = useApi()
const { isAdmin } = useAuth()
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

const roleItems = [
  { label: 'Admin', value: 'Admin' },
  { label: 'Secrétaire', value: 'Secretaire' },
  { label: 'Trésorier', value: 'Tresorier' },
  { label: 'Lecteur', value: 'Lecteur' }
]

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

// ===== CREATE USER =====
const showCreateModal = ref(false)
const createForm = reactive({ email: '', password: '', role: 'Lecteur' })
const creating = ref(false)

function openCreateModal() {
  Object.assign(createForm, { email: '', password: '', role: 'Lecteur' })
  showCreateModal.value = true
}

async function createUser() {
  if (!createForm.email || !createForm.password) {
    toast.add({ title: 'Email et mot de passe requis', color: 'warning' })
    return
  }
  creating.value = true
  try {
    await apiFetch('/Users', { method: 'POST', body: createForm })
    toast.add({ title: 'Utilisateur créé', color: 'success', icon: 'i-lucide-check-circle' })
    showCreateModal.value = false
    refresh()
  } catch (err: any) {
    toast.add({ title: 'Erreur', description: err?.data?.message || 'Impossible de créer', color: 'error' })
  } finally {
    creating.value = false
  }
}

// ===== EDIT USER (role only) =====
const showEditModal = ref(false)
const editingUser = ref<User | null>(null)
const editForm = reactive({ role: 'Lecteur' })
const saving = ref(false)

function openEditModal(user: User) {
  editingUser.value = user
  editForm.role = user.role
  showEditModal.value = true
}

async function updateUser() {
  if (!editingUser.value) return
  saving.value = true
  try {
    await apiFetch(`/Users/${editingUser.value.id}`, {
      method: 'PUT',
      body: { role: editForm.role }
    })
    toast.add({ title: 'Rôle modifié', color: 'success', icon: 'i-lucide-check-circle' })
    showEditModal.value = false
    refresh()
  } catch (err: any) {
    toast.add({ title: 'Erreur', description: err?.data?.message || 'Impossible de modifier', color: 'error' })
  } finally {
    saving.value = false
  }
}

// ===== DELETE USER =====
const showDeleteConfirm = ref(false)
const deletingUser = ref<User | null>(null)

function confirmDelete(user: User) {
  deletingUser.value = user
  showDeleteConfirm.value = true
}

async function deleteUser() {
  if (!deletingUser.value) return
  try {
    await apiFetch(`/Users/${deletingUser.value.id}`, { method: 'DELETE' })
    toast.add({ title: 'Utilisateur supprimé', color: 'success', icon: 'i-lucide-trash-2' })
    showDeleteConfirm.value = false
    refresh()
  } catch {
    toast.add({ title: 'Erreur de suppression', color: 'error' })
  }
}
</script>

<template>
  <div class="space-y-6">
    <!-- Header -->
    <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
      <div class="flex items-center gap-3">
        <UIcon name="i-lucide-user-cog" class="w-8 h-8 text-gray-400" />
        <div>
          <h1 class="text-3xl font-bold text-gray-900">Utilisateurs</h1>
          <p class="text-sm text-gray-500 mt-1">Gestion des comptes et rôles</p>
        </div>
      </div>
      <UButton
        v-if="isAdmin"
        icon="i-lucide-plus"
        class="bg-blue-600 hover:bg-blue-700"
        @click="openCreateModal"
      >
        Nouvel utilisateur
      </UButton>
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
              <th class="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase tracking-wider">Permissions</th>
              <th class="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase tracking-wider">Créé le</th>
              <th v-if="isAdmin" class="px-4 py-3 text-right text-xs font-semibold text-gray-600 uppercase tracking-wider">Actions</th>
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
              <td class="px-4 py-3 text-xs text-gray-500 max-w-[250px]">
                {{ roleDescriptions[u.role] || '—' }}
              </td>
              <td class="px-4 py-3 text-gray-600">{{ new Date(u.createdAt).toLocaleDateString('fr-FR') }}</td>
              <td v-if="isAdmin" class="px-4 py-3">
                <div class="flex items-center justify-end gap-1">
                  <button
                    class="h-8 w-8 inline-flex items-center justify-center rounded-lg text-gray-500 hover:text-blue-600 hover:bg-blue-50 transition-colors"
                    title="Modifier le rôle"
                    @click="openEditModal(u)"
                  >
                    <UIcon name="i-lucide-pencil" class="w-4 h-4" />
                  </button>
                  <button
                    class="h-8 w-8 inline-flex items-center justify-center rounded-lg text-gray-500 hover:text-red-600 hover:bg-red-50 transition-colors"
                    title="Supprimer"
                    @click="confirmDelete(u)"
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

    <!-- CREATE MODAL -->
    <UModal v-model:open="showCreateModal">
      <template #header>
        <h3 class="text-lg font-semibold text-gray-900">Nouvel utilisateur</h3>
      </template>
      <template #body>
        <form class="space-y-4" @submit.prevent="createUser">
          <div class="space-y-1">
            <label class="text-sm font-medium text-gray-700">Email *</label>
            <UInput v-model="createForm.email" type="email" placeholder="utilisateur@dkpt.com" required />
          </div>
          <div class="space-y-1">
            <label class="text-sm font-medium text-gray-700">Mot de passe *</label>
            <UInput v-model="createForm.password" type="password" placeholder="Min. 8 caractères" required />
          </div>
          <div class="space-y-1">
            <label class="text-sm font-medium text-gray-700">Rôle</label>
            <USelect v-model="createForm.role" :items="roleItems" />
          </div>
          <div class="flex justify-end gap-3 pt-4 border-t border-gray-100">
            <UButton variant="outline" type="button" @click="showCreateModal = false">Annuler</UButton>
            <UButton type="submit" :loading="creating" icon="i-lucide-save" class="bg-blue-600 hover:bg-blue-700">Créer</UButton>
          </div>
        </form>
      </template>
    </UModal>

    <!-- EDIT MODAL -->
    <UModal v-model:open="showEditModal">
      <template #header>
        <h3 class="text-lg font-semibold text-gray-900">Modifier le rôle</h3>
      </template>
      <template #body>
        <div class="space-y-4">
          <p class="text-sm text-gray-600">
            Modifier le rôle de <strong>{{ editingUser?.email }}</strong>
          </p>
          <div class="space-y-1">
            <label class="text-sm font-medium text-gray-700">Rôle</label>
            <USelect v-model="editForm.role" :items="roleItems" />
          </div>
          <div class="flex justify-end gap-3 pt-4 border-t border-gray-100">
            <UButton variant="outline" @click="showEditModal = false">Annuler</UButton>
            <UButton :loading="saving" icon="i-lucide-save" class="bg-blue-600 hover:bg-blue-700" @click="updateUser">Enregistrer</UButton>
          </div>
        </div>
      </template>
    </UModal>

    <!-- DELETE CONFIRM -->
    <UModal v-model:open="showDeleteConfirm">
      <template #header>
        <h3 class="text-lg font-semibold text-red-600">Confirmer la suppression</h3>
      </template>
      <template #body>
        <p class="text-sm text-gray-600">
          Supprimer l'utilisateur <strong>{{ deletingUser?.email }}</strong> ({{ deletingUser?.role }}) ?
          Cette action est irréversible.
        </p>
        <div class="flex justify-end gap-3 pt-4 mt-4 border-t border-gray-100">
          <UButton variant="outline" @click="showDeleteConfirm = false">Annuler</UButton>
          <UButton color="error" icon="i-lucide-trash-2" @click="deleteUser">Supprimer</UButton>
        </div>
      </template>
    </UModal>
  </div>
</template>
