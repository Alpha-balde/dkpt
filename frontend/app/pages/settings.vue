<script setup lang="ts">
import type { ContributionAmount } from '~/types'

const { apiFetch } = useApi()
const { isAdmin } = useAuth()
const toast = useToast()

// --- Default amount ---
const { data: settingRaw, refresh: refreshSetting } = await useAsyncData('setting-default', () =>
  apiFetch<any>('/Settings').catch(() => null)
)
const defaultAmount = computed(() => settingRaw.value?.montantCotisationAnnuelleParDefaut || 60000)

// --- Contributions table ---
const { data: contributions, refresh: refreshContrib } = await useAsyncData('setting-contributions', () =>
  apiFetch<ContributionAmount[]>('/Settings/contributions')
)

// --- Modals ---
const showAddModal = ref(false)
const showEditModal = ref(false)
const showDeleteConfirm = ref(false)

const addForm = reactive({ year: new Date().getFullYear(), amount: 60000 })
const editForm = reactive({ year: 0, amount: 0 })
const deleteYear = ref(0)
const saving = ref(false)

async function addContribution() {
  saving.value = true
  try {
    await apiFetch('/Settings/contributions', { method: 'POST', body: addForm })
    toast.add({ title: 'Année ajoutée', color: 'success', icon: 'i-lucide-check-circle' })
    showAddModal.value = false
    addForm.year = new Date().getFullYear()
    addForm.amount = 60000
    refreshContrib()
  } catch (err: any) {
    toast.add({ title: 'Erreur', description: err?.data?.message || 'Impossible d\'ajouter', color: 'error' })
  } finally {
    saving.value = false
  }
}

function openEdit(c: ContributionAmount) {
  editForm.year = c.year
  editForm.amount = c.amount
  showEditModal.value = true
}

async function updateContribution() {
  saving.value = true
  try {
    await apiFetch(`/Settings/contributions/${editForm.year}`, { method: 'PUT', body: { amount: editForm.amount } })
    toast.add({ title: 'Montant mis à jour', color: 'success', icon: 'i-lucide-check-circle' })
    showEditModal.value = false
    refreshContrib()
  } catch (err: any) {
    toast.add({ title: 'Erreur', description: err?.data?.message || 'Impossible de modifier', color: 'error' })
  } finally {
    saving.value = false
  }
}

function confirmDelete(year: number) {
  deleteYear.value = year
  showDeleteConfirm.value = true
}

async function deleteContribution() {
  try {
    await apiFetch(`/Settings/contributions/${deleteYear.value}`, { method: 'DELETE' })
    toast.add({ title: 'Année supprimée', color: 'success', icon: 'i-lucide-trash-2' })
    showDeleteConfirm.value = false
    refreshContrib()
  } catch {
    toast.add({ title: 'Erreur de suppression', color: 'error' })
  }
}
</script>

<template>
  <div class="space-y-6">
    <!-- Access Denied -->
    <div v-if="!isAdmin" class="flex flex-col items-center justify-center py-24">
      <div class="bg-red-50 rounded-full w-20 h-20 flex items-center justify-center">
        <UIcon name="i-lucide-shield-x" class="w-10 h-10 text-red-500" />
      </div>
      <h2 class="text-xl font-bold text-gray-900 mt-6">Accès refusé</h2>
      <p class="text-sm text-gray-500 mt-2 text-center max-w-md">
        Seuls les administrateurs peuvent accéder aux paramètres.
      </p>
      <UButton class="mt-6" variant="outline" @click="navigateTo('/dashboard')">
        <UIcon name="i-lucide-arrow-left" class="w-4 h-4 mr-2" />
        Retour au tableau de bord
      </UButton>
    </div>

    <template v-else>
      <!-- Header -->
      <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div class="flex items-center gap-3">
          <UIcon name="i-lucide-settings" class="w-8 h-8 text-gray-400" />
          <div>
            <h1 class="text-3xl font-bold text-gray-900">Paramètres des cotisations</h1>
            <p class="text-sm text-gray-500 mt-1">Montants annuels et configuration</p>
          </div>
        </div>
        <UButton
          icon="i-lucide-plus"
          class="bg-blue-600 hover:bg-blue-700"
          @click="showAddModal = true"
        >
          Nouvelle année
        </UButton>
      </div>

      <!-- Default Amount Card -->
      <div class="bg-white rounded-xl shadow-sm border border-gray-100 p-6">
        <div class="flex items-center justify-between">
          <div>
            <p class="text-xs font-medium text-gray-500 uppercase tracking-wider">Montant par défaut</p>
            <p class="text-3xl font-bold text-gray-900 mt-1">{{ defaultAmount.toLocaleString('fr-FR') }} GNF</p>
            <p class="text-xs text-gray-400 mt-1">Appliqué quand aucun montant spécifique n'est défini</p>
          </div>
          <div class="p-3 rounded-full bg-blue-50">
            <UIcon name="i-lucide-wallet" class="w-6 h-6 text-blue-500" />
          </div>
        </div>
      </div>

      <!-- Year/Amount Table -->
      <div class="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden">
        <div class="px-6 py-4 border-b border-gray-100">
          <h3 class="text-lg font-semibold text-gray-900">Montants par année</h3>
        </div>
        <table class="w-full text-sm">
          <thead>
            <tr class="bg-gray-50 border-b border-gray-200">
              <th class="px-6 py-3 text-left text-xs font-semibold text-gray-600 uppercase tracking-wider">Année</th>
              <th class="px-6 py-3 text-left text-xs font-semibold text-gray-600 uppercase tracking-wider">Montant (GNF)</th>
              <th class="px-6 py-3 text-right text-xs font-semibold text-gray-600 uppercase tracking-wider">Actions</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-100">
            <tr
              v-for="c in contributions"
              :key="c.year"
              class="hover:bg-gray-50 transition-colors"
            >
              <td class="px-6 py-4 font-bold text-gray-900">{{ c.year }}</td>
              <td class="px-6 py-4 text-gray-900">{{ c.amount.toLocaleString('fr-FR') }} GNF</td>
              <td class="px-6 py-4">
                <div class="flex items-center justify-end gap-1">
                  <button
                    class="h-8 w-8 inline-flex items-center justify-center rounded-lg text-gray-500 hover:text-blue-600 hover:bg-blue-50 transition-colors"
                    title="Modifier"
                    @click="openEdit(c)"
                  >
                    <UIcon name="i-lucide-pencil" class="w-4 h-4" />
                  </button>
                  <button
                    class="h-8 w-8 inline-flex items-center justify-center rounded-lg text-gray-500 hover:text-red-600 hover:bg-red-50 transition-colors"
                    title="Supprimer"
                    @click="confirmDelete(c.year)"
                  >
                    <UIcon name="i-lucide-trash-2" class="w-4 h-4" />
                  </button>
                </div>
              </td>
            </tr>
            <tr v-if="!contributions?.length">
              <td colspan="3" class="px-6 py-12 text-center text-gray-500">Aucun montant configuré</td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Info Box -->
      <div class="bg-blue-50 border border-blue-200 rounded-xl p-6">
        <div class="flex items-start gap-3">
          <UIcon name="i-lucide-info" class="w-5 h-5 text-blue-600 mt-0.5 shrink-0" />
          <div>
            <h3 class="text-sm font-semibold text-blue-900">Comment ça fonctionne</h3>
            <ul class="text-sm text-blue-800 mt-2 space-y-1 list-disc list-inside">
              <li>Le montant par défaut ({{ defaultAmount.toLocaleString('fr-FR') }} GNF) s'applique à toutes les années sans montant spécifique</li>
              <li>Le calcul des arriérés utilise les montants spécifiques quand disponibles</li>
              <li>Consultez le suivi des cotisations dans la page <NuxtLink to="/cotisations" class="underline font-medium">Cotisations</NuxtLink></li>
            </ul>
          </div>
        </div>
      </div>
    </template>

    <!-- Add Modal -->
    <UModal v-model:open="showAddModal">
      <template #header>
        <h3 class="text-lg font-semibold text-gray-900">Ajouter une année</h3>
      </template>
      <template #body>
        <form class="space-y-4" @submit.prevent="addContribution">
          <div class="space-y-1">
            <label class="text-sm font-medium text-gray-700">Année</label>
            <UInput v-model.number="addForm.year" type="number" required />
          </div>
          <div class="space-y-1">
            <label class="text-sm font-medium text-gray-700">Montant (GNF)</label>
            <UInput v-model.number="addForm.amount" type="number" required />
          </div>
          <div class="flex justify-end gap-3 pt-4 border-t border-gray-100">
            <UButton variant="outline" @click="showAddModal = false">Annuler</UButton>
            <UButton type="submit" :loading="saving" icon="i-lucide-save" class="bg-blue-600 hover:bg-blue-700">Enregistrer</UButton>
          </div>
        </form>
      </template>
    </UModal>

    <!-- Edit Modal -->
    <UModal v-model:open="showEditModal">
      <template #header>
        <h3 class="text-lg font-semibold text-gray-900">Modifier — {{ editForm.year }}</h3>
      </template>
      <template #body>
        <form class="space-y-4" @submit.prevent="updateContribution">
          <div class="space-y-1">
            <label class="text-sm font-medium text-gray-700">Montant (GNF)</label>
            <UInput v-model.number="editForm.amount" type="number" required />
          </div>
          <div class="flex justify-end gap-3 pt-4 border-t border-gray-100">
            <UButton variant="outline" @click="showEditModal = false">Annuler</UButton>
            <UButton type="submit" :loading="saving" icon="i-lucide-save" class="bg-blue-600 hover:bg-blue-700">Enregistrer</UButton>
          </div>
        </form>
      </template>
    </UModal>

    <!-- Delete Confirm Modal -->
    <UModal v-model:open="showDeleteConfirm">
      <template #header>
        <h3 class="text-lg font-semibold text-red-600">Confirmer la suppression</h3>
      </template>
      <template #body>
        <p class="text-sm text-gray-600">
          Supprimer le montant pour l'année <strong>{{ deleteYear }}</strong> ? Cette action est irréversible.
        </p>
        <div class="flex justify-end gap-3 pt-4 mt-4 border-t border-gray-100">
          <UButton variant="outline" @click="showDeleteConfirm = false">Annuler</UButton>
          <UButton color="error" icon="i-lucide-trash-2" @click="deleteContribution">Supprimer</UButton>
        </div>
      </template>
    </UModal>
  </div>
</template>
