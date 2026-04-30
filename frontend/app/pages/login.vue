<script setup lang="ts">
definePageMeta({ layout: 'auth' })

const { login } = useAuth()
const toast = useToast()

const state = reactive({
  email: '',
  password: ''
})
const loading = ref(false)
const currentYear = new Date().getFullYear()

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
  <div class="min-h-screen grid place-items-center px-4 py-8 bg-gradient-to-b from-slate-100 via-blue-50 to-indigo-100">
    <div class="w-full max-w-md space-y-5">
      <div class="text-center space-y-3">
        <div class="mx-auto inline-flex h-16 w-16 items-center justify-center rounded-2xl bg-blue-100 ring-1 ring-blue-200 shadow-inner">
          <UIcon
            name="i-lucide-shield-check"
            class="w-8 h-8 text-blue-600"
          />
        </div>
        <div>
          <h1 class="text-3xl font-bold tracking-tight text-gray-900">
            DKPT
          </h1>
          <p class="mt-1 text-sm text-gray-600">
            Diiwal Koïn Préfecture Tougué
          </p>
        </div>
      </div>

      <UCard class="rounded-2xl shadow-xl ring-1 ring-gray-200/80">
        <template #header>
          <div class="text-center">
            <h2 class="text-lg font-semibold text-gray-900">
              Connexion
            </h2>
            <p class="mt-1 text-xs text-gray-500">
              Accedez a votre espace DKPT
            </p>
          </div>
        </template>

        <form
          class="space-y-4"
          @submit.prevent="onSubmit"
        >
          <UFormField
            label="Email"
            required
          >
            <UInput
              v-model="state.email"
              type="email"
              placeholder="admin@dkpt.com"
              icon="i-lucide-mail"
              size="lg"
              autocomplete="email"
              class="w-full"
              required
              autofocus
            />
          </UFormField>

          <UFormField
            label="Mot de passe"
            required
          >
            <UInput
              v-model="state.password"
              type="password"
              placeholder="********"
              icon="i-lucide-lock"
              size="lg"
              autocomplete="current-password"
              class="w-full"
              required
            />
          </UFormField>

          <UButton
            type="submit"
            block
            size="lg"
            icon="i-lucide-log-in"
            :loading="loading"
            class="mt-2 bg-blue-600 hover:bg-blue-700"
          >
            {{ loading ? 'Connexion...' : 'Se connecter' }}
          </UButton>
        </form>
      </UCard>

      <p class="text-center text-xs text-gray-500">
        © {{ currentYear }} DKPT — Gestion des cotisations
      </p>
    </div>
  </div>
</template>
