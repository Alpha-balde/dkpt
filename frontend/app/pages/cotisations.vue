<script setup lang="ts">
import type { ContributionAmount } from '~/types'

const { apiFetch } = useApi()
const { isAdmin } = useAuth()
const toast = useToast()

const { data: contributions, refresh } = await useAsyncData('cotisations', () =>
  apiFetch<ContributionAmount[]>('/ContributionAmounts')
)

// Modals
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
    await apiFetch('/ContributionAmounts', { method: 'POST', body: addForm })
    toast.add({ title: 'Année ajoutée', color: 'success', icon: 'i-lucide-check-circle' })
    showAddModal.value = false
    refresh()
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
    await apiFetch(`/ContributionAmounts/${editForm.year}`, { method: 'PUT', body: { amount: editForm.amount } })
    toast.add({ title: 'Montant mis à jour', color: 'success', icon: 'i-lucide-check-circle' })
    showEditModal.value = false
    refresh()
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
    await apiFetch(`/ContributionAmounts/${deleteYear.value}`, { method: 'DELETE' })
    toast.add({ title: 'Année supprimée', color: 'success', icon: 'i-lucide-trash-2' })
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
        <UIcon name="i-lucide-settings" class="w-8 h-8 text-gray-400" />
        <div>
          <h1 class="text-3xl font-bold text-gray-900">Paramètres des cotisations</h1>
          <p class="text-sm text-gray-500 mt-1">Montants annuels de cotisation</p>
        </div>
      </div>
      <UButton
        v-if="isAdmin"
        icon="i-lucide-plus"
        class="bg-blue-600 hover:bg-blue-700"
        @click="showAddModal = true"
      >
        Nouvelle année
      </UButton>
    </div>

    <!-- Table -->
    <div class="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden">
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
              <div v-if="isAdmin" class="flex items-center justify-end gap-1">
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
          <h3 class="text-sm font-semibold text-blue-900">Montant par défaut</h3>
          <p class="text-sm text-blue-800 mt-1">
            Le montant de cotisation par défaut est de <strong>60 000 GNF</strong>. 
            Vous pouvez définir un montant différent pour chaque année en ajoutant une entrée dans le tableau ci-dessus.
          </p>
        </div>
      </div>
    </div>

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
        <h3 class="text-lg font-semibold text-gray-900">Modifier le montant — {{ editForm.year }}</h3>
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
          Êtes-vous sûr de vouloir supprimer le montant de cotisation pour l'année <strong>{{ deleteYear }}</strong> ?
          Cette action est irréversible.
        </p>
        <div class="flex justify-end gap-3 pt-4 mt-4 border-t border-gray-100">
          <UButton variant="outline" @click="showDeleteConfirm = false">Annuler</UButton>
          <UButton color="error" icon="i-lucide-trash-2" @click="deleteContribution">Supprimer</UButton>
        </div>
      </template>
    </UModal>
  </div>
</template>
