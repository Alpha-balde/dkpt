<script setup lang="ts">
const { user, logout, isAdmin } = useAuth()
const route = useRoute()
const colorMode = useColorMode()

const isDark = computed({
  get: () => colorMode.value === 'dark',
  set: (v) => { colorMode.preference = v ? 'dark' : 'light' }
})

// Initiales 2 lettres pour l'avatar
const initials = computed(() => {
  if (!user.value?.email) return '??'
  const parts = user.value.email.split('@')[0].split(/[._-]/)
  if (parts.length >= 2) return (parts[0][0] + parts[1][0]).toUpperCase()
  return user.value.email.substring(0, 2).toUpperCase()
})

const navigationItems = computed(() => {
  const items = [
    { label: 'Tableau de bord', icon: 'i-lucide-layout-dashboard', to: '/dashboard' },
    { label: 'Membres', icon: 'i-lucide-users', to: '/members' },
    { label: 'Cotisations', icon: 'i-lucide-wallet', to: '/cotisations' },
    { label: 'Paiements', icon: 'i-lucide-credit-card', to: '/payments' },
    { label: 'Arriérés', icon: 'i-lucide-alert-circle', to: '/arrieres' }
  ]

  if (isAdmin.value) {
    items.push(
      { label: 'Utilisateurs', icon: 'i-lucide-user-cog', to: '/users' },
      { label: 'Paramètres', icon: 'i-lucide-settings', to: '/settings' }
    )
  }

  return items
})

// Mobile menu
const mobileMenuOpen = ref(false)
</script>

<template>
  <div class="flex h-screen bg-gray-50">
    <!-- Desktop Sidebar -->
    <aside class="hidden lg:flex flex-col w-64 bg-white border-r border-gray-200 fixed top-0 left-0 h-screen z-30">
      <!-- Logo -->
      <div class="px-6 py-5 border-b border-gray-100">
        <h1 class="text-2xl font-bold text-blue-600">DKPT</h1>
        <p class="text-xs text-gray-500">Gestion Association</p>
      </div>

      <!-- Navigation -->
      <nav class="flex-1 px-3 py-4 space-y-1 overflow-y-auto">
        <NuxtLink
          v-for="item in navigationItems"
          :key="item.to"
          :to="item.to"
          class="flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-colors"
          :class="route.path.startsWith(item.to)
            ? 'bg-blue-50 text-blue-700'
            : 'text-gray-700 hover:bg-gray-100'"
        >
          <UIcon :name="item.icon" class="w-5 h-5 shrink-0" />
          {{ item.label }}
        </NuxtLink>
      </nav>

      <!-- Footer: User + Logout -->
      <div class="px-4 py-4 border-t border-gray-100 space-y-3">
        <div class="flex items-center justify-between">
          <span class="text-xs text-gray-500">Thème</span>
          <USwitch v-model="isDark" size="sm" />
        </div>
        <div class="flex items-center gap-3">
          <div class="flex items-center justify-center h-9 w-9 rounded-full bg-blue-100 text-blue-700 text-sm font-semibold shrink-0">
            {{ initials }}
          </div>
          <div class="flex-1 min-w-0">
            <p class="text-sm font-medium text-gray-900 truncate">{{ user?.email }}</p>
            <p class="text-xs text-gray-500">{{ user?.role }}</p>
          </div>
        </div>
        <button
          class="flex items-center gap-2 w-full px-3 py-2 rounded-lg text-sm font-medium text-red-600 hover:bg-red-50 transition-colors"
          @click="logout"
        >
          <UIcon name="i-lucide-log-out" class="w-4 h-4" />
          Déconnexion
        </button>
      </div>
    </aside>

    <!-- Mobile overlay -->
    <Transition name="fade">
      <div
        v-if="mobileMenuOpen"
        class="lg:hidden fixed inset-0 bg-black/50 z-40"
        @click="mobileMenuOpen = false"
      />
    </Transition>

    <!-- Mobile sidebar slide-in -->
    <Transition name="slide">
      <aside
        v-if="mobileMenuOpen"
        class="lg:hidden fixed top-0 left-0 h-screen w-64 bg-white z-50 shadow-xl"
      >
        <div class="px-6 py-5 border-b border-gray-100 flex items-center justify-between">
          <div>
            <h1 class="text-2xl font-bold text-blue-600">DKPT</h1>
            <p class="text-xs text-gray-500">Gestion Association</p>
          </div>
          <button class="p-1" @click="mobileMenuOpen = false">
            <UIcon name="i-lucide-x" class="w-5 h-5 text-gray-500" />
          </button>
        </div>
        <nav class="px-3 py-4 space-y-1">
          <NuxtLink
            v-for="item in navigationItems"
            :key="item.to"
            :to="item.to"
            class="flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-colors"
            :class="route.path.startsWith(item.to)
              ? 'bg-blue-50 text-blue-700'
              : 'text-gray-700 hover:bg-gray-100'"
            @click="mobileMenuOpen = false"
          >
            <UIcon :name="item.icon" class="w-5 h-5 shrink-0" />
            {{ item.label }}
          </NuxtLink>
        </nav>
        <div class="absolute bottom-0 left-0 right-0 px-4 py-4 border-t border-gray-100">
          <button
            class="flex items-center gap-2 w-full px-3 py-2 rounded-lg text-sm font-medium text-red-600 hover:bg-red-50 transition-colors"
            @click="logout"
          >
            <UIcon name="i-lucide-log-out" class="w-4 h-4" />
            Déconnexion
          </button>
        </div>
      </aside>
    </Transition>

    <!-- Main content -->
    <div class="flex-1 flex flex-col min-w-0 lg:ml-64">
      <!-- Mobile header -->
      <header class="lg:hidden flex items-center justify-between px-4 py-3 bg-white border-b border-gray-200 sticky top-0 z-20">
        <div class="flex items-center gap-3">
          <button @click="mobileMenuOpen = true">
            <UIcon name="i-lucide-menu" class="w-6 h-6 text-gray-700" />
          </button>
          <span class="text-lg font-bold text-blue-600">DKPT</span>
        </div>
        <div class="flex items-center gap-2">
          <USwitch v-model="isDark" size="sm" />
          <div class="flex items-center justify-center h-8 w-8 rounded-full bg-blue-100 text-blue-700 text-xs font-semibold">
            {{ initials }}
          </div>
        </div>
      </header>

      <!-- Page content -->
      <main class="flex-1 overflow-y-auto">
        <div class="max-w-7xl mx-auto p-4 md:p-8">
          <slot />
        </div>
      </main>
    </div>
  </div>
</template>

<style scoped>
.fade-enter-active, .fade-leave-active {
  transition: opacity 0.2s ease;
}
.fade-enter-from, .fade-leave-to {
  opacity: 0;
}
.slide-enter-active, .slide-leave-active {
  transition: transform 0.3s ease;
}
.slide-enter-from, .slide-leave-to {
  transform: translateX(-100%);
}
</style>
