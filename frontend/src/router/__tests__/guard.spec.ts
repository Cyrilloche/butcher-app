import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { defineComponent } from 'vue'
import type { MeDto } from '@/api/types'
import type * as AuthApi from '@/api/auth'

const Blank = defineComponent({ render: () => null })

// Les écrans eux-mêmes n'importent pas ici : seule la garde de navigation est éprouvée.
vi.mock('@/views/StockView.vue', () => ({ default: Blank }))
vi.mock('@/views/LoginView.vue', () => ({ default: Blank }))
vi.mock('@/views/SalesView.vue', () => ({ default: Blank }))
vi.mock('@/views/ReportsView.vue', () => ({ default: Blank }))
vi.mock('@/api/auth', () => ({
  login: vi.fn<typeof AuthApi.login>(),
  refresh: vi.fn<typeof AuthApi.refresh>(),
  logout: vi.fn<typeof AuthApi.logout>(),
  me: vi.fn<typeof AuthApi.me>(),
}))

const authApi = vi.mocked(await import('@/api/auth'))

const session = { accessToken: 'jeton', expiresAtUtc: '2026-09-14T12:00:00Z' }
const admin: MeDto = { id: 'a', email: 'a@saloir.local', displayName: 'Admin', role: 'admin' }
const user: MeDto = { id: 'u', email: 'u@saloir.local', displayName: 'Mamie', role: 'user' }

/** Routeur neuf à chaque test : il garde sinon la position et la session du test précédent. */
async function freshRouter(account: MeDto | null) {
  vi.resetModules()
  const pinia = await import('pinia')
  pinia.setActivePinia(pinia.createPinia())
  if (account) {
    authApi.refresh.mockResolvedValue(session)
    authApi.me.mockResolvedValue(account)
  } else {
    authApi.refresh.mockRejectedValue(new Error('pas de cookie'))
  }
  const router = (await import('../index')).default
  return router
}

beforeEach(() => {
  setActivePinia(createPinia())
  vi.resetAllMocks()
})

describe('garde de navigation', () => {
  it('renvoie un visiteur non connecté vers la connexion, en gardant sa destination', async () => {
    const router = await freshRouter(null)

    await router.push('/sales?paid=false')

    expect(router.currentRoute.value.name).toBe('login')
    expect(router.currentRoute.value.query.redirect).toBe('/sales?paid=false')
  })

  it('laisse un utilisateur connecté aller sur les écrans communs', async () => {
    const router = await freshRouter(user)

    await router.push('/sales')

    expect(router.currentRoute.value.name).toBe('sales')
  })

  it("renvoie un utilisateur vers le stock depuis un écran réservé à l'administrateur", async () => {
    const router = await freshRouter(user)

    const landed: Record<string, unknown> = {}
    for (const path of ['/reports', '/journal', '/accounts', '/overview']) {
      await router.push(path)
      landed[path] = router.currentRoute.value.name
    }

    expect(landed).toEqual({ '/reports': 'stock', '/journal': 'stock', '/accounts': 'stock', '/overview': 'stock' })
  })

  it("ouvre les écrans réservés à l'administrateur", async () => {
    const router = await freshRouter(admin)

    await router.push('/reports')

    expect(router.currentRoute.value.name).toBe('reports')
  })

  it("détourne un compte déjà connecté de l'écran de connexion", async () => {
    const router = await freshRouter(user)

    await router.push('/login')

    expect(router.currentRoute.value.name).toBe('stock')
  })
})
