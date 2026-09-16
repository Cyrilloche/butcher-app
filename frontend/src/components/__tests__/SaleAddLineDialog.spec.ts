import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { buttonByText, inDocument, mountWithVuetify, settle } from '@/__tests__/helpers'
import { ApiError } from '@/api/http'
import type { SaleDto } from '@/api/types'
import type { SellableLot } from '@/composables/useSales'
import type * as MovementsApi from '@/api/stockMovements'
import type * as UseSales from '@/composables/useSales'
import SaleAddLineDialog from '../domain/SaleAddLineDialog.vue'

vi.mock('@/api/stockMovements', () => ({ createStockMovement: vi.fn<typeof MovementsApi.createStockMovement>() }))
vi.mock('@/composables/useSales', () => ({ listSellableLots: vi.fn<typeof UseSales.listSellableLots>() }))

const { createStockMovement } = vi.mocked(await import('@/api/stockMovements'))
const { listSellableLots } = vi.mocked(await import('@/composables/useSales'))

const sale: SaleDto = {
  id: 7,
  saleNumber: 'V-260916-1',
  customerId: 3,
  customerName: 'Jean Dupont',
  date: '2026-09-16T10:00:00Z',
  paid: true,
  notes: null,
  total: 22.88,
  itemCount: 1,
  createdByName: 'Mamie',
  lines: [],
}

const saucisson: SellableLot = {
  stockUnitId: 1,
  productId: 1,
  productName: 'Saucisson',
  productCode: 'SC',
  label: 'SC-260910-2',
  detail: '1,24 kg · 18,50 € / kg',
  status: 'available',
  price: 22.88,
  weight: 1.237,
  remainingWeight: 1.237,
  pricePerKg: 18.5,
  allowPartialSale: false,
}
const jambonEntame: SellableLot = {
  ...saucisson,
  stockUnitId: 3,
  productId: 2,
  productName: 'Jambon',
  productCode: 'JB',
  label: 'JB-260820-1',
  status: 'opened',
  price: 135,
  weight: 5.4,
  remainingWeight: 2.15,
  pricePerKg: 25,
  allowPartialSale: true,
}

/** La fenêtre relit le stock à l'ouverture : on la monte fermée, puis on l'ouvre. */
async function openDialog(forSale: SaleDto = sale) {
  const wrapper = mountWithVuetify(SaleAddLineDialog, { props: { modelValue: false, sale: forSale } })
  await wrapper.setProps({ modelValue: true })
  await settle()
  return wrapper
}

async function search(text: string) {
  await inDocument('.sellable-lot-search__input')[0]!.setValue(text)
}

const units = () => inDocument('.sellable-lot-search__unit')

beforeEach(() => {
  vi.resetAllMocks()
  listSellableLots.mockResolvedValue([saucisson, jambonEntame])
})

afterEach(() => {
  document.body.innerHTML = ''
})

describe('SaleAddLineDialog', () => {
  it('ajoute aussitôt à la vente l’unité choisie, au prix pré-calculé, puis se ferme', async () => {
    createStockMovement.mockResolvedValue({} as never)
    const wrapper = await openDialog()

    await search('saucisson')
    await units()[0]!.trigger('click')
    await settle()

    expect(createStockMovement).toHaveBeenCalledWith(1, {
      type: 'sale',
      saleId: 7,
      isFullSale: true,
      soldWeight: 1.237,
      amount: 22.88,
    })
    expect(wrapper.emitted('added')).toHaveLength(1)
    expect(wrapper.emitted('update:modelValue')).toEqual([[false]])
  })

  it('ajoute une tranche d’un jambon entamé', async () => {
    createStockMovement.mockResolvedValue({} as never)
    await openDialog()

    await search('jambon')
    await units()[0]!.trigger('click')
    await inDocument('.sale-line-chooser__pending-weight input')[0]!.setValue('400')
    await buttonByText('Ajouter').trigger('click')
    await settle()

    expect(createStockMovement).toHaveBeenCalledWith(3, {
      type: 'sale',
      saleId: 7,
      isFullSale: false,
      soldWeight: 0.4,
      amount: 10,
    })
  })

  it('affiche le refus du serveur et reste ouverte', async () => {
    createStockMovement.mockRejectedValue(new ApiError(409, "L'unité SC-260910-2 n'est plus en stock."))
    const wrapper = await openDialog()

    await search('saucisson')
    await units()[0]!.trigger('click')
    await settle()

    expect(inDocument('.sale-add-line__error')[0]!.text()).toBe("L'unité SC-260910-2 n'est plus en stock.")
    expect(wrapper.emitted('added')).toBeUndefined()
  })

  it('prévient qu’une vente payée le reste après un complément', async () => {
    await openDialog()
    expect(inDocument('.sale-add-line__hint')).toHaveLength(1)

    document.body.innerHTML = ''
    await openDialog({ ...sale, paid: false })
    expect(inDocument('.sale-add-line__hint')).toHaveLength(0)
  })
})
