import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { buttonByText, inDocument, mountWithVuetify, settle } from '@/__tests__/helpers'
import { ApiError } from '@/api/http'
import type { StockMovementDto } from '@/api/types'
import type * as MovementsApi from '@/api/stockMovements'
import SaleLineEditDialog from '../domain/SaleLineEditDialog.vue'

vi.mock('@/api/stockMovements', () => ({
  updateStockMovement: vi.fn<typeof MovementsApi.updateStockMovement>(),
  deleteStockMovement: vi.fn<typeof MovementsApi.deleteStockMovement>(),
}))

const { updateStockMovement, deleteStockMovement } = vi.mocked(await import('@/api/stockMovements'))

/** Le « Retirer » de la confirmation, et non celui du formulaire. */
function confirmRemoval() {
  const buttons = inDocument('.sale-line-edit__dialog button').filter((b) => b.text() === 'Retirer')
  return buttons[buttons.length - 1]!
}

const tranche: StockMovementDto = {
  id: 42,
  stockUnitId: 2,
  type: 'sale',
  date: '2026-09-10T10:00:00Z',
  soldWeight: 0.4,
  amount: 10,
  customerId: 3,
  customerName: 'Jean Dupont',
  saleId: 7,
  saleNumber: 'V-260910-1',
  productName: 'Jambon',
  productIsActive: true,
  unitNumber: 'JB-260901-1',
  notes: 'bien fumé',
  createdByName: 'Mamie',
}

/** La fenêtre se remplit à l'ouverture : on la monte fermée, puis on l'ouvre. */
async function openDialog(line: StockMovementDto) {
  const wrapper = mountWithVuetify(SaleLineEditDialog, { props: { modelValue: false, line } })
  await wrapper.setProps({ modelValue: true })
  await settle()
  return wrapper
}

function inputs() {
  return inDocument('.sale-line-edit__dialog input')
}

beforeEach(() => {
  vi.resetAllMocks()
})

afterEach(() => {
  document.body.innerHTML = ''
})

describe('SaleLineEditDialog', () => {
  it('pré-remplit le poids en grammes et le montant saisi', async () => {
    await openDialog(tranche)

    expect(inputs().map((i) => (i.element as HTMLInputElement).value)).toEqual(['400', '10'])
  })

  it('corrige le poids sans recalculer le montant, et renvoie la note intacte', async () => {
    updateStockMovement.mockResolvedValue(tranche)
    const wrapper = await openDialog(tranche)

    await inputs()[0]!.setValue('350')
    await buttonByText('Enregistrer').trigger('click')
    await settle()

    expect(updateStockMovement).toHaveBeenCalledWith(42, { soldWeight: 0.35, amount: 10, notes: 'bien fumé' })
    expect(wrapper.emitted('saved')).toHaveLength(1)
    expect(wrapper.emitted('update:modelValue')).toEqual([[false]])
  })

  it('ne propose pas de poids sur une ligne vendue à la pièce', async () => {
    updateStockMovement.mockResolvedValue(tranche)
    await openDialog({ ...tranche, soldWeight: null, amount: 6, notes: null })

    expect(inputs()).toHaveLength(1)
    await inputs()[0]!.setValue('5.5')
    await buttonByText('Enregistrer').trigger('click')
    await settle()

    expect(updateStockMovement).toHaveBeenCalledWith(42, { soldWeight: undefined, amount: 5.5, notes: undefined })
  })

  it('demande confirmation avant de retirer la ligne', async () => {
    deleteStockMovement.mockResolvedValue(undefined)
    const wrapper = await openDialog(tranche)

    await buttonByText('Retirer').trigger('click')
    expect(document.body.textContent).toContain('Retirer cette ligne ?')
    expect(deleteStockMovement).not.toHaveBeenCalled()

    await confirmRemoval().trigger('click')
    await settle()

    expect(deleteStockMovement).toHaveBeenCalledWith(42)
    expect(wrapper.emitted('removed')).toHaveLength(1)
  })

  it('revient au formulaire avec la phrase du serveur quand le retrait est refusé', async () => {
    deleteStockMovement.mockRejectedValue(new ApiError(409, 'C’est la dernière ligne : supprimez la vente.'))
    const wrapper = await openDialog(tranche)

    await buttonByText('Retirer').trigger('click')
    await confirmRemoval().trigger('click')
    await settle()

    expect(inDocument('.sale-line-edit__error')[0]!.text()).toBe('C’est la dernière ligne : supprimez la vente.')
    expect(document.body.textContent).not.toContain('Retirer cette ligne ?')
    expect(wrapper.emitted('removed')).toBeUndefined()
  })
})
