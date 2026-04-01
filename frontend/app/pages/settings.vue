<script setup lang="ts">
const { apiFetch } = useApi()
const { isAdmin } = useAuth()
const toast = useToast()

const { data: settings, refresh } = await useAsyncData('settings', () =>
  apiFetch<{ montantCotisationAnnuelleParDefaut: number }>('/Settings')
)

const showEditModal = ref(false)
const editAmount = ref(60000)
const saving = ref(false)

function openEdit() {
  editAmount.value = settings.value?.montantCotisationAnnuelleParDefaut || 60000
  showEditModal.value = true
}

async function updateSettings() {
  saving.value = true
  try {
    await apiFetch('/Settings', {
      method: 'PUT',
      body: { montantCotisationAnnuelleParDefaut: editAmount.value }
    })
    toast.add({ title: 'Paramètres mis à jour', color: 'success', icon: 'i-lucide-check-circle' })
    showEditModal.value = false
    refresh()
  } catch (err: any) {
    toast.add({ title: 'Erreur', description: err?.data?.message || 'Impossible de modifier', color: 'error' })
  } finally {
    saving.value = false
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
        Vous n'avez pas les permissions nécessaires pour accéder à cette page.
        Seuls les administrateurs peuvent modifier les paramètres.
      </p>
      <UButton class="mt-6" variant="outline" @click="navigateTo('/dashboard')">
        <UIcon name="i-lucide-arrow-left" class="w-4 h-4 mr-2" />
        Retour au tableau de bord
      </UButton>
    </div>

    <!-- Settings Content -->
    <template v-else>
      <!-- Header -->
      <div class="flex items-center gap-3">
        <UIcon name="i-lucide-settings" class="w-8 h-8 text-gray-400" />
        <div>
          <h1 class="text-3xl font-bold text-gray-900">Paramètres</h1>
          <p class="text-sm text-gray-500 mt-1">Configuration générale de l'application</p>
        </div>
      </div>

      <!-- Settings Card -->
      <div class="bg-white rounded-xl shadow-sm border border-gray-100 p-6">
        <div class="flex items-center justify-between">
          <div>
            <h3 class="text-sm font-semibold text-gray-900">Montant de cotisation annuelle par défaut</h3>
            <p class="text-3xl font-bold text-gray-900 mt-2">
              {{ (settings?.montantCotisationAnnuelleParDefaut || 0).toLocaleString('fr-FR') }} GNF
            </p>
            <p class="text-xs text-gray-500 mt-1">
              Ce montant s'applique quand aucun montant spécifique n'est défini pour une année
            </p>
          </div>
          <div class="flex items-center gap-2">
            <div class="p-3 rounded-full bg-blue-50">
              <UIcon name="i-lucide-wallet" class="w-6 h-6 text-blue-500" />
            </div>
            <button
              class="h-8 w-8 inline-flex items-center justify-center rounded-lg text-gray-500 hover:text-blue-600 hover:bg-blue-50 transition-colors"
              title="Modifier"
              @click="openEdit"
            >
              <UIcon name="i-lucide-pencil" class="w-4 h-4" />
            </button>
          </div>
        </div>
      </div>

      <!-- Info Box -->
      <div class="bg-blue-50 border border-blue-200 rounded-xl p-6">
        <div class="flex items-start gap-3">
          <UIcon name="i-lucide-info" class="w-5 h-5 text-blue-600 mt-0.5 shrink-0" />
          <div>
            <h3 class="text-sm font-semibold text-blue-900">Comment ça fonctionne</h3>
            <ul class="text-sm text-blue-800 mt-2 space-y-1 list-disc list-inside">
              <li>Le montant par défaut s'applique à toutes les années sans montant spécifique</li>
              <li>Vous pouvez définir des montants par année dans la page <NuxtLink to="/cotisations" class="underline font-medium">Cotisations</NuxtLink></li>
              <li>Le calcul des arriérés utilise les montants spécifiques quand disponibles, sinon le montant par défaut</li>
            </ul>
          </div>
        </div>
      </div>
    </template>

    <!-- Edit Modal -->
    <UModal v-model:open="showEditModal">
      <template #header>
        <h3 class="text-lg font-semibold text-gray-900">Modifier le montant par défaut</h3>
      </template>
      <template #body>
        <form class="space-y-4" @submit.prevent="updateSettings">
          <div class="space-y-1">
            <label class="text-sm font-medium text-gray-700">Montant annuel (GNF)</label>
            <UInput v-model.number="editAmount" type="number" required />
          </div>
          <div class="flex justify-end gap-3 pt-4 border-t border-gray-100">
            <UButton variant="outline" @click="showEditModal = false">Annuler</UButton>
            <UButton type="submit" :loading="saving" icon="i-lucide-save" class="bg-blue-600 hover:bg-blue-700">
              Enregistrer
            </UButton>
          </div>
        </form>
      </template>
    </UModal>
  </div>
</template>
