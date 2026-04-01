<script setup lang="ts">
definePageMeta({ layout: 'auth' })

const { login } = useAuth()
const toast = useToast()

const state = reactive({
  email: '',
  password: ''
})
const loading = ref(false)

async function onSubmit() {
  loading.value = true
  const success = await login({ email: state.email, password: state.password })
  loading.value = false

  if (success) {
    navigateTo('/dashboard')
  } else {
    toast.add({
      title: 'Erreur de connexion',
      description: 'Email ou mot de passe incorrect.',
      color: 'error',
      icon: 'i-lucide-alert-circle'
    })
  }
}
</script>

<template>
  <div class="min-h-screen flex items-center justify-center px-4 bg-gradient-to-br from-blue-50 to-indigo-100">
    <div class="w-full max-w-md">
      <!-- Login Card -->
      <div class="bg-white rounded-2xl shadow-xl p-8 space-y-6">
        <!-- Logo -->
        <div class="text-center space-y-3">
          <div class="inline-flex items-center justify-center w-16 h-16 rounded-full bg-blue-100">
            <UIcon name="i-lucide-shield-check" class="w-8 h-8 text-blue-600" />
          </div>
          <div>
            <h1 class="text-2xl font-bold text-gray-900">DKPT</h1>
            <p class="text-sm text-gray-500">Diiwal Koïn Préfecture Tougué</p>
          </div>
        </div>

        <!-- Separator -->
        <div class="border-t border-gray-100" />

        <!-- Form -->
        <form class="space-y-4" @submit.prevent="onSubmit">
          <div class="space-y-2">
            <label class="text-sm font-medium text-gray-700">Email</label>
            <UInput
              v-model="state.email"
              type="email"
              placeholder="admin@dkpt.com"
              icon="i-lucide-mail"
              size="lg"
              required
              autofocus
            />
          </div>

          <div class="space-y-2">
            <label class="text-sm font-medium text-gray-700">Mot de passe</label>
            <UInput
              v-model="state.password"
              type="password"
              placeholder="••••••••"
              icon="i-lucide-lock"
              size="lg"
              required
            />
          </div>

          <UButton
            type="submit"
            block
            size="lg"
            :loading="loading"
            class="bg-blue-600 hover:bg-blue-700 mt-2"
          >
            <template v-if="loading">
              Connexion...
            </template>
            <template v-else>
              Se connecter
            </template>
          </UButton>
        </form>
      </div>

      <!-- Footer -->
      <p class="text-center text-xs text-gray-400 mt-6">
        © {{ new Date().getFullYear() }} DKPT — Gestion des cotisations
      </p>
    </div>
  </div>
</template>
