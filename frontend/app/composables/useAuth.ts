import type { LoginRequest, LoginResponse } from '~/types'

export function useAuth() {
  const token = useCookie('dkpt_token', { maxAge: 60 * 60 * 24 })
  const user = useState<{ email: string, role: string } | null>('auth_user', () => null)
  const config = useRuntimeConfig()

  const isAuthenticated = computed(() => !!token.value)

  const isAdmin = computed(() => user.value?.role === 'Admin')
  const isSecretaire = computed(() => user.value?.role === 'Secretaire')
  const isTresorier = computed(() => user.value?.role === 'Tresorier')

  const canManageMembers = computed(() =>
    ['Admin', 'Secretaire'].includes(user.value?.role || '')
  )
  const canManagePayments = computed(() =>
    ['Admin', 'Tresorier'].includes(user.value?.role || '')
  )

  async function login(credentials: LoginRequest): Promise<boolean> {
    try {
      const data = await $fetch<LoginResponse>(`${config.public.apiBase}/Auth/login`, {
        method: 'POST',
        body: credentials
      })
      token.value = data.token
      user.value = { email: data.email, role: data.role }
      return true
    }
    catch {
      return false
    }
  }

  function logout() {
    token.value = null
    user.value = null
    navigateTo('/login')
  }

  function parseToken() {
    if (!token.value) return
    try {
      const payload = JSON.parse(atob(token.value.split('.')[1]))
      user.value = {
        email: payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'] || '',
        role: payload['role'] || 'Lecteur'
      }
    }
    catch {
      token.value = null
    }
  }

  // Auto-parse on init
  if (token.value && !user.value) {
    parseToken()
  }

  return {
    token,
    user,
    isAuthenticated,
    isAdmin,
    isSecretaire,
    isTresorier,
    canManageMembers,
    canManagePayments,
    login,
    logout,
    parseToken
  }
}
