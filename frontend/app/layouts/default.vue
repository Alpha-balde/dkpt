<script setup lang="ts">
const { user, logout, isAdmin } = useAuth()
const route = useRoute()
const colorMode = useColorMode()

const isDark = computed({
  get: () => colorMode.value === 'dark',
  set: (v) => { colorMode.preference = v ? 'dark' : 'light' }
})

const navigationItems = computed(() => {
  const items = [
    {
      label: 'Tableau de bord',
      icon: 'i-lucide-layout-dashboard',
      to: '/dashboard'
    },
    {
      label: 'Membres',
      icon: 'i-lucide-users',
      to: '/members'
    },
    {
      label: 'Paiements',
      icon: 'i-lucide-credit-card',
      to: '/payments'
    },
    {
      label: 'Cotisations',
      icon: 'i-lucide-bar-chart-3',
      to: '/cotisations'
    },
    {
      label: 'Arriérés',
      icon: 'i-lucide-alert-triangle',
      to: '/arrieres'
    }
  ]

  if (isAdmin.value) {
    items.push(
      {
        label: 'Utilisateurs',
        icon: 'i-lucide-shield',
        to: '/users'
      },
      {
        label: 'Paramètres',
        icon: 'i-lucide-settings',
        to: '/settings'
      }
    )
  }

  return items
})
</script>

<template>
  <div class="flex h-screen">
    <!-- Sidebar -->
    <aside class="hidden lg:flex flex-col w-64 border-r border-(--ui-border) bg-(--ui-bg-elevated)">
      <!-- Logo -->
      <div class="flex items-center gap-3 px-6 py-5 border-b border-(--ui-border)">
        <div class="flex items-center justify-center w-9 h-9 rounded-lg bg-(--ui-primary) text-white font-bold text-sm">
          DK
        </div>
        <div>
          <h1 class="text-sm font-semibold text-(--ui-text-highlighted)">DKPT</h1>
          <p class="text-xs text-(--ui-text-muted)">Gestion Association</p>
        </div>
      </div>

      <!-- Navigation -->
      <nav class="flex-1 px-3 py-4 space-y-1 overflow-y-auto">
        <NuxtLink
          v-for="item in navigationItems"
          :key="item.to"
          :to="item.to"
          class="flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-colors"
          :class="route.path.startsWith(item.to)
            ? 'bg-(--ui-primary)/10 text-(--ui-primary)'
            : 'text-(--ui-text-muted) hover:text-(--ui-text-highlighted) hover:bg-(--ui-bg-accented)'"
        >
          <UIcon :name="item.icon" class="w-5 h-5 shrink-0" />
          {{ item.label }}
        </NuxtLink>
      </nav>

      <!-- Footer -->
      <div class="px-4 py-4 border-t border-(--ui-border) space-y-3">
        <div class="flex items-center justify-between">
          <span class="text-xs text-(--ui-text-muted)">Thème</span>
          <USwitch v-model="isDark" size="sm" />
        </div>
        <div class="flex items-center gap-3">
          <UAvatar :text="user?.email?.charAt(0).toUpperCase()" size="sm" />
          <div class="flex-1 min-w-0">
            <p class="text-xs font-medium text-(--ui-text-highlighted) truncate">{{ user?.email }}</p>
            <p class="text-xs text-(--ui-text-muted)">{{ user?.role }}</p>
          </div>
          <UButton icon="i-lucide-log-out" color="neutral" variant="ghost" size="xs" @click="logout" />
        </div>
      </div>
    </aside>

    <!-- Mobile header + content -->
    <div class="flex-1 flex flex-col min-w-0">
      <!-- Mobile header -->
      <header class="lg:hidden flex items-center justify-between px-4 py-3 border-b border-(--ui-border) bg-(--ui-bg-elevated)">
        <div class="flex items-center gap-2">
          <div class="flex items-center justify-center w-8 h-8 rounded-lg bg-(--ui-primary) text-white font-bold text-xs">
            DK
          </div>
          <span class="text-sm font-semibold">DKPT</span>
        </div>
        <div class="flex items-center gap-2">
          <USwitch v-model="isDark" size="sm" />
          <UButton icon="i-lucide-log-out" color="neutral" variant="ghost" size="xs" @click="logout" />
        </div>
      </header>

      <!-- Page content -->
      <main class="flex-1 overflow-y-auto p-4 lg:p-8">
        <slot />
      </main>

      <!-- Mobile bottom nav -->
      <nav class="lg:hidden flex items-center justify-around border-t border-(--ui-border) bg-(--ui-bg-elevated) py-2">
        <NuxtLink
          v-for="item in navigationItems.slice(0, 5)"
          :key="item.to"
          :to="item.to"
          class="flex flex-col items-center gap-1 px-3 py-1"
          :class="route.path.startsWith(item.to)
            ? 'text-(--ui-primary)'
            : 'text-(--ui-text-muted)'"
        >
          <UIcon :name="item.icon" class="w-5 h-5" />
          <span class="text-[10px]">{{ item.label.split(' ')[0] }}</span>
        </NuxtLink>
      </nav>
    </div>
  </div>
</template>
