<script setup lang="ts">
import type { ContributionAmount } from '~/types'

const { apiFetch } = useApi()

const { data: contributions } = await useAsyncData('cotisations-amounts', () =>
  apiFetch<ContributionAmount[]>('/ContributionAmounts')
)
</script>

<template>
  <div class="space-y-6">
    <div>
      <h1 class="text-2xl font-bold text-(--ui-text-highlighted)">Cotisations</h1>
      <p class="text-sm text-(--ui-text-muted)">Suivi des cotisations par année</p>
    </div>

    <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
      <UCard v-for="c in contributions?.sort((a, b) => b.year - a.year)" :key="c.year">
        <div class="flex items-center justify-between">
          <div>
            <p class="text-sm text-(--ui-text-muted)">Année {{ c.year }}</p>
            <p class="text-2xl font-bold text-(--ui-text-highlighted)">{{ c.amount.toLocaleString('fr-FR') }} GNF</p>
          </div>
          <div class="p-3 rounded-xl bg-blue-500/10">
            <UIcon name="i-lucide-banknote" class="w-6 h-6 text-blue-500" />
          </div>
        </div>
      </UCard>
    </div>
  </div>
</template>
