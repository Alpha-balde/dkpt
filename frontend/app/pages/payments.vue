<script setup lang="ts">
import type { Payment, PagedResult } from '~/types'

const { apiFetch } = useApi()
const { canManagePayments } = useAuth()

const page = ref(1)
const pageSize = 20
const search = ref('')

const { data, refresh, status } = await useAsyncData(
  'payments',
  () => apiFetch<PagedResult<Payment>>(`/Payments?page=${page.value}&pageSize=${pageSize}`),
  { watch: [page] }
)

const totalPages = computed(() => Math.ceil((data.value?.totalCount || 0) / pageSize))

const paymentMethodBadge = (method: string) => {
  const map: Record<string, string> = {
    Especes: 'bg-green-50 text-green-700',
    MobileMoney: 'bg-blue-50 text-blue-700',
    Virement: 'bg-purple-50 text-purple-700',
    Autre: 'bg-gray-50 text-gray-700'
  }
  return map[method] || 'bg-gray-50 text-gray-700'
}
</script>

<template>
  <div class="space-y-6">
    <!-- Header -->
    <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
      <div>
        <h1 class="text-3xl font-bold text-gray-900">Paiements</h1>
        <p class="text-sm text-gray-500 mt-1">{{ data?.totalCount || 0 }} paiements enregistrés</p>
      </div>
    </div>

    <!-- Filters -->
    <div class="bg-white rounded-xl p-6 shadow-sm border border-gray-100">
      <div class="grid grid-cols-1 md:grid-cols-4 gap-4">
        <UInput
          v-model="search"
          placeholder="Rechercher un membre..."
          icon="i-lucide-search"
          class="md:col-span-2"
          @keyup.enter="refresh()"
        />
        <UButton variant="soft" icon="i-lucide-search" @click="refresh()">
          Rechercher
        </UButton>
        <UButton variant="soft" color="neutral" icon="i-lucide-rotate-ccw" @click="search = ''; refresh()">
          Réinitialiser
        </UButton>
      </div>
    </div>

    <!-- Table -->
    <div class="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden">
      <div class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead>
            <tr class="bg-gray-50 border-b border-gray-200">
              <th class="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase tracking-wider">Date</th>
              <th class="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase tracking-wider">Membre</th>
              <th class="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase tracking-wider">Année</th>
              <th class="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase tracking-wider">Montant</th>
              <th class="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase tracking-wider hidden md:table-cell">Moyen / Réf</th>
              <th class="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase tracking-wider hidden lg:table-cell">Note</th>
              <th class="px-4 py-3 text-right text-xs font-semibold text-gray-600 uppercase tracking-wider">Actions</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-100">
            <tr
              v-for="p in data?.items"
              :key="p.id"
              class="hover:bg-gray-50 transition-colors"
            >
              <td class="px-4 py-3 text-gray-600">{{ p.datePaiement }}</td>
              <td class="px-4 py-3">
                <NuxtLink
                  v-if="p.member"
                  :to="`/members/${p.memberId}`"
                  class="group"
                >
                  <p class="font-medium text-gray-900 group-hover:text-blue-600 transition-colors">
                    {{ p.member.prenom }} {{ p.member.nom }}
                  </p>
                  <p class="text-xs text-gray-500">{{ p.member.numeroMembre }}</p>
                </NuxtLink>
                <span v-else class="text-gray-400">—</span>
              </td>
              <td class="px-4 py-3">
                <span class="inline-flex items-center rounded-full bg-blue-50 text-blue-700 px-2.5 py-0.5 text-xs font-medium">
                  {{ p.annee }}
                </span>
              </td>
              <td class="px-4 py-3 font-bold text-gray-900">{{ p.montant.toLocaleString('fr-FR') }} GNF</td>
              <td class="px-4 py-3 hidden md:table-cell">
                <span :class="[paymentMethodBadge(p.moyenPaiement), 'inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium']">
                  {{ p.moyenPaiement }}
                </span>
                <span v-if="p.reference" class="text-xs text-gray-400 ml-1">{{ p.reference }}</span>
              </td>
              <td class="px-4 py-3 text-gray-500 text-xs hidden lg:table-cell max-w-[200px] truncate">
                {{ p.note || '—' }}
              </td>
              <td class="px-4 py-3">
                <div class="flex items-center justify-end gap-1">
                  <button
                    class="h-8 w-8 inline-flex items-center justify-center rounded-lg text-gray-500 hover:text-blue-600 hover:bg-blue-50 transition-colors"
                    title="Voir membre"
                    @click="navigateTo(`/members/${p.memberId}`)"
                  >
                    <UIcon name="i-lucide-eye" class="w-4 h-4" />
                  </button>
                </div>
              </td>
            </tr>
            <tr v-if="status === 'pending'">
              <td colspan="7" class="px-4 py-12 text-center">
                <UIcon name="i-lucide-loader-2" class="w-6 h-6 animate-spin text-gray-400 mx-auto" />
              </td>
            </tr>
            <tr v-else-if="!data?.items?.length">
              <td colspan="7" class="px-4 py-12 text-center text-gray-500">Aucun paiement trouvé</td>
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
