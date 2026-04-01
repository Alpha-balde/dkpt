<script setup lang="ts">
import type { Setting } from '~/types'

const { apiFetch } = useApi()

const { data: settings } = await useAsyncData('settings', () =>
  apiFetch<Setting>('/Settings')
)
</script>

<template>
  <div class="space-y-6">
    <div>
      <h1 class="text-2xl font-bold text-(--ui-text-highlighted)">Paramètres</h1>
      <p class="text-sm text-(--ui-text-muted)">Configuration de l'application</p>
    </div>

    <UCard>
      <template #header>
        <h3 class="font-semibold text-(--ui-text-highlighted)">Cotisation par défaut</h3>
      </template>
      <div class="flex items-center gap-4">
        <UIcon name="i-lucide-banknote" class="w-8 h-8 text-amber-500" />
        <div>
          <p class="text-sm text-(--ui-text-muted)">Montant annuel par défaut</p>
          <p class="text-2xl font-bold text-(--ui-text-highlighted)">
            {{ settings?.montantCotisationAnnuelleParDefaut?.toLocaleString('fr-FR') || '—' }} GNF
          </p>
        </div>
      </div>
    </UCard>
  </div>
</template>
