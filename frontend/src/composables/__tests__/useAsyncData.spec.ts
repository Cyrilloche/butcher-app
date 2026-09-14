import { describe, expect, it, vi } from 'vitest'
import { flushPromises } from '@vue/test-utils'
import { ApiError } from '@/api/http'
import { useAsyncData } from '../useAsyncData'
import { apiErrorMessage } from '../useApiError'

describe('useAsyncData', () => {
  it('charge dès l’appel, puis expose la donnée', async () => {
    const { data, loading, error } = useAsyncData(() => Promise.resolve([1, 2]), [] as number[])

    expect(loading.value).toBe(true)
    expect(data.value).toEqual([])

    await flushPromises()

    expect(loading.value).toBe(false)
    expect(data.value).toEqual([1, 2])
    expect(error.value).toBeNull()
  })

  it('affiche le message du serveur, ou un message générique pour une panne réseau', async () => {
    const fromServer = useAsyncData(() => Promise.reject(new ApiError(403, 'Geste réservé.')), null)
    const offline = useAsyncData(() => Promise.reject(new TypeError('Failed to fetch')), null)

    await flushPromises()

    expect(fromServer.error.value).toBe('Geste réservé.')
    expect(offline.error.value).toBe('Erreur de chargement.')
    expect(offline.loading.value).toBe(false)
  })

  it('efface l’erreur précédente au rechargement', async () => {
    const loader = vi.fn<() => Promise<string>>().mockRejectedValueOnce(new ApiError(500, 'Panne')).mockResolvedValueOnce('ok')
    const { data, error, reload } = useAsyncData(loader, '')
    await flushPromises()
    expect(error.value).toBe('Panne')

    await reload()

    expect(error.value).toBeNull()
    expect(data.value).toBe('ok')
  })
})

describe('apiErrorMessage', () => {
  it('reprend le message du serveur tel quel, sinon le repli', () => {
    expect(apiErrorMessage(new ApiError(409, 'Dernière ligne de la vente.'), 'repli')).toBe('Dernière ligne de la vente.')
    expect(apiErrorMessage(new Error('technique'), 'repli')).toBe('repli')
  })
})
