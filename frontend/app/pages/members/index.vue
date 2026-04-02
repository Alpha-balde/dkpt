<script setup lang="ts">
import type { Member, PagedResult } from '~/types'

const { apiFetch } = useApi()
const { canManageMembers, isAdmin } = useAuth()
const toast = useToast()

const search = ref('')
const statusFilter = ref('all')
const page = ref(1)
const pageSize = 20
const normalizedStatusFilter = computed(() => {
  if (typeof statusFilter.value === 'string') return statusFilter.value

  if (statusFilter.value && typeof statusFilter.value === 'object' && 'value' in statusFilter.value) {
    return String((statusFilter.value as { value: unknown }).value)
  }

  return 'all'
})

const { data, refresh, status } = await useAsyncData(
  'members',
  () => {
    const actifFilter = normalizedStatusFilter.value === 'actif'
      ? '&actif=true'
      : normalizedStatusFilter.value === 'inactif'
        ? '&actif=false'
        : ''
    const legacyStatusFilter = normalizedStatusFilter.value === 'actif'
      ? '&status=Actif'
      : normalizedStatusFilter.value === 'inactif'
        ? '&status=Inactif'
        : ''

    return apiFetch<PagedResult<Member>>(
      `/Members?page=${page.value}&pageSize=${pageSize}${search.value ? `&search=${encodeURIComponent(search.value)}` : ''}${actifFilter}${legacyStatusFilter}`
    )
  },
  { watch: [page] }
)

const totalPages = computed(() => Math.ceil((data.value?.totalCount || 0) / pageSize))

function onSearch() {
  page.value = 1
  refresh()
}

function onFilterChange() {
  page.value = 1
  refresh()
}

watch(normalizedStatusFilter, () => {
  onFilterChange()
})

// Year options for selects (2010 → current year + 1)
const currentYear = new Date().getFullYear()
const yearOptions = computed(() => {
  const years = []
  for (let y = currentYear + 1; y >= 2010; y--) {
    years.push({ label: y.toString(), value: y })
  }
  return years
})

// ===== CREATE MODAL =====
const showCreateModal = ref(false)
const createForm = reactive({
  numeroMembre: '',
  prenom: '',
  nom: '',
  telephone: '',
  whatsApp: '',
  residence: '',
  village: '',
  sousPrefecture: '',
  anneeDebut: currentYear,
  actif: true
})
const creating = ref(false)

function openCreateModal() {
  Object.assign(createForm, {
    numeroMembre: '', prenom: '', nom: '', telephone: '', whatsApp: '',
    residence: '', village: '', sousPrefecture: '', anneeDebut: currentYear, actif: true
  })
  showCreateModal.value = true
}

async function createMember() {
  creating.value = true
  try {
    await apiFetch('/Members', { method: 'POST', body: createForm })
    toast.add({ title: 'Membre créé avec succès', color: 'success', icon: 'i-lucide-check-circle' })
    showCreateModal.value = false
    refresh()
  } catch (err: any) {
    toast.add({ title: 'Erreur', description: err?.data?.message || 'Impossible de créer le membre', color: 'error' })
  } finally {
    creating.value = false
  }
}

// ===== EDIT MODAL =====
const showEditModal = ref(false)
const editingMemberId = ref<string | null>(null)
const editForm = reactive({
  numeroMembre: '',
  prenom: '',
  nom: '',
  telephone: '',
  whatsApp: '',
  residence: '',
  village: '',
  sousPrefecture: '',
  anneeDebut: currentYear,
  actif: true
})
const saving = ref(false)

function openEditModal(member: Member) {
  editingMemberId.value = member.id
  Object.assign(editForm, {
    numeroMembre: member.numeroMembre,
    prenom: member.prenom,
    nom: member.nom,
    telephone: member.telephone || '',
    whatsApp: member.whatsApp || '',
    residence: member.residence || '',
    village: member.village || '',
    sousPrefecture: member.sousPrefecture || '',
    anneeDebut: member.anneeDebut,
    actif: member.actif
  })
  showEditModal.value = true
}

async function updateMember() {
  if (!editingMemberId.value) return
  saving.value = true
  try {
    await apiFetch(`/Members/${editingMemberId.value}`, { method: 'PUT', body: editForm })
    toast.add({ title: 'Membre modifié', color: 'success', icon: 'i-lucide-check-circle' })
    showEditModal.value = false
    refresh()
  } catch (err: any) {
    toast.add({ title: 'Erreur', description: err?.data?.message || 'Impossible de modifier', color: 'error' })
  } finally {
    saving.value = false
  }
}

// ===== DELETE CONFIRM =====
const showDeleteConfirm = ref(false)
const deletingMember = ref<Member | null>(null)

function confirmDelete(member: Member) {
  deletingMember.value = member
  showDeleteConfirm.value = true
}

async function deleteMember() {
  if (!deletingMember.value) return
  try {
    await apiFetch(`/Members/${deletingMember.value.id}`, { method: 'DELETE' })
    toast.add({ title: 'Membre supprimé', color: 'success', icon: 'i-lucide-trash-2' })
    showDeleteConfirm.value = false
    refresh()
  } catch {
    toast.add({ title: 'Erreur de suppression', color: 'error' })
  }
}
</script>

<template>
  <div class="space-y-6">
    <!-- Header -->
    <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
      <div>
        <h1 class="text-3xl font-bold text-gray-900">Membres</h1>
        <p class="text-sm text-gray-500 mt-1">{{ data?.totalCount || 0 }} membres enregistrés</p>
      </div>
      <UButton
        v-if="canManageMembers"
        icon="i-lucide-plus"
        class="bg-blue-600 hover:bg-blue-700"
        @click="openCreateModal"
      >
        Nouveau membre
      </UButton>
    </div>

    <!-- Filters -->
    <div class="bg-white rounded-xl p-6 shadow-sm border border-gray-100">
      <div class="grid grid-cols-1 md:grid-cols-4 gap-4">
        <UInput
          v-model="search"
          placeholder="Rechercher par nom, prénom, numéro..."
          icon="i-lucide-search"
          class="md:col-span-2"
          @keyup.enter="onSearch"
        />
        <USelect
          v-model="statusFilter"
          :items="[
            { label: 'Tous les statuts', value: 'all' },
            { label: 'Actifs', value: 'actif' },
            { label: 'Inactifs', value: 'inactif' }
          ]"
          value-key="value"
          label-key="label"
          placeholder="Statut"
        />
        <UButton variant="soft" icon="i-lucide-search" @click="onSearch">
          Rechercher
        </UButton>
      </div>
    </div>

    <!-- Table -->
    <div class="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden">
      <div class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead>
            <tr class="bg-gray-50 border-b border-gray-200">
              <th class="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase tracking-wider">N°</th>
              <th class="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase tracking-wider">Nom complet</th>
              <th class="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase tracking-wider hidden sm:table-cell">Téléphone</th>
              <th class="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase tracking-wider hidden md:table-cell">Village</th>
              <th class="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase tracking-wider hidden lg:table-cell">Année</th>
              <th class="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase tracking-wider">Statut</th>
              <th class="px-4 py-3 text-right text-xs font-semibold text-gray-600 uppercase tracking-wider">Actions</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-100">
            <tr
              v-for="member in data?.items"
              :key="member.id"
              class="hover:bg-gray-50 transition-colors"
            >
              <td class="px-4 py-3 font-mono text-xs text-gray-500">{{ member.numeroMembre }}</td>
              <td class="px-4 py-3">
                <NuxtLink :to="`/members/${member.id}`" class="group">
                  <p class="font-medium text-gray-900 group-hover:text-blue-600 transition-colors">
                    {{ member.prenom }} {{ member.nom }}
                  </p>
                  <p class="text-xs text-gray-500">{{ member.residence || '—' }}</p>
                </NuxtLink>
              </td>
              <td class="px-4 py-3 text-gray-600 hidden sm:table-cell">{{ member.telephone || '—' }}</td>
              <td class="px-4 py-3 text-gray-600 hidden md:table-cell">{{ member.village || '—' }}</td>
              <td class="px-4 py-3 hidden lg:table-cell">
                <span class="inline-flex items-center rounded-full bg-blue-50 text-blue-700 px-2 py-0.5 text-xs font-medium">{{ member.anneeDebut }}</span>
              </td>
              <td class="px-4 py-3">
                <span
                  :class="member.actif
                    ? 'bg-green-100 text-green-800'
                    : 'bg-red-100 text-red-800'"
                  class="inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium"
                >
                  {{ member.actif ? 'Actif' : 'Inactif' }}
                </span>
              </td>
              <td class="px-4 py-3">
                <div class="flex items-center justify-end gap-1">
                  <button
                    class="h-8 w-8 inline-flex items-center justify-center rounded-lg text-gray-500 hover:text-blue-600 hover:bg-blue-50 transition-colors"
                    title="Voir le profil"
                    @click="navigateTo(`/members/${member.id}`)"
                  >
                    <UIcon name="i-lucide-eye" class="w-4 h-4" />
                  </button>
                  <button
                    v-if="canManageMembers"
                    class="h-8 w-8 inline-flex items-center justify-center rounded-lg text-gray-500 hover:text-blue-600 hover:bg-blue-50 transition-colors"
                    title="Modifier"
                    @click="openEditModal(member)"
                  >
                    <UIcon name="i-lucide-pencil" class="w-4 h-4" />
                  </button>
                  <button
                    v-if="isAdmin"
                    class="h-8 w-8 inline-flex items-center justify-center rounded-lg text-gray-500 hover:text-red-600 hover:bg-red-50 transition-colors"
                    title="Supprimer"
                    @click="confirmDelete(member)"
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
              <td colspan="7" class="px-4 py-12 text-center text-gray-500">Aucun membre trouvé</td>
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

    <!-- CREATE MODAL -->
    <UModal v-model:open="showCreateModal">
      <template #header>
        <h3 class="text-lg font-semibold text-gray-900">Nouveau membre</h3>
      </template>
      <template #body>
        <form class="space-y-4 max-h-[70vh] overflow-y-auto pr-1" @submit.prevent="createMember">
          <div class="grid grid-cols-2 gap-4">
            <div class="space-y-1">
              <label class="text-sm font-medium text-gray-700">N° Membre *</label>
              <UInput v-model="createForm.numeroMembre" placeholder="ID_0500" required />
            </div>
            <div class="space-y-1">
              <label class="text-sm font-medium text-gray-700">Statut</label>
              <USelect v-model="createForm.actif" :items="[{ label: 'Actif', value: true }, { label: 'Inactif', value: false }]" />
            </div>
          </div>
          <div class="grid grid-cols-2 gap-4">
            <div class="space-y-1">
              <label class="text-sm font-medium text-gray-700">Prénom *</label>
              <UInput v-model="createForm.prenom" required />
            </div>
            <div class="space-y-1">
              <label class="text-sm font-medium text-gray-700">Nom *</label>
              <UInput v-model="createForm.nom" required />
            </div>
          </div>
          <div class="grid grid-cols-2 gap-4">
            <div class="space-y-1">
              <label class="text-sm font-medium text-gray-700">Téléphone</label>
              <UInput v-model="createForm.telephone" />
            </div>
            <div class="space-y-1">
              <label class="text-sm font-medium text-gray-700">WhatsApp</label>
              <UInput v-model="createForm.whatsApp" />
            </div>
          </div>
          <div class="grid grid-cols-2 gap-4">
            <div class="space-y-1">
              <label class="text-sm font-medium text-gray-700">Résidence</label>
              <UInput v-model="createForm.residence" />
            </div>
            <div class="space-y-1">
              <label class="text-sm font-medium text-gray-700">Année d'adhésion</label>
              <USelect v-model="createForm.anneeDebut" :items="yearOptions" value-key="value" label-key="label" />
            </div>
          </div>
          <div class="grid grid-cols-2 gap-4">
            <div class="space-y-1">
              <label class="text-sm font-medium text-gray-700">Village</label>
              <UInput v-model="createForm.village" />
            </div>
            <div class="space-y-1">
              <label class="text-sm font-medium text-gray-700">Sous-préfecture</label>
              <UInput v-model="createForm.sousPrefecture" />
            </div>
          </div>
          <div class="flex justify-end gap-3 pt-4 border-t border-gray-100">
            <UButton variant="outline" type="button" @click="showCreateModal = false">Annuler</UButton>
            <UButton type="submit" :loading="creating" icon="i-lucide-save" class="bg-blue-600 hover:bg-blue-700">Enregistrer</UButton>
          </div>
        </form>
      </template>
    </UModal>

    <!-- EDIT MODAL -->
    <UModal v-model:open="showEditModal">
      <template #header>
        <h3 class="text-lg font-semibold text-gray-900">Modifier le membre</h3>
      </template>
      <template #body>
        <form class="space-y-4 max-h-[70vh] overflow-y-auto pr-1" @submit.prevent="updateMember">
          <div class="grid grid-cols-2 gap-4">
            <div class="space-y-1">
              <label class="text-sm font-medium text-gray-700">N° Membre</label>
              <UInput v-model="editForm.numeroMembre" />
            </div>
            <div class="space-y-1">
              <label class="text-sm font-medium text-gray-700">Statut</label>
              <USelect v-model="editForm.actif" :items="[{ label: 'Actif', value: true }, { label: 'Inactif', value: false }]" />
            </div>
          </div>
          <div class="grid grid-cols-2 gap-4">
            <div class="space-y-1">
              <label class="text-sm font-medium text-gray-700">Prénom</label>
              <UInput v-model="editForm.prenom" />
            </div>
            <div class="space-y-1">
              <label class="text-sm font-medium text-gray-700">Nom</label>
              <UInput v-model="editForm.nom" />
            </div>
          </div>
          <div class="grid grid-cols-2 gap-4">
            <div class="space-y-1">
              <label class="text-sm font-medium text-gray-700">Téléphone</label>
              <UInput v-model="editForm.telephone" />
            </div>
            <div class="space-y-1">
              <label class="text-sm font-medium text-gray-700">WhatsApp</label>
              <UInput v-model="editForm.whatsApp" />
            </div>
          </div>
          <div class="grid grid-cols-2 gap-4">
            <div class="space-y-1">
              <label class="text-sm font-medium text-gray-700">Résidence</label>
              <UInput v-model="editForm.residence" />
            </div>
            <div class="space-y-1">
              <label class="text-sm font-medium text-gray-700">Année d'adhésion</label>
              <USelect v-model="editForm.anneeDebut" :items="yearOptions" value-key="value" label-key="label" />
            </div>
          </div>
          <div class="grid grid-cols-2 gap-4">
            <div class="space-y-1">
              <label class="text-sm font-medium text-gray-700">Village</label>
              <UInput v-model="editForm.village" />
            </div>
            <div class="space-y-1">
              <label class="text-sm font-medium text-gray-700">Sous-préfecture</label>
              <UInput v-model="editForm.sousPrefecture" />
            </div>
          </div>
          <div class="flex justify-end gap-3 pt-4 border-t border-gray-100">
            <UButton variant="outline" type="button" @click="showEditModal = false">Annuler</UButton>
            <UButton type="submit" :loading="saving" icon="i-lucide-save" class="bg-blue-600 hover:bg-blue-700">Enregistrer</UButton>
          </div>
        </form>
      </template>
    </UModal>

    <!-- DELETE CONFIRM -->
    <UModal v-model:open="showDeleteConfirm">
      <template #header>
        <h3 class="text-lg font-semibold text-red-600">Confirmer la suppression</h3>
      </template>
      <template #body>
        <p class="text-sm text-gray-600">
          Supprimer le membre <strong>{{ deletingMember?.prenom }} {{ deletingMember?.nom }}</strong> ({{ deletingMember?.numeroMembre }}) ?
          Cette action est irréversible.
        </p>
        <div class="flex justify-end gap-3 pt-4 mt-4 border-t border-gray-100">
          <UButton variant="outline" @click="showDeleteConfirm = false">Annuler</UButton>
          <UButton color="error" icon="i-lucide-trash-2" @click="deleteMember">Supprimer</UButton>
        </div>
      </template>
    </UModal>
  </div>
</template>
