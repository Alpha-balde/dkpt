<script setup lang="ts">
import type { Payment, PagedResult } from '~/types'

const { apiFetch } = useApi()

// Données — pour le moment on montre les paiements manquants comme placeholder
// L'API d'arriérés dédié n'existe pas encore côté backend
const loading = ref(true)
const arrieres = ref<any[]>([])

onMounted(async () => {
  // Placeholder: affichage d'un message car l'endpoint dédié n'existe pas encore
  loading.value = false
})

const totalArriere = computed(() =>
  arrieres.value.reduce((sum, a) => sum + (a.reste || 0), 0)
)
</script>

<template>
  <div class="space-y-6">
    <!-- Header -->
    <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
      <div>
        <h1 class="text-3xl font-bold text-gray-900">Arriérés</h1>
        <p class="text-sm text-gray-500 mt-1">Membres en retard de paiement</p>
      </div>
    </div>

    <!-- Summary Card : Gradient rouge -->
    <div class="bg-gradient-to-r from-red-500 to-red-600 rounded-xl p-6 shadow-lg text-white">
      <div class="flex items-center justify-between">
        <div>
          <p class="text-sm text-red-100 font-medium">Total des arriérés</p>
          <p class="text-4xl font-bold mt-1">{{ totalArriere.toLocaleString('fr-FR') }} GNF</p>
          <p class="text-xs text-red-200 mt-1">{{ arrieres.length }} membres concernés</p>
        </div>
        <div class="p-4 rounded-full bg-white/10">
          <UIcon name="i-lucide-alert-circle" class="w-8 h-8 text-white" />
        </div>
      </div>
    </div>

    <!-- Filtres -->
    <div class="bg-white rounded-xl p-6 shadow-sm border border-gray-100">
      <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
        <UInput
          placeholder="Rechercher un membre..."
          icon="i-lucide-search"
        />
        <UButton variant="soft" icon="i-lucide-search">
          Rechercher
        </UButton>
      </div>
    </div>

    <!-- Table desktop -->
    <div v-if="arrieres.length" class="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden">
      <table class="w-full text-sm">
        <thead>
          <tr class="bg-gray-50 border-b border-gray-200">
            <th class="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase tracking-wider">Membre</th>
            <th class="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase tracking-wider">Montant dû</th>
            <th class="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase tracking-wider">Déjà payé</th>
            <th class="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase tracking-wider">Reste</th>
            <th class="px-4 py-3 text-right text-xs font-semibold text-gray-600 uppercase tracking-wider">Action</th>
          </tr>
        </thead>
        <tbody class="divide-y divide-gray-100">
          <tr v-for="a in arrieres" :key="a.id" class="hover:bg-gray-50 transition-colors">
            <td class="px-4 py-3">
              <p class="font-medium text-gray-900">{{ a.nom }}</p>
              <p class="text-xs text-gray-500">{{ a.numero }}</p>
            </td>
            <td class="px-4 py-3 text-gray-900">{{ a.montantDu?.toLocaleString('fr-FR') }} GNF</td>
            <td class="px-4 py-3 text-green-600 font-medium">{{ a.dejaPaye?.toLocaleString('fr-FR') }} GNF</td>
            <td class="px-4 py-3 font-bold text-red-600">{{ a.reste?.toLocaleString('fr-FR') }} GNF</td>
            <td class="px-4 py-3 text-right">
              <UButton size="xs" class="bg-blue-600 hover:bg-blue-700">
                Payer
              </UButton>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Empty state -->
    <div v-else-if="!loading" class="bg-white rounded-xl shadow-sm border border-gray-100 p-12 text-center">
      <div class="max-w-sm mx-auto">
        <div class="bg-green-50 rounded-full w-16 h-16 flex items-center justify-center mx-auto">
          <UIcon name="i-lucide-check-circle" class="w-8 h-8 text-green-500" />
        </div>
        <h3 class="text-lg font-semibold text-gray-900 mt-4">Tous les membres sont en ordre ! 🎉</h3>
        <p class="text-sm text-gray-500 mt-2">
          Aucun arriéré détecté. Le calcul métier sera implémenté avec un endpoint dédié côté backend.
        </p>
      </div>
    </div>

    <!-- Info -->
    <div class="bg-amber-50 border border-amber-200 rounded-xl p-6">
      <div class="flex items-start gap-3">
        <UIcon name="i-lucide-construction" class="w-5 h-5 text-amber-600 mt-0.5 shrink-0" />
        <div>
          <h3 class="text-sm font-semibold text-amber-900">Fonctionnalité en cours</h3>
          <p class="text-sm text-amber-800 mt-1">
            Le calcul des arriérés nécessite un endpoint backend dédié qui compare les cotisations attendues avec les paiements effectifs de chaque membre par année.
          </p>
        </div>
      </div>
    </div>
  </div>
</template>
