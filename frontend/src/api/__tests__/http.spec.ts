import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { ApiError, apiFetch, rawRequest } from '../http'
import { useAuthStore } from '@/stores/auth'
import type * as AuthApi from '@/api/auth'

vi.mock('@/api/auth', () => ({
  login: vi.fn<typeof AuthApi.login>(),
  refresh: vi.fn<typeof AuthApi.refresh>(),
  logout: vi.fn<typeof AuthApi.logout>(),
  me: vi.fn<typeof AuthApi.me>(),
}))

const authApi = await import('@/api/auth')

const fetchMock = vi.fn<typeof fetch>()

function json(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } })
}

/** En-têtes et options du n-ième appel à fetch. */
function call(index: number) {
  const [url, init] = fetchMock.mock.calls[index]!
  return { url: String(url), init: init!, headers: new Headers(init!.headers) }
}

beforeEach(() => {
  setActivePinia(createPinia())
  fetchMock.mockReset()
  vi.stubGlobal('fetch', fetchMock)
  vi.mocked(authApi.refresh).mockReset()
  vi.mocked(authApi.logout).mockReset().mockResolvedValue(undefined)
})

describe('rawRequest', () => {
  it('envoie le cookie, le jeton et le type JSON quand il y a un corps', async () => {
    fetchMock.mockResolvedValue(json({ ok: true }))

    const result = await rawRequest('/api/x', { method: 'POST', body: '{}' }, 'jeton')

    expect(result).toEqual({ ok: true })
    const { url, init, headers } = call(0)
    expect(url).toContain('/api/x')
    expect(init.credentials).toBe('include')
    expect(headers.get('Authorization')).toBe('Bearer jeton')
    expect(headers.get('Content-Type')).toBe('application/json')
  })

  it("n'ajoute ni jeton ni type sans jeton ni corps", async () => {
    fetchMock.mockResolvedValue(json([]))

    await rawRequest('/api/x')

    const { headers } = call(0)
    expect(headers.has('Authorization')).toBe(false)
    expect(headers.has('Content-Type')).toBe(false)
  })

  it('rend undefined sur un 204', async () => {
    fetchMock.mockResolvedValue(new Response(null, { status: 204 }))

    await expect(rawRequest('/api/x', { method: 'DELETE' })).resolves.toBeUndefined()
  })

  it('reprend le message métier du serveur, en français, tel quel', async () => {
    fetchMock.mockResolvedValue(json({ status: 409, title: 'Conflict', detail: 'Ce client a des ventes.' }, 409))

    const error = await rawRequest('/api/x').catch((err: unknown) => err)

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ status: 409, message: 'Ce client a des ventes.' })
  })

  it('garde le détail des erreurs de validation', async () => {
    const errors = { Amount: ['Le montant doit être positif.'] }
    fetchMock.mockResolvedValue(json({ status: 400, title: 'Validation', errors }, 400))

    await expect(rawRequest('/api/x')).rejects.toMatchObject({ status: 400, message: 'Validation', errors })
  })

  it('retombe sur le statut HTTP sans corps JSON', async () => {
    fetchMock.mockResolvedValue(new Response('', { status: 502, statusText: 'Bad Gateway' }))

    await expect(rawRequest('/api/x')).rejects.toMatchObject({ status: 502, message: 'Bad Gateway' })
  })
})

describe('apiFetch', () => {
  it('sérialise le corps JSON et pose le jeton de la session', async () => {
    useAuthStore().accessToken = 'jeton'
    fetchMock.mockResolvedValue(json({ id: 1 }))

    await apiFetch('/api/sales', { method: 'POST', json: { customerId: 3 } })

    const { init, headers } = call(0)
    expect(init.body).toBe('{"customerId":3}')
    expect(headers.get('Authorization')).toBe('Bearer jeton')
  })

  it('sur un 401, rafraîchit le jeton puis rejoue la requête une fois avec le nouveau', async () => {
    const auth = useAuthStore()
    auth.accessToken = 'périmé'
    vi.mocked(authApi.refresh).mockResolvedValue({ accessToken: 'neuf', expiresAtUtc: '2026-09-14T12:00:00Z' })
    fetchMock.mockResolvedValueOnce(new Response(null, { status: 401 })).mockResolvedValueOnce(json({ id: 1 }))

    await expect(apiFetch('/api/sales/1')).resolves.toEqual({ id: 1 })

    expect(fetchMock).toHaveBeenCalledTimes(2)
    expect(call(1).headers.get('Authorization')).toBe('Bearer neuf')
  })

  it('ferme la session et rend le 401 quand le rafraîchissement échoue', async () => {
    const auth = useAuthStore()
    auth.accessToken = 'périmé'
    vi.mocked(authApi.refresh).mockRejectedValue(new ApiError(401, 'Session expirée'))
    fetchMock.mockResolvedValue(new Response(null, { status: 401, statusText: 'Unauthorized' }))

    await expect(apiFetch('/api/sales/1')).rejects.toMatchObject({ status: 401 })

    expect(fetchMock).toHaveBeenCalledTimes(1)
    expect(auth.isAuthenticated).toBe(false)
    expect(authApi.logout).toHaveBeenCalled()
  })

  it('ne rafraîchit pas sur une autre erreur, un 403 par exemple', async () => {
    fetchMock.mockResolvedValue(json({ status: 403, title: 'Forbidden', detail: 'Geste réservé.' }, 403))

    await expect(apiFetch('/api/accounts')).rejects.toMatchObject({ status: 403, message: 'Geste réservé.' })

    expect(authApi.refresh).not.toHaveBeenCalled()
  })
})
