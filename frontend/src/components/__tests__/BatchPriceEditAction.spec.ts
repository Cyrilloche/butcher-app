import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { buttonByText, inDocument, mountWithVuetify, settle } from '@/__tests__/helpers'
import { ApiError } from '@/api/http'
import type { StockDetailBatch } from '@/composables/useStock'
import type * as BatchesApi from '@/api/productionBatches'
import BatchPriceEditAction from '../domain/BatchPriceEditAction.vue'

vi.mock('@/api/productionBatches', () => ({ updateProductionBatch: vi.fn<typeof BatchesApi.updateProductionBatch>() }))

const { updateProductionBatch } = vi.mocked(await import('@/api/productionBatches'))

const fournee: StockDetailBatch = {
  id: 12,
  dayRankLabel: '2ᵉ fournée',
  dateLabel: '10 septembre 2026',
  priceLabel: '18,50 € / kg',
  salePrice: 18.5,
  priceUnit: 'kg',
  untouched: { rawMaterialRef: 'porc — grossiste X', expiryDate: '2026-12-01', notes: 'fumé' },
  createdByName: null,
  units: [],
}

async function openDialog() {
  const wrapper = mountWithVuetify(BatchPriceEditAction, { props: { batch: fournee } })
  await wrapper.find('.batch-price-edit__trigger').trigger('click')
  await settle()
  return wrapper
}

function priceInput() {
  return inDocument('.batch-price-edit__dialog input')[0]!
}

beforeEach(() => {
  vi.resetAllMocks()
})

afterEach(() => {
  document.body.innerHTML = ''
})

describe('BatchPriceEditAction', () => {
  it('nomme la fournée et part du prix actuel', async () => {
    const wrapper = await openDialog()

    expect(wrapper.find('.batch-price-edit__trigger').attributes('aria-label')).toBe(
      'Corriger le prix de la fournée du 10 septembre 2026, 2ᵉ fournée',
    )
    expect((priceInput().element as HTMLInputElement).value).toBe('18.5')
  })

  it('renvoie la DLC, la matière première et les notes telles quelles avec le nouveau prix', async () => {
    updateProductionBatch.mockResolvedValue({} as never)
    const wrapper = await openDialog()

    await priceInput().setValue('21')
    await buttonByText('Enregistrer').trigger('click')
    await settle()

    expect(updateProductionBatch).toHaveBeenCalledWith(12, {
      salePrice: 21,
      rawMaterialRef: 'porc — grossiste X',
      expiryDate: '2026-12-01',
      notes: 'fumé',
    })
    expect(wrapper.emitted('done')).toHaveLength(1)
  })

  it('refuse un prix nul', async () => {
    await openDialog()

    await priceInput().setValue('0')

    expect(buttonByText('Enregistrer').attributes('disabled')).toBeDefined()
  })

  it('affiche le refus du serveur', async () => {
    updateProductionBatch.mockRejectedValue(new ApiError(400, 'Le prix doit être positif.'))
    const wrapper = await openDialog()

    await buttonByText('Enregistrer').trigger('click')
    await settle()

    expect(document.body.textContent).toContain('Le prix doit être positif.')
    expect(wrapper.emitted('done')).toBeUndefined()
  })
})
