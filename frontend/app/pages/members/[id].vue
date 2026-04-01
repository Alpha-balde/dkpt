<script setup lang="ts">
import type { Member, Payment, PagedResult } from '~/types'

const { apiFetch } = useApi()
const route = useRoute()
const id = route.params.id as string

const { data: member } = await useAsyncData(`member-${id}`, () =>
  apiFetch<Member>(`/Members/${id}`)
)

const { data: payments } = await useAsyncData(`member-payments-${id}`, () =>
  apiFetch<PagedResult<Payment>>(`/Payments?memberId=${id}&page=1&pageSize=50`)
)

const totalPaid = computed(() =>
  payments.value?.items?.reduce((sum, p) => sum + p.montant, 0) || 0
)

const activeTab = ref('payments')

const infoFields = computed(() => {
  if (!member.value) return []
  return [
    { label: 'N° Membre', value: member.value.numeroMembre },
    { label: 'Statut', value: member.value.actif ? 'Actif' : 'Inactif', badge: true, active: member.value.actif },
    { label: 'Téléphone', value: member.value.telephone || '—' },
    { label: 'WhatsApp', value: member.value.whatsApp || '—' },
    { label: 'Village', value: member.value.village || '—' },
    { label: 'Sous-préfecture', value: member.value.sousPrefecture || '—' },
    { label: 'Résidence', value: member.value.residence || '—' },
    { label: 'Année d\'adhésion', value: member.value.anneeDebut?.toString() || '—' },
    { label: 'Date création', value: member.value.createdAt ? new Date(member.value.createdAt).toLocaleDateString('fr-FR') : '—' }
  ]
})
</script>

<template>
  <div class="space-y-6">
    <!-- Navigation -->
    <div class="flex items-center justify-between">
      <button
        class="flex items-center gap-2 text-sm text-gray-600 hover:text-gray-900 transition-colors"
        @click="navigateTo('/members')"
      >
        <UIcon name="i-lucide-arrow-left" class="w-4 h-4" />
        Retour à la liste
      </button>
      <span
        v-if="member"
        :class="member.actif ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'"
        class="inline-flex items-center rounded-full px-3 py-1 text-xs font-medium"
      >
        {{ member.actif ? 'Actif' : 'Inactif' }}
      </span>
    </div>

    <!-- Member Name -->
    <div v-if="member">
      <h1 class="text-3xl font-bold text-gray-900">{{ member.prenom }} {{ member.nom }}</h1>
      <p class="text-sm text-gray-500 mt-1">{{ member.numeroMembre }}</p>
    </div>

    <!-- Info Card -->
    <div class="bg-white rounded-xl shadow-sm border border-gray-100 p-6">
      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
        <div v-for="field in infoFields" :key="field.label">
          <p class="text-xs font-medium text-gray-500 uppercase tracking-wider">{{ field.label }}</p>
          <div class="mt-1">
            <span
              v-if="field.badge"
              :class="field.active ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'"
              class="inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium"
            >
              {{ field.value }}
            </span>
            <p v-else class="text-sm font-semibold text-gray-900">{{ field.value }}</p>
          </div>
        </div>
      </div>
    </div>

    <!-- KPI Cards -->
    <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
      <div class="bg-white rounded-xl p-6 shadow-sm border border-gray-100">
        <div class="flex items-center justify-between">
          <div>
            <p class="text-sm text-gray-500 font-medium">Total cotisations payées</p>
            <p class="text-3xl font-bold text-gray-900 mt-1">{{ totalPaid.toLocaleString('fr-FR') }} GNF</p>
            <p class="text-xs text-gray-400 mt-1">{{ payments?.totalCount || 0 }} paiements</p>
          </div>
          <div class="p-3 rounded-full bg-green-50">
            <UIcon name="i-lucide-credit-card" class="w-6 h-6 text-green-500" />
          </div>
        </div>
      </div>
      <div class="bg-white rounded-xl p-6 shadow-sm border border-gray-100">
        <div class="flex items-center justify-between">
          <div>
            <p class="text-sm text-gray-500 font-medium">Total arriérés</p>
            <p class="text-3xl font-bold text-gray-900 mt-1">0 GNF</p>
            <p class="text-xs text-gray-400 mt-1">Calcul à implémenter</p>
          </div>
          <div class="p-3 rounded-full bg-pink-50">
            <UIcon name="i-lucide-alert-circle" class="w-6 h-6 text-pink-500" />
          </div>
        </div>
      </div>
    </div>

    <!-- Tabs -->
    <div class="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden">
      <div class="border-b border-gray-200">
        <nav class="flex -mb-px">
          <button
            v-for="tab in [
              { key: 'payments', label: 'Historique Paiements', icon: 'i-lucide-credit-card' },
              { key: 'cotisations', label: 'Cotisations', icon: 'i-lucide-wallet' },
              { key: 'arrieres', label: 'Arriérés', icon: 'i-lucide-alert-circle' }
            ]"
            :key="tab.key"
            class="flex items-center gap-2 px-6 py-3 text-sm font-medium border-b-2 transition-colors"
            :class="activeTab === tab.key
              ? 'border-blue-600 text-blue-600'
              : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'"
            @click="activeTab = tab.key"
          >
            <UIcon :name="tab.icon" class="w-4 h-4" />
            {{ tab.label }}
          </button>
        </nav>
      </div>

      <!-- Tab: Payments -->
      <div v-if="activeTab === 'payments'">
        <table v-if="payments?.items?.length" class="w-full text-sm">
          <thead>
            <tr class="bg-gray-50 border-b border-gray-200">
              <th class="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase">Date</th>
              <th class="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase">Année</th>
              <th class="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase">Montant</th>
              <th class="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase">Moyen</th>
              <th class="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase">Référence</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-100">
            <tr v-for="p in payments.items" :key="p.id" class="hover:bg-gray-50 transition-colors">
              <td class="px-4 py-3 text-gray-600">{{ p.datePaiement }}</td>
              <td class="px-4 py-3">
                <span class="inline-flex items-center rounded-full bg-blue-50 text-blue-700 px-2.5 py-0.5 text-xs font-medium">
                  {{ p.annee }}
                </span>
              </td>
              <td class="px-4 py-3 font-bold text-gray-900">{{ p.montant.toLocaleString('fr-FR') }} GNF</td>
              <td class="px-4 py-3">
                <span class="inline-flex items-center rounded-full bg-green-50 text-green-700 px-2.5 py-0.5 text-xs font-medium">
                  {{ p.moyenPaiement }}
                </span>
              </td>
              <td class="px-4 py-3 text-gray-500">{{ p.reference || '—' }}</td>
            </tr>
          </tbody>
        </table>
        <div v-else class="p-12 text-center">
          <div class="bg-gray-50 rounded-lg border border-dashed border-gray-200 p-8 max-w-sm mx-auto">
            <UIcon name="i-lucide-credit-card" class="w-12 h-12 text-gray-200 mx-auto" />
            <p class="text-sm text-gray-500 mt-3">Aucun paiement enregistré</p>
          </div>
        </div>
      </div>

      <!-- Tab: Cotisations / Arriérés placeholder -->
      <div v-if="activeTab === 'cotisations' || activeTab === 'arrieres'" class="p-12 text-center">
        <div class="bg-gray-50 rounded-lg border border-dashed border-gray-200 p-8 max-w-sm mx-auto">
          <UIcon name="i-lucide-construction" class="w-12 h-12 text-gray-200 mx-auto" />
          <p class="text-sm text-gray-500 mt-3">Fonctionnalité à venir</p>
        </div>
      </div>
    </div>
  </div>
</template>
