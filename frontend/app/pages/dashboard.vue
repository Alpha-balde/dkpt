<script setup lang="ts">
import type { Member, PagedResult, ContributionAmount } from '~/types'

const { apiFetch } = useApi()

const { data: membersData } = await useAsyncData('dashboard-members', () =>
  apiFetch<PagedResult<Member>>('/Members?page=1&pageSize=1')
)

const { data: activeMembers } = await useAsyncData('dashboard-active', () =>
  apiFetch<PagedResult<Member>>('/Members?page=1&pageSize=1&actif=true')
)

const { data: contributions } = await useAsyncData('dashboard-contributions', () =>
  apiFetch<ContributionAmount[]>('/ContributionAmounts')
)

const { data: recentPayments } = await useAsyncData('dashboard-recent-payments', () =>
  apiFetch<PagedResult<any>>('/Payments?page=1&pageSize=5')
)

const totalMembers = computed(() => membersData.value?.totalCount || 0)
const totalActive = computed(() => activeMembers.value?.totalCount || 0)
const totalInactive = computed(() => totalMembers.value - totalActive.value)
const currentYear = new Date().getFullYear()
const currentContribution = computed(() =>
  contributions.value?.find(c => c.year === currentYear)?.amount || 0
)

const stats = computed(() => [
  {
    label: 'Total membres',
    value: totalMembers.value,
    icon: 'i-lucide-users',
    color: 'text-blue-500',
    bg: 'bg-blue-500/10'
  },
  {
    label: 'Membres actifs',
    value: totalActive.value,
    icon: 'i-lucide-user-check',
    color: 'text-emerald-500',
    bg: 'bg-emerald-500/10'
  },
  {
    label: 'Membres inactifs',
    value: totalInactive.value,
    icon: 'i-lucide-user-x',
    color: 'text-red-500',
    bg: 'bg-red-500/10'
  },
  {
    label: `Cotisation ${currentYear}`,
    value: `${currentContribution.value.toLocaleString('fr-FR')} GNF`,
    icon: 'i-lucide-banknote',
    color: 'text-amber-500',
    bg: 'bg-amber-500/10'
  }
])
</script>

<template>
  <div class="space-y-6">
    <!-- Header -->
    <div>
      <h1 class="text-2xl font-bold text-(--ui-text-highlighted)">Tableau de bord</h1>
      <p class="text-sm text-(--ui-text-muted)">Vue d'ensemble de l'association DKPT</p>
    </div>

    <!-- KPI Cards -->
    <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
      <UCard v-for="stat in stats" :key="stat.label">
        <div class="flex items-center gap-4">
          <div :class="[stat.bg, 'p-3 rounded-xl']">
            <UIcon :name="stat.icon" :class="[stat.color, 'w-6 h-6']" />
          </div>
          <div>
            <p class="text-sm text-(--ui-text-muted)">{{ stat.label }}</p>
            <p class="text-xl font-bold text-(--ui-text-highlighted)">{{ stat.value }}</p>
          </div>
        </div>
      </UCard>
    </div>

    <!-- Contribution amounts + recent payments -->
    <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
      <!-- Cotisations par année -->
      <UCard>
        <template #header>
          <div class="flex items-center gap-2">
            <UIcon name="i-lucide-calendar" class="w-5 h-5 text-(--ui-text-muted)" />
            <h3 class="font-semibold text-(--ui-text-highlighted)">Montants cotisations</h3>
          </div>
        </template>

        <div class="divide-y divide-(--ui-border)">
          <div
            v-for="c in contributions?.sort((a, b) => b.year - a.year)"
            :key="c.year"
            class="flex items-center justify-between py-3"
          >
            <div class="flex items-center gap-3">
              <UBadge :label="String(c.year)" variant="subtle" />
            </div>
            <span class="font-semibold text-(--ui-text-highlighted)">
              {{ c.amount.toLocaleString('fr-FR') }} GNF
            </span>
          </div>
        </div>
      </UCard>

      <!-- Derniers paiements -->
      <UCard>
        <template #header>
          <div class="flex items-center justify-between">
            <div class="flex items-center gap-2">
              <UIcon name="i-lucide-credit-card" class="w-5 h-5 text-(--ui-text-muted)" />
              <h3 class="font-semibold text-(--ui-text-highlighted)">Derniers paiements</h3>
            </div>
            <UButton to="/payments" variant="ghost" size="xs" trailing-icon="i-lucide-arrow-right">
              Voir tout
            </UButton>
          </div>
        </template>

        <div class="divide-y divide-(--ui-border)">
          <div
            v-for="p in recentPayments?.items"
            :key="p.id"
            class="flex items-center justify-between py-3"
          >
            <div>
              <p class="text-sm font-medium text-(--ui-text-highlighted)">
                {{ p.member?.prenom }} {{ p.member?.nom }}
              </p>
              <p class="text-xs text-(--ui-text-muted)">{{ p.datePaiement }} — {{ p.moyenPaiement }}</p>
            </div>
            <span class="font-semibold text-emerald-500">
              {{ p.montant.toLocaleString('fr-FR') }} GNF
            </span>
          </div>
          <p v-if="!recentPayments?.items?.length" class="py-4 text-center text-sm text-(--ui-text-muted)">
            Aucun paiement récent
          </p>
        </div>
      </UCard>
    </div>
  </div>
</template>
