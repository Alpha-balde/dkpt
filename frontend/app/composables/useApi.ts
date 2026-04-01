export function useApi() {
  const config = useRuntimeConfig()
  const { token, logout } = useAuth()

  async function apiFetch<T>(path: string, options: any = {}): Promise<T> {
    return $fetch<T>(`${config.public.apiBase}${path}`, {
      ...options,
      headers: {
        ...options.headers,
        ...(token.value ? { Authorization: `Bearer ${token.value}` } : {})
      },
      onResponseError({ response }) {
        if (response.status === 401) {
          logout()
        }
      }
    })
  }

  return { apiFetch }
}
