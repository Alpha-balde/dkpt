<script setup lang="ts">
import { Bar, Doughnut } from 'vue-chartjs'
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  BarElement,
  ArcElement,
  Tooltip,
  Legend
} from 'chart.js'
import type { Member, Payment, PagedResult, ContributionAmount } from '~/types'

ChartJS.register(CategoryScale, LinearScale, BarElement, ArcElement, Tooltip, Legend)

const { apiFetch } = useApi()

const currentYear = new Date().getFullYear()

// Year selector
const { data: availableYears } = await useAsyncData('dash-years', () =>
  apiFetch<number[]>('/Settings/years').catch(() => [] as number[])
)

const yearItems = computed(() => {
  const items: { label: string, value: number }[] = []
  if (availableYears.value) {
    for (const y of availableYears.value) items.push({ label: y.toString(), value: y })
  }
  if (!items.find(i => i.value === currentYear)) items.push({ label: currentYear.toString(), value: currentYear })
  return items.sort((a, b) => b.value - a.value)
})

const selectedYear = ref(currentYear)

// Data fetches
const { data: membersData } = await useAsyncData('dashboard-members', () =>
  apiFetch<PagedResult<Member>>('/Members?page=1&pageSize=1')
)

const { data: activeMembers } = await useAsyncData('dashboard-active', () =>
  apiFetch<PagedResult<Member>>('/Members?page=1&pageSize=1&actif=true')
)

const { data: inactiveMembers } = await useAsyncData('dashboard-inactive', () =>
  apiFetch<PagedResult<Member>>('/Members?page=1&pageSize=1&actif=false')
)

const { data: contributions } = await useAsyncData('dashboard-contributions', () =>
  apiFetch<ContributionAmount[]>('/ContributionAmounts')
)

const { data: allPayments, refresh: refreshPayments } = await useAsyncData(
  'dashboard-all-payments',
  () => apiFetch<PagedResult<Payment>>(`/Payments?page=1&pageSize=500&year=${selectedYear.value}`)
)

const { data: recentPayments } = await useAsyncData('dashboard-recent-payments', () =>
  apiFetch<PagedResult<Payment>>('/Payments?page=1&pageSize=5')
)

// Cotisations stats for arrears count
const { data: arrearsCotis, refresh: refreshArrears } = await useAsyncData(
  'dashboard-arrears',
  () => apiFetch<PagedResult<any>>(`/Cotisations?page=1&pageSize=1&status=Pas en ordre&year=${selectedYear.value}`)
)

const totalMembers = computed(() => membersData.value?.totalCount || 0)
const totalActive = computed(() => activeMembers.value?.totalCount || 0)
const totalInactive = computed(() => inactiveMembers.value?.totalCount || 0)
const arrearsCount = computed(() => arrearsCotis.value?.totalCount || 0)

// Total expected for selected year
const expectedForYear = computed(() => {
  const contrib = contributions.value?.find(c => c.year === selectedYear.value)
  return (contrib?.amount || 60000) * totalMembers.value
})

// Total encaissements filtered by selected year
const totalEncaissements = computed(() =>
  allPayments.value?.items?.reduce((sum, p) => sum + p.montant, 0) || 0
)

// Total frais
const totalFrais = computed(() =>
  allPayments.value?.items?.reduce((sum, p) => sum + p.fraisPaiement, 0) || 0
)

function onYearChange() {
  refreshPayments()
  refreshArrears()
}

// KPI Cards
const stats = computed(() => [
  {
    label: 'Total membres',
    value: totalMembers.value,
    icon: 'i-lucide-users',
    iconColor: 'text-blue-500',
    iconBg: 'bg-blue-50'
  },
  {
    label: 'Total cotisations',
    value: `${expectedForYear.value.toLocaleString('fr-FR')} GNF`,
    subtitle: `Année ${selectedYear.value}`,
    icon: 'i-lucide-credit-card',
    iconColor: 'text-cyan-500',
    iconBg: 'bg-cyan-50'
  },
  {
    label: 'Encaissements',
    value: `${totalEncaissements.value.toLocaleString('fr-FR')} GNF`,
    subtitle: `Reçus en ${selectedYear.value}`,
    icon: 'i-lucide-trending-up',
    iconColor: 'text-green-500',
    iconBg: 'bg-green-50'
  },
  {
    label: 'Total arriérés',
    value: arrearsCount.value,
    subtitle: 'À recouvrer',
    icon: 'i-lucide-alert-circle',
    iconColor: 'text-pink-500',
    iconBg: 'bg-pink-50'
  }
])

// Bar Chart — Encaissements par mois
const months = ['Jan', 'Fév', 'Mar', 'Avr', 'Mai', 'Jun', 'Jul', 'Aoû', 'Sep', 'Oct', 'Nov', 'Déc']
const monthlyData = computed(() => {
  const data = new Array(12).fill(0)
  allPayments.value?.items?.forEach(p => {
    const date = new Date(p.datePaiement)
    if (date.getFullYear() === selectedYear.value) {
      data[date.getMonth()] += p.montant
    }
  })
  return data
})

const barChartData = computed(() => ({
  labels: months,
  datasets: [{
    label: 'Encaissements (GNF)',
    data: monthlyData.value,
    backgroundColor: '#22d3ee',
    borderRadius: { topLeft: 6, topRight: 6 },
    borderSkipped: 'bottom'
  }]
}))

const barChartOptions = {
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: { display: false },
    tooltip: {
      callbacks: {
        label: (ctx: any) => `${ctx.parsed.y.toLocaleString('fr-FR')} GNF`
      }
    }
  },
  scales: {
    y: {
      beginAtZero: true,
      ticks: {
        callback: (val: any) => `${(val / 1000).toFixed(0)}k`
      },
      grid: { color: '#f3f4f6' }
    },
    x: {
      grid: { display: false }
    }
  }
}

// Doughnut Chart — Répartition membres
const doughnutData = computed(() => ({
  labels: ['Actifs', 'Inactifs'],
  datasets: [{
    data: [totalActive.value, totalInactive.value],
    backgroundColor: ['#22c55e', '#ec4899'],
    borderWidth: 0
  }]
}))

const doughnutOptions = {
  responsive: true,
  maintainAspectRatio: false,
  cutout: '65%',
  plugins: {
    legend: {
      position: 'bottom' as const,
      labels: { usePointStyle: true, padding: 16 }
    }
  }
}

// Taux de participation
const participationRate = computed(() => {
  if (!totalMembers.value) return 0
  return Math.round((totalActive.value / totalMembers.value) * 100)
})
</script>

<template>
  <div class="space-y-6">
    <!-- Header with year selector -->
    <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
      <div>
        <h1 class="text-3xl font-bold text-gray-900">Tableau de bord</h1>
        <p class="text-sm text-gray-500 mt-1">Vue d'ensemble des membres, cotisations et paiements.</p>
      </div>
      <div class="w-32">
        <USelect
          v-model="selectedYear"
          :items="yearItems"
          value-key="value"
          label-key="label"
          @update:model-value="onYearChange"
        />
      </div>
    </div>

    <!-- KPI Cards -->
    <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
      <div
        v-for="stat in stats"
        :key="stat.label"
        class="bg-white rounded-xl p-6 shadow-sm border border-gray-100"
      >
        <div class="flex items-center justify-between">
          <div>
            <p class="text-sm text-gray-500 font-medium">{{ stat.label }}</p>
            <p class="text-3xl font-bold text-gray-900 mt-1">{{ stat.value }}</p>
            <p v-if="stat.subtitle" class="text-sm text-gray-400 mt-1">{{ stat.subtitle }}</p>
          </div>
          <div :class="[stat.iconBg, 'p-3 rounded-full']">
            <UIcon :name="stat.icon" :class="[stat.iconColor, 'w-6 h-6']" />
          </div>
        </div>
      </div>
    </div>

    <!-- Charts Row -->
    <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
      <!-- Bar Chart — Encaissements mensuels -->
      <div class="bg-white rounded-xl p-6 shadow-sm border border-gray-100">
        <div class="flex items-center justify-between mb-4">
          <h3 class="text-lg font-semibold text-gray-900">Encaissements mensuels</h3>
          <span class="text-xs bg-blue-100 text-blue-600 px-2 py-1 rounded-full font-medium">
            {{ selectedYear }}
          </span>
        </div>
        <div class="h-64">
          <Bar :data="barChartData" :options="barChartOptions" />
        </div>
      </div>

      <!-- Doughnut Chart — Répartition membres -->
      <div class="bg-white rounded-xl p-6 shadow-sm border border-gray-100">
        <div class="flex items-center justify-between mb-4">
          <h3 class="text-lg font-semibold text-gray-900">Répartition des membres</h3>
        </div>
        <div class="h-64">
          <Doughnut :data="doughnutData" :options="doughnutOptions" />
        </div>
      </div>
    </div>

    <!-- Summary Gradient Card -->
    <div class="bg-gradient-to-r from-blue-500 to-cyan-500 rounded-xl p-6 shadow-lg text-white">
      <div class="grid grid-cols-1 sm:grid-cols-3 gap-6 text-center">
        <div>
          <p class="text-sm text-blue-100 font-medium">Taux de participation</p>
          <p class="text-3xl font-bold mt-1">{{ participationRate }}%</p>
          <p class="text-xs text-blue-200 mt-1">{{ totalActive }} / {{ totalMembers }} membres</p>
        </div>
        <div>
          <p class="text-sm text-blue-100 font-medium">Frais de paiement</p>
          <p class="text-3xl font-bold mt-1">{{ totalFrais.toLocaleString('fr-FR') }}</p>
          <p class="text-xs text-blue-200 mt-1">GNF collectés en frais</p>
        </div>
        <div>
          <p class="text-sm text-blue-100 font-medium">Nombre de paiements</p>
          <p class="text-3xl font-bold mt-1">{{ allPayments?.totalCount || 0 }}</p>
          <p class="text-xs text-blue-200 mt-1">paiements en {{ selectedYear }}</p>
        </div>
      </div>
    </div>

    <!-- Recent Payments -->
    <div class="bg-white rounded-xl shadow-sm border border-gray-100">
      <div class="flex items-center justify-between px-6 py-4 border-b border-gray-100">
        <h3 class="text-lg font-semibold text-gray-900">Derniers paiements</h3>
        <NuxtLink to="/payments" class="text-sm text-blue-600 hover:text-blue-700 font-medium flex items-center gap-1">
          Voir tout
          <UIcon name="i-lucide-arrow-right" class="w-4 h-4" />
        </NuxtLink>
      </div>
      <div class="divide-y divide-gray-100">
        <div
          v-for="p in recentPayments?.items"
          :key="p.id"
          class="flex items-center justify-between px-6 py-4 hover:bg-gray-50 transition-colors"
        >
          <div>
            <p class="text-sm font-medium text-gray-900">
              {{ p.member?.prenom }} {{ p.member?.nom }}
            </p>
            <p class="text-xs text-gray-500">{{ p.datePaiement }} — {{ p.moyenPaiement }}</p>
          </div>
          <span class="text-sm font-bold text-green-600">
            {{ p.montant.toLocaleString('fr-FR') }} GNF
          </span>
        </div>
        <div v-if="!recentPayments?.items?.length" class="px-6 py-8 text-center text-sm text-gray-500">
          Aucun paiement récent
        </div>
      </div>
    </div>
  </div>
</template>
