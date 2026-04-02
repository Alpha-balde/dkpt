<script setup lang="ts">
import type { Payment, MemberSimple, PagedResult } from '~/types'

const { apiFetch } = useApi()
const { canManagePayments } = useAuth()
const toast = useToast()
const route = useRoute()

const page = ref(1)
const pageSize = 20
const search = ref('')
const yearFilter = ref(new Date().getFullYear())
const methodFilter = ref('all')

// Available years from settings
const { data: availableYears } = await useAsyncData('payment-years', () =>
  apiFetch<number[]>('/Settings/years').catch(() => [] as number[])
)

const yearItems = computed(() => {
  const items = [{ label: 'Toutes les années', value: 0 }]
  if (availableYears.value) {
    for (const y of availableYears.value) {
      items.push({ label: y.toString(), value: y })
    }
  }
  const cy = new Date().getFullYear()
  if (!items.find(i => i.value === cy)) items.push({ label: cy.toString(), value: cy })
  return items.sort((a, b) => b.value - a.value)
})

const methodItems = [
  { label: 'Tous les moyens', value: 'all' },
  { label: 'Espèces', value: 'Especes' },
  { label: 'Orange Money', value: 'OrangeMoney' },
  { label: 'Virement', value: 'Virement' },
  { label: 'Autre', value: 'Autre' }
]

// Fetch payments with filters
const { data, refresh, status } = await useAsyncData(
  'payments',
  () => {
    const params = new URLSearchParams()
    params.set('page', page.value.toString())
    params.set('pageSize', pageSize.toString())
    if (search.value) params.set('search', search.value)
    if (yearFilter.value > 0) params.set('year', yearFilter.value.toString())
    if (methodFilter.value !== 'all') params.set('method', methodFilter.value)
    // Check if redirected from cotisations with memberId
    const memberId = route.query.memberId as string
    if (memberId) params.set('memberId', memberId)
    return apiFetch<PagedResult<Payment>>(`/Payments?${params.toString()}`)
  },
  { watch: [page] }
)

const totalPages = computed(() => Math.ceil((data.value?.totalCount || 0) / pageSize))

function onFilterChange() {
  page.value = 1
  refresh()
}

// Payment method badge colors
const methodBadge = (method: string) => {
  const map: Record<string, string> = {
    Especes: 'bg-green-50 text-green-700',
    OrangeMoney: 'bg-orange-50 text-orange-700',
    Virement: 'bg-purple-50 text-purple-700',
    Autre: 'bg-gray-50 text-gray-700'
  }
  return map[method] || 'bg-gray-50 text-gray-700'
}

const methodLabel = (method: string) => {
  const map: Record<string, string> = {
    Especes: 'Espèces',
    OrangeMoney: 'Orange Money',
    Virement: 'Virement',
    Autre: 'Autre'
  }
  return map[method] || method
}

// --- Payment Modal ---
const showPaymentModal = ref(false)
const isEditing = ref(false)
const editingPaymentId = ref<string | null>(null)
const saving = ref(false)

const paymentForm = reactive({
  memberId: '',
  annee: new Date().getFullYear(),
  datePaiement: new Date().toISOString().split('T')[0],
  montant: 0,
  fraisPaiement: 0,
  moyenPaiement: 'Especes',
  reference: '',
  note: ''
})

// Member search for combobox
const memberSearch = ref('')
const { data: membersList } = await useAsyncData('members-simple', () =>
  apiFetch<MemberSimple[]>('/Members/simple')
)

const filteredMembers = computed(() => {
  if (!membersList.value) return []
  if (!memberSearch.value) return membersList.value.slice(0, 20)
  const q = memberSearch.value.toLowerCase()
  return membersList.value
    .filter(m =>
      m.prenom.toLowerCase().includes(q) ||
      m.nom.toLowerCase().includes(q) ||
      m.numeroMembre.toLowerCase().includes(q)
    )
    .slice(0, 20)
})

const selectedMember = computed(() =>
  membersList.value?.find(m => m.id === paymentForm.memberId)
)

function openNewPayment(prefillMemberId?: string) {
  isEditing.value = false
  editingPaymentId.value = null
  paymentForm.memberId = prefillMemberId || ''
  paymentForm.annee = new Date().getFullYear()
  paymentForm.datePaiement = new Date().toISOString().split('T')[0]
  paymentForm.montant = 60000
  paymentForm.fraisPaiement = 0
  paymentForm.moyenPaiement = 'Especes'
  paymentForm.reference = ''
  paymentForm.note = ''
  memberSearch.value = ''
  showPaymentModal.value = true
}

function openEditPayment(p: Payment) {
  isEditing.value = true
  editingPaymentId.value = p.id
  paymentForm.memberId = p.memberId
  paymentForm.annee = p.annee
  paymentForm.datePaiement = p.datePaiement
  paymentForm.montant = p.montant
  paymentForm.fraisPaiement = p.fraisPaiement
  paymentForm.moyenPaiement = p.moyenPaiement
  paymentForm.reference = p.reference || ''
  paymentForm.note = p.note || ''
  memberSearch.value = ''
  showPaymentModal.value = true
}

async function savePayment() {
  if (!paymentForm.memberId) {
    toast.add({ title: 'Sélectionnez un membre', color: 'warning' })
    return
  }
  saving.value = true
  try {
    if (isEditing.value && editingPaymentId.value) {
      await apiFetch(`/Payments/${editingPaymentId.value}`, {
        method: 'PUT',
        body: {
          memberId: paymentForm.memberId,
          annee: paymentForm.annee,
          datePaiement: paymentForm.datePaiement,
          montant: paymentForm.montant,
          fraisPaiement: paymentForm.fraisPaiement,
          moyenPaiement: paymentForm.moyenPaiement,
          reference: paymentForm.reference || null,
          note: paymentForm.note || null
        }
      })
      toast.add({ title: 'Paiement modifié', color: 'success', icon: 'i-lucide-check-circle' })
    } else {
      await apiFetch('/Payments', {
        method: 'POST',
        body: {
          memberId: paymentForm.memberId,
          annee: paymentForm.annee,
          datePaiement: paymentForm.datePaiement,
          montant: paymentForm.montant,
          fraisPaiement: paymentForm.fraisPaiement,
          moyenPaiement: paymentForm.moyenPaiement,
          reference: paymentForm.reference || null,
          note: paymentForm.note || null
        }
      })
      toast.add({ title: 'Paiement enregistré', color: 'success', icon: 'i-lucide-check-circle' })
    }
    showPaymentModal.value = false
    refresh()
  } catch (err: any) {
    toast.add({ title: 'Erreur', description: err?.data?.message || 'Impossible d\'enregistrer', color: 'error' })
  } finally {
    saving.value = false
  }
}

// Delete
const showDeleteConfirm = ref(false)
const deletingPaymentId = ref<string | null>(null)

function confirmDelete(id: string) {
  deletingPaymentId.value = id
  showDeleteConfirm.value = true
}

async function deletePayment() {
  if (!deletingPaymentId.value) return
  try {
    await apiFetch(`/Payments/${deletingPaymentId.value}`, { method: 'DELETE' })
    toast.add({ title: 'Paiement supprimé', color: 'success', icon: 'i-lucide-trash-2' })
    showDeleteConfirm.value = false
    refresh()
  } catch {
    toast.add({ title: 'Erreur de suppression', color: 'error' })
  }
}

// Pre-fill from query param
onMounted(() => {
  const mid = route.query.memberId as string
  if (mid) {
    openNewPayment(mid)
  }
})
</script>

<template>
  <div class="space-y-6">
    <!-- Header -->
    <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
      <div>
        <h1 class="text-3xl font-bold text-gray-900">Paiements</h1>
        <p class="text-sm text-gray-500 mt-1">Historique des transactions enregistrées.</p>
      </div>
      <UButton
        v-if="canManagePayments"
        icon="i-lucide-plus"
        class="bg-blue-600 hover:bg-blue-700"
        @click="openNewPayment()"
      >
        Nouveau paiement
      </UButton>
    </div>

    <!-- Filters -->
    <div class="bg-white rounded-lg p-6 shadow-sm border border-gray-200">
      <div class="grid grid-cols-1 sm:grid-cols-3 lg:grid-cols-4 gap-4">
        <USelect
          v-model="yearFilter"
          :items="yearItems"
          value-key="value"
          label-key="label"
          @update:model-value="onFilterChange"
        />
        <USelect
          v-model="methodFilter"
          :items="methodItems"
          @update:model-value="onFilterChange"
        />
        <UInput
          v-model="search"
          class="lg:col-span-2"
          placeholder="Nom, Numéro membre..."
          icon="i-lucide-search"
          @keyup.enter="onFilterChange"
        />
      </div>
    </div>

    <!-- Table desktop -->
    <div class="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden hidden lg:block">
      <div class="px-6 py-4 border-b border-gray-100">
        <h3 class="text-lg font-semibold text-gray-900">Historique</h3>
        <p class="text-xs text-gray-500">{{ data?.totalCount || 0 }} transactions trouvées</p>
      </div>
      <div class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead>
            <tr class="bg-gray-50 border-b border-gray-200">
              <th class="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase tracking-wider">Date</th>
              <th class="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase tracking-wider">Membre</th>
              <th class="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase tracking-wider">Année</th>
              <th class="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase tracking-wider">Montant</th>
              <th class="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase tracking-wider">Moyen / Réf</th>
              <th class="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase tracking-wider">Note</th>
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
                <NuxtLink v-if="p.member" :to="`/members/${p.memberId}`" class="group">
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
              <td class="px-4 py-3">
                <span :class="[methodBadge(p.moyenPaiement), 'inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium']">
                  {{ methodLabel(p.moyenPaiement) }}
                </span>
                <span v-if="p.reference" class="text-xs text-gray-400 ml-1">{{ p.reference }}</span>
              </td>
              <td class="px-4 py-3 text-gray-500 text-xs max-w-[200px] truncate">
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
                  <button
                    v-if="canManagePayments"
                    class="h-8 w-8 inline-flex items-center justify-center rounded-lg text-gray-500 hover:text-blue-600 hover:bg-blue-50 transition-colors"
                    title="Modifier"
                    @click="openEditPayment(p)"
                  >
                    <UIcon name="i-lucide-pencil" class="w-4 h-4" />
                  </button>
                  <button
                    v-if="canManagePayments"
                    class="h-8 w-8 inline-flex items-center justify-center rounded-lg text-gray-500 hover:text-red-600 hover:bg-red-50 transition-colors"
                    title="Supprimer"
                    @click="confirmDelete(p.id)"
                  >
                    <UIcon name="i-lucide-trash-2" class="w-4 h-4" />
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

    <!-- Mobile cards -->
    <div class="lg:hidden space-y-3">
      <div
        v-for="p in data?.items"
        :key="p.id"
        class="bg-white rounded-xl shadow-sm border border-gray-100 p-4"
      >
        <div class="flex items-center justify-between mb-2">
          <span class="text-xs text-gray-500">{{ p.datePaiement }}</span>
          <span :class="[methodBadge(p.moyenPaiement), 'inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium']">
            {{ methodLabel(p.moyenPaiement) }}
          </span>
        </div>
        <NuxtLink v-if="p.member" :to="`/members/${p.memberId}`">
          <p class="font-medium text-gray-900">{{ p.member.prenom }} {{ p.member.nom }}</p>
          <p class="text-xs text-gray-500">{{ p.member.numeroMembre }}</p>
        </NuxtLink>
        <div class="flex items-center justify-between mt-2">
          <p class="text-lg font-bold text-gray-900">{{ p.montant.toLocaleString('fr-FR') }} GNF</p>
          <span class="inline-flex items-center rounded-full bg-blue-50 text-blue-700 px-2 py-0.5 text-xs font-medium">{{ p.annee }}</span>
        </div>
        <div v-if="canManagePayments" class="flex justify-end gap-1 mt-3 pt-3 border-t border-gray-100">
          <button class="h-8 w-8 inline-flex items-center justify-center rounded-lg text-gray-500 hover:text-blue-600 hover:bg-blue-50" @click="openEditPayment(p)">
            <UIcon name="i-lucide-pencil" class="w-4 h-4" />
          </button>
          <button class="h-8 w-8 inline-flex items-center justify-center rounded-lg text-gray-500 hover:text-red-600 hover:bg-red-50" @click="confirmDelete(p.id)">
            <UIcon name="i-lucide-trash-2" class="w-4 h-4" />
          </button>
        </div>
      </div>
      <div v-if="!data?.items?.length" class="py-12 text-center text-gray-500 text-sm">Aucun paiement trouvé</div>
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

    <!-- Payment Modal (Create / Edit) -->
    <UModal v-model:open="showPaymentModal">
      <template #header>
        <h3 class="text-lg font-semibold text-gray-900">
          {{ isEditing ? 'Modifier le paiement' : 'Nouveau paiement' }}
        </h3>
      </template>
      <template #body>
        <form class="space-y-4" @submit.prevent="savePayment">
          <!-- Member Search -->
          <div class="space-y-1">
            <label class="text-sm font-medium text-gray-700">Membre *</label>
            <div v-if="selectedMember" class="flex items-center justify-between bg-blue-50 rounded-lg px-3 py-2">
              <div>
                <p class="text-sm font-medium text-blue-900">{{ selectedMember.prenom }} {{ selectedMember.nom }}</p>
                <p class="text-xs text-blue-600">{{ selectedMember.numeroMembre }}</p>
              </div>
              <button type="button" class="text-blue-400 hover:text-blue-600" @click="paymentForm.memberId = ''">
                <UIcon name="i-lucide-x" class="w-4 h-4" />
              </button>
            </div>
            <div v-else>
              <UInput
                v-model="memberSearch"
                placeholder="Rechercher un membre par nom ou numéro..."
                icon="i-lucide-search"
              />
              <div v-if="memberSearch && filteredMembers.length" class="mt-1 max-h-40 overflow-y-auto border border-gray-200 rounded-lg bg-white shadow-sm">
                <button
                  v-for="m in filteredMembers"
                  :key="m.id"
                  type="button"
                  class="w-full text-left px-3 py-2 hover:bg-blue-50 transition-colors text-sm"
                  @click="paymentForm.memberId = m.id; memberSearch = ''"
                >
                  <span class="font-medium text-gray-900">{{ m.prenom }} {{ m.nom }}</span>
                  <span class="text-xs text-gray-500 ml-2">{{ m.numeroMembre }}</span>
                </button>
              </div>
            </div>
          </div>

          <div class="grid grid-cols-2 gap-4">
            <div class="space-y-1">
              <label class="text-sm font-medium text-gray-700">Année *</label>
              <UInput v-model.number="paymentForm.annee" type="number" required />
            </div>
            <div class="space-y-1">
              <label class="text-sm font-medium text-gray-700">Date *</label>
              <UInput v-model="paymentForm.datePaiement" type="date" required />
            </div>
          </div>

          <div class="grid grid-cols-2 gap-4">
            <div class="space-y-1">
              <label class="text-sm font-medium text-gray-700">Montant (GNF) *</label>
              <UInput v-model.number="paymentForm.montant" type="number" required />
            </div>
            <div class="space-y-1">
              <label class="text-sm font-medium text-gray-700">Frais</label>
              <UInput v-model.number="paymentForm.fraisPaiement" type="number" />
            </div>
          </div>

          <div class="grid grid-cols-2 gap-4">
            <div class="space-y-1">
              <label class="text-sm font-medium text-gray-700">Moyen de paiement</label>
              <USelect
                v-model="paymentForm.moyenPaiement"
                :items="[
                  { label: 'Espèces', value: 'Especes' },
                  { label: 'Orange Money', value: 'OrangeMoney' },
                  { label: 'Virement', value: 'Virement' },
                  { label: 'Autre', value: 'Autre' }
                ]"
              />
            </div>
            <div class="space-y-1">
              <label class="text-sm font-medium text-gray-700">Référence</label>
              <UInput v-model="paymentForm.reference" placeholder="N° de reçu, référence..." />
            </div>
          </div>

          <div class="space-y-1">
            <label class="text-sm font-medium text-gray-700">Note</label>
            <UTextarea v-model="paymentForm.note" rows="2" placeholder="Commentaire libre..." />
          </div>

          <div class="flex justify-end gap-3 pt-4 border-t border-gray-100">
            <UButton variant="outline" type="button" @click="showPaymentModal = false">Annuler</UButton>
            <UButton type="submit" :loading="saving" icon="i-lucide-save" class="bg-blue-600 hover:bg-blue-700">
              {{ isEditing ? 'Modifier' : 'Enregistrer' }}
            </UButton>
          </div>
        </form>
      </template>
    </UModal>

    <!-- Delete Confirm Modal -->
    <UModal v-model:open="showDeleteConfirm">
      <template #header>
        <h3 class="text-lg font-semibold text-red-600">Confirmer la suppression</h3>
      </template>
      <template #body>
        <p class="text-sm text-gray-600">
          Êtes-vous sûr de vouloir supprimer ce paiement ? Cette action est irréversible.
        </p>
        <div class="flex justify-end gap-3 pt-4 mt-4 border-t border-gray-100">
          <UButton variant="outline" @click="showDeleteConfirm = false">Annuler</UButton>
          <UButton color="error" icon="i-lucide-trash-2" @click="deletePayment">Supprimer</UButton>
        </div>
      </template>
    </UModal>
  </div>
</template>
