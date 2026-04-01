<script setup lang="ts">
import type { Member, PagedResult } from '~/types'

const { apiFetch } = useApi()
const { canManageMembers } = useAuth()
const toast = useToast()

const search = ref('')
const page = ref(1)
const pageSize = 20

const { data, refresh, status } = await useAsyncData(
  'members',
  () => apiFetch<PagedResult<Member>>(
    `/Members?page=${page.value}&pageSize=${pageSize}${search.value ? `&search=${encodeURIComponent(search.value)}` : ''}`
  ),
  { watch: [page] }
)

const columns = [
  { key: 'numeroMembre', label: 'N° Membre' },
  { key: 'prenom', label: 'Prénom' },
  { key: 'nom', label: 'Nom' },
  { key: 'telephone', label: 'Téléphone' },
  { key: 'residence', label: 'Résidence' },
  { key: 'village', label: 'Village' },
  { key: 'anneeDebut', label: 'Année' },
  { key: 'actif', label: 'Statut' },
  { key: 'actions', label: '' }
]

const totalPages = computed(() => Math.ceil((data.value?.totalCount || 0) / pageSize))

function onSearch() {
  page.value = 1
  refresh()
}

// Create member modal
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
  anneeDebut: new Date().getFullYear(),
  actif: true
})
const creating = ref(false)

async function createMember() {
  creating.value = true
  try {
    await apiFetch('/Members', {
      method: 'POST',
      body: createForm
    })
    toast.add({ title: 'Membre créé avec succès', color: 'success', icon: 'i-lucide-check-circle' })
    showCreateModal.value = false
    Object.assign(createForm, { numeroMembre: '', prenom: '', nom: '', telephone: '', whatsApp: '', residence: '', village: '', sousPrefecture: '' })
    refresh()
  } catch (err: any) {
    toast.add({ title: 'Erreur', description: err?.data?.message || 'Impossible de créer le membre', color: 'error' })
  } finally {
    creating.value = false
  }
}

async function deleteMember(member: Member) {
  if (!confirm(`Supprimer ${member.prenom} ${member.nom} ?`)) return
  try {
    await apiFetch(`/Members/${member.id}`, { method: 'DELETE' })
    toast.add({ title: 'Membre supprimé', color: 'success', icon: 'i-lucide-trash-2' })
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
        <h1 class="text-2xl font-bold text-(--ui-text-highlighted)">Membres</h1>
        <p class="text-sm text-(--ui-text-muted)">
          {{ data?.totalCount || 0 }} membres enregistrés
        </p>
      </div>
      <UButton
        v-if="canManageMembers"
        icon="i-lucide-plus"
        @click="showCreateModal = true"
      >
        Nouveau membre
      </UButton>
    </div>

    <!-- Search -->
    <div class="flex gap-3">
      <UInput
        v-model="search"
        placeholder="Rechercher par nom, prénom, numéro..."
        icon="i-lucide-search"
        class="flex-1"
        @keyup.enter="onSearch"
      />
      <UButton variant="soft" @click="onSearch">
        Rechercher
      </UButton>
    </div>

    <!-- Table -->
    <UCard :ui="{ body: 'p-0' }">
      <div class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead>
            <tr class="border-b border-(--ui-border)">
              <th v-for="col in columns" :key="col.key" class="px-4 py-3 text-left text-xs font-medium text-(--ui-text-muted) uppercase tracking-wider">
                {{ col.label }}
              </th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="member in data?.items"
              :key="member.id"
              class="border-b border-(--ui-border) hover:bg-(--ui-bg-elevated) transition-colors cursor-pointer"
              @click="navigateTo(`/members/${member.id}`)"
            >
              <td class="px-4 py-3 font-mono text-xs">{{ member.numeroMembre }}</td>
              <td class="px-4 py-3 font-medium text-(--ui-text-highlighted)">{{ member.prenom }}</td>
              <td class="px-4 py-3">{{ member.nom }}</td>
              <td class="px-4 py-3 text-(--ui-text-muted)">{{ member.telephone || '—' }}</td>
              <td class="px-4 py-3 text-(--ui-text-muted)">{{ member.residence || '—' }}</td>
              <td class="px-4 py-3 text-(--ui-text-muted)">{{ member.village || '—' }}</td>
              <td class="px-4 py-3">{{ member.anneeDebut }}</td>
              <td class="px-4 py-3">
                <UBadge
                  :label="member.actif ? 'Actif' : 'Inactif'"
                  :color="member.actif ? 'success' : 'error'"
                  variant="subtle"
                  size="sm"
                />
              </td>
              <td class="px-4 py-3" @click.stop>
                <UButton
                  v-if="canManageMembers"
                  icon="i-lucide-trash-2"
                  color="error"
                  variant="ghost"
                  size="xs"
                  @click="deleteMember(member)"
                />
              </td>
            </tr>
            <tr v-if="status === 'pending'">
              <td :colspan="columns.length" class="px-4 py-8 text-center">
                <UIcon name="i-lucide-loader-2" class="w-6 h-6 animate-spin text-(--ui-text-muted)" />
              </td>
            </tr>
            <tr v-else-if="!data?.items?.length">
              <td :colspan="columns.length" class="px-4 py-8 text-center text-(--ui-text-muted)">
                Aucun membre trouvé
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </UCard>

    <!-- Pagination -->
    <div v-if="totalPages > 1" class="flex justify-center">
      <UPagination v-model="page" :total="data?.totalCount || 0" :items-per-page="pageSize" />
    </div>

    <!-- Create Modal -->
    <UModal v-model:open="showCreateModal">
      <template #header>
        <h3 class="text-lg font-semibold">Nouveau membre</h3>
      </template>
      <template #body>
        <form class="space-y-4" @submit.prevent="createMember">
          <div class="grid grid-cols-2 gap-4">
            <UFormField label="N° Membre" required>
              <UInput v-model="createForm.numeroMembre" placeholder="ID_0500" required />
            </UFormField>
            <UFormField label="Année début">
              <UInput v-model.number="createForm.anneeDebut" type="number" />
            </UFormField>
          </div>
          <div class="grid grid-cols-2 gap-4">
            <UFormField label="Prénom" required>
              <UInput v-model="createForm.prenom" required />
            </UFormField>
            <UFormField label="Nom" required>
              <UInput v-model="createForm.nom" required />
            </UFormField>
          </div>
          <div class="grid grid-cols-2 gap-4">
            <UFormField label="Téléphone">
              <UInput v-model="createForm.telephone" />
            </UFormField>
            <UFormField label="WhatsApp">
              <UInput v-model="createForm.whatsApp" />
            </UFormField>
          </div>
          <div class="grid grid-cols-2 gap-4">
            <UFormField label="Résidence">
              <UInput v-model="createForm.residence" />
            </UFormField>
            <UFormField label="Village">
              <UInput v-model="createForm.village" />
            </UFormField>
          </div>
          <UFormField label="Sous-préfecture">
            <UInput v-model="createForm.sousPrefecture" />
          </UFormField>
          <div class="flex justify-end gap-3 pt-2">
            <UButton variant="ghost" @click="showCreateModal = false">Annuler</UButton>
            <UButton type="submit" :loading="creating">Créer</UButton>
          </div>
        </form>
      </template>
    </UModal>
  </div>
</template>
