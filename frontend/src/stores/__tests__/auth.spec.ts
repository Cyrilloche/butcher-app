import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useAuthStore } from '../auth'
import { ApiError } from '@/api/http'
import type { MeDto } from '@/api/types'
import type * as AuthApi from '@/api/auth'

vi.mock('@/api/auth', () => ({
  login: vi.fn<typeof AuthApi.login>(),
  refresh: vi.fn<typeof AuthApi.refresh>(),
  logout: vi.fn<typeof AuthApi.logout>(),
  me: vi.fn<typeof AuthApi.me>(),
}))

const authApi = vi.mocked(await import('@/api/auth'))

const session = { accessToken: 'jeton', expiresAtUtc: '2026-09-14T12:00:00Z' }
const admin: MeDto = { id: 'a', email: 'admin@saloir.local', displayName: 'Admin', role: 'admin' }
const user: MeDto = { id: 'u', email: 'mamie@saloir.local', displayName: 'Mamie', role: 'user' }

beforeEach(() => {
  setActivePinia(createPinia())
  vi.resetAllMocks()
})

describe('login', () => {
  it('ouvre la session et charge le compte connecté', async () => {
    authApi.login.mockResolvedValue(session)
    authApi.me.mockResolvedValue(admin)
    const auth = useAuthStore()

    await auth.login('admin@saloir.local', 'phrase de passe')

    expect(authApi.login).toHaveBeenCalledWith({ email: 'admin@saloir.local', password: 'phrase de passe' })
    expect(auth.isAuthenticated).toBe(true)
    expect(auth.account).toEqual(admin)
    expect(auth.isAdmin).toBe(true)
  })

  it("ne reconnaît pas l'administrateur à un simple utilisateur", async () => {
    authApi.login.mockResolvedValue(session)
    authApi.me.mockResolvedValue(user)
    const auth = useAuthStore()

    await auth.login('mamie@saloir.local', 'phrase de passe')

    expect(auth.isAdmin).toBe(false)
  })

  it('ne garde pas une session dont le compte ne se charge pas', async () => {
    authApi.login.mockResolvedValue(session)
    authApi.me.mockRejectedValue(new ApiError(401, 'Compte désactivé'))
    const auth = useAuthStore()

    await expect(auth.login('a', 'b')).rejects.toThrow('Compte désactivé')

    expect(auth.isAuthenticated).toBe(false)
    expect(auth.account).toBeNull()
  })

  it('laisse remonter un refus de connexion sans ouvrir de session', async () => {
    authApi.login.mockRejectedValue(new ApiError(429, 'Trop de tentatives.'))
    const auth = useAuthStore()

    await expect(auth.login('a', 'b')).rejects.toThrow('Trop de tentatives.')

    expect(auth.isAuthenticated).toBe(false)
    expect(authApi.me).not.toHaveBeenCalled()
  })
})

describe('ensureReady', () => {
  it('retrouve la session par le cookie, une seule fois même appelée plusieurs fois', async () => {
    authApi.refresh.mockResolvedValue(session)
    authApi.me.mockResolvedValue(user)
    const auth = useAuthStore()

    await Promise.all([auth.ensureReady(), auth.ensureReady()])
    await auth.ensureReady()

    expect(authApi.refresh).toHaveBeenCalledTimes(1)
    expect(auth.isAuthenticated).toBe(true)
    expect(auth.account).toEqual(user)
  })

  it("ne lève rien pour un visiteur sans cookie : il reste simplement déconnecté", async () => {
    authApi.refresh.mockRejectedValue(new ApiError(401, 'Unauthorized'))
    const auth = useAuthStore()

    await expect(auth.ensureReady()).resolves.toBeUndefined()

    expect(auth.isAuthenticated).toBe(false)
  })
})

describe('logout', () => {
  it('efface la session locale même si le serveur est injoignable, et autorise une nouvelle reprise', async () => {
    authApi.refresh.mockResolvedValue(session)
    authApi.me.mockResolvedValue(admin)
    authApi.logout.mockRejectedValue(new TypeError('Failed to fetch'))
    const auth = useAuthStore()
    await auth.ensureReady()

    await auth.logout()

    expect(auth.isAuthenticated).toBe(false)
    expect(auth.account).toBeNull()

    await auth.ensureReady()
    expect(authApi.refresh).toHaveBeenCalledTimes(2)
  })
})
