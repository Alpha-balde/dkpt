<script setup lang="ts">
import type { Member, Payment, PagedResult } from '~/types'

const route = useRoute()
const { apiFetch } = useApi()
const memberId = route.params.id as string

const { data: member } = await useAsyncData(`member-${memberId}`, () =>
  apiFetch<Member>(`/Members/${memberId}`)
)

const { data: payments } = await useAsyncData(`member-payments-${memberId}`, () =>
  apiFetch<PagedResult<Payment>>(`/Payments?memberId=${memberId}&page=1&pageSize=50`)
)

const totalPaid = computed(() =>
  payments.value?.items?.reduce((sum, p) => sum + p.montant, 0) || 0
)

const infoItems = computed(() => [
  { label: 'N° Membre', value: member.value?.numeroMembre, icon: 'i-lucide-hash' },
  { label: 'Téléphone', value: member.value?.telephone || '—', icon: 'i-lucide-phone' },
  { label: 'WhatsApp', value: member.value?.whatsApp || '—', icon: 'i-lucide-message-circle' },
  { label: 'Résidence', value: member.value?.residence || '—', icon: 'i-lucide-map-pin' },
  { label: 'Village', value: member.value?.village || '—', icon: 'i-lucide-home' },
  { label: 'Sous-préfecture', value: member.value?.sousPrefecture || '—', icon: 'i-lucide-building' },
  { label: 'Année début', value: member.value?.anneeDebut, icon: 'i-lucide-calendar' },
  { label: 'Inscrit le', value: member.value?.createdAt ? new Date(member.value.createdAt).toLocaleDateString('fr-FR') : '—', icon: 'i-lucide-clock' }
])
</script>

<template>
  <div class="space-y-6">
    <!-- Back + Header -->
    <div class="flex items-center gap-4">
      <UButton icon="i-lucide-arrow-left" variant="ghost" to="/members" />
      <div class="flex-1">
        <h1 class="text-2xl font-bold text-(--ui-text-highlighted)">
          {{ member?.prenom }} {{ member?.nom }}
        </h1>
        <div class="flex items-center gap-2 mt-1">
          <UBadge
            :label="member?.actif ? 'Actif' : 'Inactif'"
            :color="member?.actif ? 'success' : 'error'"
            variant="subtle"
          />
          <span class="text-sm text-(--ui-text-muted)">{{ member?.numeroMembre }}</span>
        </div>
      </div>
    </div>

    <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
      <!-- Info card -->
      <UCard class="lg:col-span-1">
        <template #header>
          <h3 class="font-semibold text-(--ui-text-highlighted)">Informations</h3>
        </template>
        <div class="space-y-4">
          <div v-for="item in infoItems" :key="item.label" class="flex items-start gap-3">
            <UIcon :name="item.icon" class="w-4 h-4 mt-0.5 text-(--ui-text-muted) shrink-0" />
            <div>
              <p class="text-xs text-(--ui-text-muted)">{{ item.label }}</p>
              <p class="text-sm font-medium text-(--ui-text-highlighted)">{{ item.value }}</p>
            </div>
          </div>
        </div>
      </UCard>

      <!-- Payments -->
      <UCard class="lg:col-span-2">
        <template #header>
          <div class="flex items-center justify-between">
            <h3 class="font-semibold text-(--ui-text-highlighted)">
              Historique paiements ({{ payments?.totalCount || 0 }})
            </h3>
            <UBadge color="success" variant="subtle">
              Total : {{ totalPaid.toLocaleString('fr-FR') }} GNF
            </UBadge>
          </div>
        </template>
        <div class="overflow-x-auto">
          <table class="w-full text-sm">
            <thead>
              <tr class="border-b border-(--ui-border)">
                <th class="px-4 py-2 text-left text-xs font-medium text-(--ui-text-muted)">Année</th>
                <th class="px-4 py-2 text-left text-xs font-medium text-(--ui-text-muted)">Date</th>
                <th class="px-4 py-2 text-left text-xs font-medium text-(--ui-text-muted)">Montant</th>
                <th class="px-4 py-2 text-left text-xs font-medium text-(--ui-text-muted)">Moyen</th>
                <th class="px-4 py-2 text-left text-xs font-medium text-(--ui-text-muted)">Référence</th>
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="p in payments?.items"
                :key="p.id"
                class="border-b border-(--ui-border)"
              >
                <td class="px-4 py-2">
                  <UBadge :label="String(p.annee)" variant="subtle" size="sm" />
                </td>
                <td class="px-4 py-2 text-(--ui-text-muted)">{{ p.datePaiement }}</td>
                <td class="px-4 py-2 font-semibold text-emerald-500">{{ p.montant.toLocaleString('fr-FR') }} GNF</td>
                <td class="px-4 py-2 text-(--ui-text-muted)">{{ p.moyenPaiement }}</td>
                <td class="px-4 py-2 text-(--ui-text-muted)">{{ p.reference || '—' }}</td>
              </tr>
              <tr v-if="!payments?.items?.length">
                <td colspan="5" class="px-4 py-6 text-center text-(--ui-text-muted)">
                  Aucun paiement enregistré
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </UCard>
    </div>
  </div>
</template>
