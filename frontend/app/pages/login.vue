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
  <div class="min-h-screen flex items-center justify-center px-4 bg-(--ui-bg)">
    <div class="w-full max-w-sm space-y-8">
      <!-- Logo -->
      <div class="text-center space-y-3">
        <div class="inline-flex items-center justify-center w-16 h-16 rounded-2xl bg-(--ui-primary) text-white text-2xl font-bold">
          DK
        </div>
        <div>
          <h1 class="text-2xl font-bold text-(--ui-text-highlighted)">DKPT</h1>
          <p class="text-sm text-(--ui-text-muted)">Diiwal Koïn Préfecture Tougué</p>
        </div>
      </div>

      <!-- Form -->
      <UCard>
        <div class="space-y-4">
          <h2 class="text-lg font-semibold text-(--ui-text-highlighted)">Connexion</h2>

          <form class="space-y-4" @submit.prevent="onSubmit">
            <UFormField label="Email">
              <UInput
                v-model="state.email"
                type="email"
                placeholder="admin@dkpt.com"
                icon="i-lucide-mail"
                required
                autofocus
              />
            </UFormField>

            <UFormField label="Mot de passe">
              <UInput
                v-model="state.password"
                type="password"
                placeholder="••••••••"
                icon="i-lucide-lock"
                required
              />
            </UFormField>

            <UButton
              type="submit"
              block
              :loading="loading"
              icon="i-lucide-log-in"
            >
              Se connecter
            </UButton>
          </form>
        </div>
      </UCard>

      <p class="text-center text-xs text-(--ui-text-muted)">
        © {{ new Date().getFullYear() }} DKPT — Gestion des cotisations
      </p>
    </div>
  </div>
</template>
