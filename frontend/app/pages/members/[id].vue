<script setup lang="ts">
import type { Member, Payment, PagedResult } from '~/types'

interface CotisationStat {
  id: string
  numeroMembre: string
  prenom: string
  nom: string
  paidAmount: number
  expectedAmount: number
  gap: number
  status: string
}

const { apiFetch } = useApi()
const route = useRoute()
const id = route.params.id as string

const { data: member } = await useAsyncData(`member-${id}`, () =>
  apiFetch<Member>(`/Members/${id}`)
)

const { data: payments } = await useAsyncData(`member-payments-${id}`, () =>
  apiFetch<Payment[]>(`/Payments/member/${id}`)
)

// Cotisation stats for this member (all years)
const { data: cotisStats } = await useAsyncData(`member-cotis-${id}`, () =>
  apiFetch<PagedResult<CotisationStat>>(`/Cotisations?page=1&pageSize=1&search=${id}`).catch(() => null)
)

// Available years for contributions
const { data: availableYears } = await useAsyncData('member-years', () =>
  apiFetch<number[]>('/Settings/years').catch(() => [] as number[])
)

// Settings contributions for expected amounts
const { data: contributions } = await useAsyncData('member-contributions', () =>
  apiFetch<{ year: number, amount: number }[]>('/Settings/contributions').catch(() => [])
)

// Settings default amount
const { data: settingRaw } = await useAsyncData('member-setting', () =>
  apiFetch<any>('/Settings').catch(() => null)
)
const defaultAmount = computed(() => settingRaw.value?.montantCotisationAnnuelleParDefaut || 60000)

const totalPaid = computed(() =>
  payments.value?.reduce((sum, p) => sum + p.montant, 0) || 0
)

// Calculate per-year data
const yearData = computed(() => {
  if (!member.value || !payments.value) return []
  const amountMap = new Map<number, number>()
  if (contributions.value) {
    for (const c of contributions.value) {
      amountMap.set(c.year, c.amount)
    }
  }

  const currentYear = new Date().getFullYear()
  const startYear = member.value.anneeDebut || currentYear
  const years = []

  for (let y = currentYear; y >= startYear; y--) {
    const expected = amountMap.get(y) || defaultAmount.value
    const paid = payments.value
      .filter(p => p.annee === y)
      .reduce((sum, p) => sum + p.montant, 0)
    const gap = Math.max(expected - paid, 0)
    years.push({
      year: y,
      expected,
      paid,
      gap,
      status: gap === 0 ? 'En ordre' : 'Pas en ordre'
    })
  }
  return years
})

const totalExpected = computed(() => yearData.value.reduce((s, y) => s + y.expected, 0))
const totalGap = computed(() => Math.max(totalExpected.value - totalPaid.value, 0))

const arrearsYears = computed(() => yearData.value.filter(y => y.gap > 0))

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
    <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
      <div class="bg-white rounded-xl p-6 shadow-sm border border-gray-100">
        <div class="flex items-center justify-between">
          <div>
            <p class="text-sm text-gray-500 font-medium">Total payé</p>
            <p class="text-2xl font-bold text-green-600 mt-1">{{ totalPaid.toLocaleString('fr-FR') }} GNF</p>
            <p class="text-xs text-gray-400 mt-1">{{ payments?.length || 0 }} paiements</p>
          </div>
          <div class="p-3 rounded-full bg-green-50">
            <UIcon name="i-lucide-credit-card" class="w-6 h-6 text-green-500" />
          </div>
        </div>
      </div>
      <div class="bg-white rounded-xl p-6 shadow-sm border border-gray-100">
        <div class="flex items-center justify-between">
          <div>
            <p class="text-sm text-gray-500 font-medium">Total attendu</p>
            <p class="text-2xl font-bold text-gray-900 mt-1">{{ totalExpected.toLocaleString('fr-FR') }} GNF</p>
            <p class="text-xs text-gray-400 mt-1">{{ yearData.length }} années</p>
          </div>
          <div class="p-3 rounded-full bg-blue-50">
            <UIcon name="i-lucide-wallet" class="w-6 h-6 text-blue-500" />
          </div>
        </div>
      </div>
      <div class="bg-white rounded-xl p-6 shadow-sm border border-gray-100">
        <div class="flex items-center justify-between">
          <div>
            <p class="text-sm text-gray-500 font-medium">Arriérés</p>
            <p class="text-2xl font-bold mt-1" :class="totalGap > 0 ? 'text-red-600' : 'text-green-600'">
              {{ totalGap.toLocaleString('fr-FR') }} GNF
            </p>
            <p class="text-xs text-gray-400 mt-1">{{ arrearsYears.length }} année(s) en retard</p>
          </div>
          <div class="p-3 rounded-full" :class="totalGap > 0 ? 'bg-red-50' : 'bg-green-50'">
            <UIcon name="i-lucide-alert-circle" class="w-6 h-6" :class="totalGap > 0 ? 'text-red-500' : 'text-green-500'" />
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
              { key: 'payments', label: 'Paiements', icon: 'i-lucide-credit-card', count: payments?.length || 0 },
              { key: 'cotisations', label: 'Cotisations', icon: 'i-lucide-wallet', count: yearData.length },
              { key: 'arrieres', label: 'Arriérés', icon: 'i-lucide-alert-circle', count: arrearsYears.length }
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
            <span class="ml-1 text-xs bg-gray-100 text-gray-600 rounded-full px-2 py-0.5">{{ tab.count }}</span>
          </button>
        </nav>
      </div>

      <!-- Tab: Payments -->
      <div v-if="activeTab === 'payments'">
        <table v-if="payments?.length" class="w-full text-sm">
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
            <tr v-for="p in payments" :key="p.id" class="hover:bg-gray-50 transition-colors">
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
          <UIcon name="i-lucide-credit-card" class="w-12 h-12 text-gray-200 mx-auto" />
          <p class="text-sm text-gray-500 mt-3">Aucun paiement enregistré</p>
        </div>
      </div>

      <!-- Tab: Cotisations (year breakdown) -->
      <div v-if="activeTab === 'cotisations'">
        <table v-if="yearData.length" class="w-full text-sm">
          <thead>
            <tr class="bg-gray-50 border-b border-gray-200">
              <th class="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase">Année</th>
              <th class="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase">Attendu</th>
              <th class="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase">Payé</th>
              <th class="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase">Reste</th>
              <th class="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase">Statut</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-100">
            <tr v-for="y in yearData" :key="y.year" class="hover:bg-gray-50 transition-colors">
              <td class="px-4 py-3">
                <span class="inline-flex items-center rounded-full bg-blue-50 text-blue-700 px-2.5 py-0.5 text-xs font-bold">{{ y.year }}</span>
              </td>
              <td class="px-4 py-3 text-gray-600">{{ y.expected.toLocaleString('fr-FR') }} GNF</td>
              <td class="px-4 py-3 font-medium text-green-600">{{ y.paid.toLocaleString('fr-FR') }} GNF</td>
              <td class="px-4 py-3">
                <span :class="y.gap > 0 ? 'font-bold text-red-600' : 'font-medium text-green-600'">
                  {{ y.gap.toLocaleString('fr-FR') }} GNF
                </span>
              </td>
              <td class="px-4 py-3">
                <span
                  :class="y.status === 'En ordre' ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'"
                  class="inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium"
                >
                  {{ y.status }}
                </span>
              </td>
            </tr>
          </tbody>
        </table>
        <div v-else class="p-12 text-center">
          <UIcon name="i-lucide-wallet" class="w-12 h-12 text-gray-200 mx-auto" />
          <p class="text-sm text-gray-500 mt-3">Aucune cotisation attendue</p>
        </div>
      </div>

      <!-- Tab: Arriérés -->
      <div v-if="activeTab === 'arrieres'">
        <div v-if="arrearsYears.length">
          <!-- Summary banner -->
          <div class="bg-gradient-to-r from-red-500 to-pink-500 text-white p-4 flex items-center justify-between">
            <div>
              <p class="text-sm font-medium opacity-90">Total des arriérés</p>
              <p class="text-2xl font-bold">{{ totalGap.toLocaleString('fr-FR') }} GNF</p>
            </div>
            <UButton
              class="bg-white/20 hover:bg-white/30 text-white"
              icon="i-lucide-credit-card"
              @click="navigateTo(`/payments?memberId=${id}`)"
            >
              Régulariser
            </UButton>
          </div>
          <table class="w-full text-sm">
            <thead>
              <tr class="bg-gray-50 border-b border-gray-200">
                <th class="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase">Année</th>
                <th class="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase">Montant dû</th>
                <th class="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase">Déjà payé</th>
                <th class="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase">Reste à payer</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-100">
              <tr v-for="y in arrearsYears" :key="y.year" class="hover:bg-gray-50 transition-colors">
                <td class="px-4 py-3">
                  <span class="inline-flex items-center rounded-full bg-red-50 text-red-700 px-2.5 py-0.5 text-xs font-bold">{{ y.year }}</span>
                </td>
                <td class="px-4 py-3 text-gray-600">{{ y.expected.toLocaleString('fr-FR') }} GNF</td>
                <td class="px-4 py-3 font-medium text-green-600">{{ y.paid.toLocaleString('fr-FR') }} GNF</td>
                <td class="px-4 py-3 font-bold text-red-600">{{ y.gap.toLocaleString('fr-FR') }} GNF</td>
              </tr>
            </tbody>
          </table>
        </div>
        <div v-else class="p-12 text-center">
          <div class="inline-flex items-center justify-center w-16 h-16 rounded-full bg-green-50 mb-4">
            <UIcon name="i-lucide-party-popper" class="w-8 h-8 text-green-500" />
          </div>
          <p class="text-lg font-semibold text-gray-900">Aucun arriéré !</p>
          <p class="text-sm text-gray-500 mt-1">Ce membre est à jour sur toutes ses cotisations 🎉</p>
        </div>
      </div>
    </div>
  </div>
</template>
