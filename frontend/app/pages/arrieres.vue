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

const page = ref(1)
const pageSize = 20
const search = ref('')
const currentYear = new Date().getFullYear()
const yearFilter = ref(currentYear)

// Available years
const { data: availableYears } = await useAsyncData('arrier-years', () =>
  apiFetch<number[]>('/Settings/years').catch(() => [] as number[])
)

const yearItems = computed(() => {
  const items = [{ label: 'Toutes les années', value: 0 }]
  if (availableYears.value) {
    for (const y of availableYears.value) items.push({ label: y.toString(), value: y })
  }
  if (!items.find(i => i.value === currentYear)) items.push({ label: currentYear.toString(), value: currentYear })
  return items.sort((a, b) => b.value - a.value)
})

// Dynamic subtitle
const subtitleYear = computed(() =>
  yearFilter.value > 0 ? `pour ${yearFilter.value}` : 'toutes années confondues'
)

const { data, refresh, status: fetchStatus } = await useAsyncData(
  'arrieres',
  () => {
    const params = new URLSearchParams()
    params.set('page', page.value.toString())
    params.set('pageSize', pageSize.toString())
    params.set('status', 'Pas en ordre')
    if (search.value) params.set('search', search.value)
    if (yearFilter.value > 0) params.set('year', yearFilter.value.toString())
    return apiFetch<PagedResult<CotisationStat>>(`/Cotisations?${params.toString()}`)
  },
  { watch: [page] }
)

const totalPages = computed(() => Math.ceil((data.value?.totalCount || 0) / pageSize))
const totalArriere = computed(() =>
  data.value?.items?.reduce((sum, a) => sum + a.gap, 0) || 0
)

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
        <h1 class="text-3xl font-bold text-gray-900">Arriérés</h1>
        <p class="text-sm text-gray-500 mt-1">Montants en retard à recouvrer {{ subtitleYear }}.</p>
      </div>
    </div>

    <!-- Summary Card -->
    <div class="bg-gradient-to-r from-red-500 to-red-600 rounded-xl p-6 shadow-lg text-white">
      <div class="flex items-center justify-between">
        <div>
          <p class="text-sm text-red-100 font-medium">Total arriéré{{ yearFilter > 0 ? ` (${yearFilter})` : '' }}</p>
          <p class="text-4xl font-bold mt-1">{{ totalArriere.toLocaleString('fr-FR') }} GNF</p>
          <p class="text-xs text-red-200 mt-1">{{ data?.totalCount || 0 }} membres en retard</p>
        </div>
        <div class="p-4 rounded-full bg-white/10">
          <UIcon name="i-lucide-alert-circle" class="w-8 h-8 text-white" />
        </div>
      </div>
    </div>

    <!-- Filters -->
    <div class="bg-white rounded-lg p-6 shadow-sm border border-gray-200">
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <USelect
          v-model="yearFilter"
          :items="yearItems"
          value-key="value"
          label-key="label"
          @update:model-value="onFilterChange"
        />
        <UInput
          v-model="search"
          placeholder="Nom, Numéro..."
          icon="i-lucide-search"
          @keyup.enter="onFilterChange"
        />
      </div>
    </div>

    <!-- Table -->
    <div class="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden">
      <div class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead>
            <tr class="bg-gray-50 border-b border-gray-200">
              <th class="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase tracking-wider">N°</th>
              <th class="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase tracking-wider">Membre</th>
              <th class="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase tracking-wider">Montant dû</th>
              <th class="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase tracking-wider">Déjà payé</th>
              <th class="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase tracking-wider">Reste</th>
              <th class="px-4 py-3 text-right text-xs font-semibold text-gray-600 uppercase tracking-wider">Action</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-100">
            <tr
              v-for="a in data?.items"
              :key="a.id"
              class="hover:bg-gray-50 transition-colors"
            >
              <td class="px-4 py-3 font-mono text-xs text-gray-500">{{ a.numeroMembre }}</td>
              <td class="px-4 py-3">
                <NuxtLink :to="`/members/${a.id}`" class="group">
                  <p class="font-medium text-gray-900 group-hover:text-blue-600 transition-colors">{{ a.prenom }} {{ a.nom }}</p>
                </NuxtLink>
              </td>
              <td class="px-4 py-3 text-gray-600">{{ a.expectedAmount.toLocaleString('fr-FR') }} GNF</td>
              <td class="px-4 py-3 font-medium text-green-600">{{ a.paidAmount.toLocaleString('fr-FR') }} GNF</td>
              <td class="px-4 py-3 font-bold text-red-600">{{ a.gap.toLocaleString('fr-FR') }} GNF</td>
              <td class="px-4 py-3 text-right">
                <UButton
                  size="xs"
                  icon="i-lucide-credit-card"
                  class="bg-blue-600 hover:bg-blue-700"
                  @click="navigateTo(`/payments?memberId=${a.id}`)"
                >
                  Payer
                </UButton>
              </td>
            </tr>
            <tr v-if="fetchStatus === 'pending'">
              <td colspan="6" class="px-4 py-12 text-center">
                <UIcon name="i-lucide-loader-2" class="w-6 h-6 animate-spin text-gray-400 mx-auto" />
              </td>
            </tr>
            <tr v-else-if="!data?.items?.length">
              <td colspan="6" class="px-4 py-12 text-center">
                <div class="max-w-sm mx-auto">
                  <div class="inline-flex items-center justify-center w-16 h-16 rounded-full bg-green-50 mb-4">
                    <UIcon name="i-lucide-party-popper" class="w-8 h-8 text-green-500" />
                  </div>
                  <p class="text-lg font-semibold text-gray-900">Aucun arriéré ! 🎉</p>
                  <p class="text-sm text-gray-500 mt-1">Tous les membres sont à jour</p>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Pagination -->
    <div v-if="totalPages > 1" class="flex justify-center items-center gap-2">
      <UButton
        variant="outline"
        color="neutral"
        label="‹"
        square
        :disabled="page <= 1"
        @click="page = Math.max(1, page - 1)"
      />
      <UPagination v-model:page="page" :total="data?.totalCount || 0" :items-per-page="pageSize" :show-controls="false" />
      <UButton
        variant="outline"
        color="neutral"
        label="›"
        square
        :disabled="page >= totalPages"
        @click="page = Math.min(totalPages, page + 1)"
      />
    </div>
  </div>
</template>
