export function useApi() {
  const config = useRuntimeConfig()
  const { token, logout } = useAuth()

  async function apiFetch<T>(path: string, options: any = {}): Promise<T> {
    const baseUrl = import.meta.server && config.apiBaseInternal
      ? config.apiBaseInternal
      : config.public.apiBase
    return $fetch<T>(`${baseUrl}${path}`, {
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
