import type { ProblemDetailsDto, ValidationProblemDetailsDto } from './types'

const BASE_URL = import.meta.env.VITE_API_BASE_URL

export class ApiError extends Error {
  status: number
  errors?: Record<string, string[]>

  constructor(status: number, message: string, errors?: Record<string, string[]>) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.errors = errors
  }
}

async function parseError(res: Response): Promise<ApiError> {
  let body: ProblemDetailsDto | ValidationProblemDetailsDto | null = null
  try {
    body = await res.json()
  } catch {
    // Pas de corps JSON (ex. 401 sans ProblemDetails) — on retombe sur le statut HTTP.
  }
  if (body && 'errors' in body) {
    return new ApiError(res.status, body.title, body.errors)
  }
  return new ApiError(res.status, body?.detail ?? body?.title ?? res.statusText)
}

async function parseResponse<T>(res: Response): Promise<T> {
  if (!res.ok) throw await parseError(res)
  if (res.status === 204) return undefined as T
  return (await res.json()) as T
}

/**
 * Requête bas niveau : pas d'injection de token, pas de retry sur 401.
 * Utilisée par api/auth.ts (login/refresh/logout) pour éviter toute
 * récursion avec apiFetch (qui, lui, appelle refresh() sur 401).
 */
export async function rawRequest<T>(
  path: string,
  options: RequestInit = {},
  accessToken?: string | null,
): Promise<T> {
  const headers = new Headers(options.headers)
  if (options.body && !headers.has('Content-Type')) headers.set('Content-Type', 'application/json')
  if (accessToken) headers.set('Authorization', `Bearer ${accessToken}`)

  const res = await fetch(`${BASE_URL}${path}`, {
    ...options,
    headers,
    credentials: 'include',
  })
  return parseResponse<T>(res)
}

export interface ApiFetchOptions extends RequestInit {
  /** JSON sérialisé automatiquement si fourni. */
  json?: unknown
}

/**
 * Requête authentifiée : injecte le token du store auth, et sur 401 tente
 * un refresh silencieux puis rejoue la requête une fois. La dépendance au
 * store est résolue à l'appel (pas au chargement du module) pour éviter le
 * cycle d'import statique avec stores/auth.ts.
 */
export async function apiFetch<T>(path: string, options: ApiFetchOptions = {}): Promise<T> {
  const { useAuthStore } = await import('@/stores/auth')
  const auth = useAuthStore()

  const { json, ...rest } = options
  const body = json !== undefined ? JSON.stringify(json) : rest.body

  try {
    return await rawRequest<T>(path, { ...rest, body }, auth.accessToken)
  } catch (err) {
    if (err instanceof ApiError && err.status === 401) {
      try {
        await auth.refresh()
      } catch {
        auth.logout()
        throw err
      }
      return await rawRequest<T>(path, { ...rest, body }, auth.accessToken)
    }
    throw err
  }
}
