<script setup lang="ts">
import type { Payment, PagedResult } from '~/types'

const { apiFetch } = useApi()
const { canManagePayments } = useAuth()

const page = ref(1)
const pageSize = 20

const { data, refresh, status } = await useAsyncData(
  'payments',
  () => apiFetch<PagedResult<Payment>>(`/Payments?page=${page.value}&pageSize=${pageSize}`),
  { watch: [page] }
)

const totalPages = computed(() => Math.ceil((data.value?.totalCount || 0) / pageSize))
</script>

<template>
  <div class="space-y-6">
    <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
      <div>
        <h1 class="text-2xl font-bold text-(--ui-text-highlighted)">Paiements</h1>
        <p class="text-sm text-(--ui-text-muted)">{{ data?.totalCount || 0 }} paiements enregistrés</p>
      </div>
    </div>

    <UCard :ui="{ body: 'p-0' }">
      <div class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead>
            <tr class="border-b border-(--ui-border)">
              <th class="px-4 py-3 text-left text-xs font-medium text-(--ui-text-muted) uppercase">Membre</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-(--ui-text-muted) uppercase">Année</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-(--ui-text-muted) uppercase">Date</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-(--ui-text-muted) uppercase">Montant</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-(--ui-text-muted) uppercase">Frais</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-(--ui-text-muted) uppercase">Moyen</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-(--ui-text-muted) uppercase">Référence</th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="p in data?.items"
              :key="p.id"
              class="border-b border-(--ui-border) hover:bg-(--ui-bg-elevated) transition-colors"
            >
              <td class="px-4 py-3">
                <NuxtLink
                  v-if="p.member"
                  :to="`/members/${p.memberId}`"
                  class="font-medium text-(--ui-primary) hover:underline"
                >
                  {{ p.member.prenom }} {{ p.member.nom }}
                </NuxtLink>
                <span v-else class="text-(--ui-text-muted)">—</span>
              </td>
              <td class="px-4 py-3">
                <UBadge :label="String(p.annee)" variant="subtle" size="sm" />
              </td>
              <td class="px-4 py-3 text-(--ui-text-muted)">{{ p.datePaiement }}</td>
              <td class="px-4 py-3 font-semibold text-emerald-500">{{ p.montant.toLocaleString('fr-FR') }} GNF</td>
              <td class="px-4 py-3 text-(--ui-text-muted)">{{ p.fraisPaiement.toLocaleString('fr-FR') }}</td>
              <td class="px-4 py-3">
                <UBadge :label="p.moyenPaiement" variant="subtle" color="neutral" size="sm" />
              </td>
              <td class="px-4 py-3 text-(--ui-text-muted)">{{ p.reference || '—' }}</td>
            </tr>
            <tr v-if="status === 'pending'">
              <td colspan="7" class="px-4 py-8 text-center">
                <UIcon name="i-lucide-loader-2" class="w-6 h-6 animate-spin text-(--ui-text-muted)" />
              </td>
            </tr>
            <tr v-else-if="!data?.items?.length">
              <td colspan="7" class="px-4 py-8 text-center text-(--ui-text-muted)">Aucun paiement trouvé</td>
            </tr>
          </tbody>
        </table>
      </div>
    </UCard>

    <div v-if="totalPages > 1" class="flex justify-center">
      <UPagination v-model="page" :total="data?.totalCount || 0" :items-per-page="pageSize" />
    </div>
  </div>
</template>
