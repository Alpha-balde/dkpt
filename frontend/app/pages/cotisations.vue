<script setup lang="ts">
import type { PagedResult } from '~/types'

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
const { canManagePayments } = useAuth()

const page = ref(1)
const pageSize = 20
const statusFilter = ref('all')
const search = ref('')

const currentYear = new Date().getFullYear()

// Available years
const { data: availableYears } = await useAsyncData('cotis-years', () =>
  apiFetch<number[]>('/Settings/years').catch(() => [] as number[])
)

const yearItems = computed(() => {
  const items = [{ label: 'Toutes les années', value: 0 }]
  if (availableYears.value) {
    for (const y of availableYears.value) {
      items.push({ label: y.toString(), value: y })
    }
  }
  if (!items.find(i => i.value === currentYear)) {
    items.push({ label: currentYear.toString(), value: currentYear })
  }
  return items.sort((a, b) => b.value - a.value)
})

// Default to current year
const selectedYear = ref(currentYear)

// Dynamic subtitle
const subtitleYear = computed(() =>
  selectedYear.value > 0 ? selectedYear.value.toString() : 'Toutes les années'
)

const statusItems = [
  { label: 'Tous', value: 'all' },
  { label: 'En ordre', value: 'En ordre' },
  { label: 'Pas en ordre', value: 'Pas en ordre' }
]

const { data, refresh, status: fetchStatus } = await useAsyncData(
  'cotisations-list',
  () => {
    const params = new URLSearchParams()
    params.set('page', page.value.toString())
    params.set('pageSize', pageSize.toString())
    if (selectedYear.value > 0) params.set('year', selectedYear.value.toString())
    if (statusFilter.value !== 'all') params.set('status', statusFilter.value)
    if (search.value) params.set('search', search.value)
    return apiFetch<PagedResult<CotisationStat>>(`/Cotisations?${params.toString()}`)
  },
  { watch: [page] }
)

const totalPages = computed(() => Math.ceil((data.value?.totalCount || 0) / pageSize))

function onFilterChange() {
  page.value = 1
  refresh()
}
</script>

<template>
  <div class="space-y-6">
    <!-- Header -->
    <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
      <div>
        <h1 class="text-3xl font-bold text-gray-900">Cotisations</h1>
        <p class="text-sm text-gray-500 mt-1">Suivi des cotisations – {{ subtitleYear }}</p>
      </div>
      <UButton
        v-if="canManagePayments"
        icon="i-lucide-plus"
        class="bg-blue-600 hover:bg-blue-700"
        @click="navigateTo('/payments')"
      >
        Nouveau paiement
      </UButton>
    </div>

    <!-- Filters with labels -->
    <div class="bg-white rounded-xl p-6 shadow-sm border border-gray-100">
      <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div class="space-y-1.5">
          <label class="text-sm font-medium text-gray-700">Année</label>
          <USelect
            v-model="selectedYear"
            :items="yearItems"
            value-key="value"
            label-key="label"
            @update:model-value="onFilterChange"
          />
        </div>
        <div class="space-y-1.5">
          <label class="text-sm font-medium text-gray-700">Statut</label>
          <USelect
            v-model="statusFilter"
            :items="statusItems"
            @update:model-value="onFilterChange"
          />
        </div>
        <div class="space-y-1.5">
          <label class="text-sm font-medium text-gray-700">Recherche</label>
          <UInput
            v-model="search"
            placeholder="Nom, numéro..."
            icon="i-lucide-search"
            @keyup.enter="onFilterChange"
          />
        </div>
      </div>
    </div>

    <!-- Table -->
    <div class="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden">
      <div class="px-6 py-4 border-b border-gray-100">
        <h3 class="text-lg font-semibold text-gray-900">Historique</h3>
        <p class="text-xs text-gray-500">{{ data?.totalCount || 0 }} résultats trouvés</p>
      </div>
      <div class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead>
            <tr class="bg-gray-50 border-b border-gray-200">
              <th class="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase tracking-wider">Membre</th>
              <th class="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase tracking-wider">Payé</th>
              <th class="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase tracking-wider">Attendu</th>
              <th class="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase tracking-wider">Reste</th>
              <th class="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase tracking-wider">Statut</th>
              <th class="px-4 py-3 text-right text-xs font-semibold text-gray-600 uppercase tracking-wider">Action</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-100">
            <tr
              v-for="row in data?.items"
              :key="row.id"
              class="hover:bg-gray-50 transition-colors"
            >
              <td class="px-4 py-3">
                <NuxtLink :to="`/members/${row.id}`" class="group">
                  <p class="font-medium text-gray-900 group-hover:text-blue-600 transition-colors">
                    {{ row.prenom }} {{ row.nom }}
                  </p>
                  <p class="text-xs text-gray-500">{{ row.numeroMembre }}</p>
                </NuxtLink>
              </td>
              <td class="px-4 py-3 font-medium text-green-600">{{ row.paidAmount.toLocaleString('fr-FR') }} GNF</td>
              <td class="px-4 py-3 text-gray-600">{{ row.expectedAmount.toLocaleString('fr-FR') }} GNF</td>
              <td class="px-4 py-3">
                <span :class="row.gap > 0 ? 'font-bold text-red-600' : 'font-medium text-green-600'">
                  {{ row.gap.toLocaleString('fr-FR') }} GNF
                </span>
              </td>
              <td class="px-4 py-3">
                <span
                  :class="row.status === 'En ordre'
                    ? 'bg-green-100 text-green-800'
                    : 'bg-red-100 text-red-800'"
                  class="inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium"
                >
                  {{ row.status }}
                </span>
              </td>
              <td class="px-4 py-3 text-right">
                <UButton
                  v-if="row.status !== 'En ordre'"
                  size="xs"
                  class="bg-blue-600 hover:bg-blue-700"
                  icon="i-lucide-credit-card"
                  @click="navigateTo(`/payments?memberId=${row.id}`)"
                >
                  Payer
                </UButton>
                <span v-else class="text-xs text-gray-400">—</span>
              </td>
            </tr>
            <tr v-if="fetchStatus === 'pending'">
              <td colspan="6" class="px-4 py-12 text-center">
                <UIcon name="i-lucide-loader-2" class="w-6 h-6 animate-spin text-gray-400 mx-auto" />
              </td>
            </tr>
            <tr v-else-if="!data?.items?.length">
              <td colspan="6" class="px-4 py-12 text-center text-gray-500">
                Aucun résultat trouvé
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Pagination -->
    <div v-if="totalPages > 1" class="flex justify-center">
      <UPagination v-model="page" :total="data?.totalCount || 0" :items-per-page="pageSize" />
    </div>
  </div>
</template>
