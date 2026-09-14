import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { buttonByText, inDocument, mountWithVuetify, settle } from '@/__tests__/helpers'
import { ApiError } from '@/api/http'
import type { StockDetailUnit } from '@/composables/useStock'
import type * as MovementsApi from '@/api/stockMovements'
import type * as UnitsApi from '@/api/stockUnits'
import StockUnitOutcomeMenu from '../domain/StockUnitOutcomeMenu.vue'

vi.mock('@/api/stockMovements', () => ({ createStockMovement: vi.fn<typeof MovementsApi.createStockMovement>() }))
vi.mock('@/api/stockUnits', () => ({ closeStockUnit: vi.fn<typeof UnitsApi.closeStockUnit>() }))

const { createStockMovement } = vi.mocked(await import('@/api/stockMovements'))
const { closeStockUnit } = vi.mocked(await import('@/api/stockUnits'))

const intact: StockDetailUnit = {
  id: 5,
  number: 'SC-260910-1',
  weightKg: 1.2,
  remainingKg: 1.2,
  weightLabel: '1,2 kg',
  remainingLabel: '1,2 kg',
  isEmptied: false,
  status: 'available',
}
const entame: StockDetailUnit = { ...intact, id: 6, number: 'JB-260901-1', weightKg: 5.4, remainingKg: 2.15, status: 'opened' }

async function openMenu(unit: StockDetailUnit) {
  const wrapper = mountWithVuetify(StockUnitOutcomeMenu, { props: { unit } })
  await wrapper.find('.stock-unit-outcome__trigger').trigger('click')
  await settle()
  return wrapper
}

async function choose(wrapper: ReturnType<typeof mountWithVuetify>, item: string) {
  await buttonByText(item).trigger('click')
  await settle()
}

function confirmButton(label: string) {
  const button = inDocument('.stock-unit-outcome__actions button').find((b) => b.text() === label)
  if (!button) throw new Error(`Pas de bouton « ${label} »`)
  return button
}

beforeEach(() => {
  vi.resetAllMocks()
})

afterEach(() => {
  document.body.innerHTML = ''
})

describe('StockUnitOutcomeMenu', () => {
  it('ne propose la clôture que pour une unité entamée', async () => {
    await openMenu(intact)
    const items = inDocument('.v-list-item').map((i) => i.text())

    expect(items).toEqual(['Usage perso', 'Déclarer une perte'])
  })

  it('annonce le poids restant qui sera enregistré, puis laisse le serveur le calculer', async () => {
    createStockMovement.mockResolvedValue({} as never)
    const wrapper = await openMenu(entame)

    await choose(wrapper, 'Usage perso')
    expect(document.body.textContent).toContain("L'unité JB-260901-1 quittera le stock et sera marquée « Perso ».")
    expect(document.body.textContent).toContain('Poids qui sera enregistré : 2,15 kg')

    await confirmButton('Marquer en perso').trigger('click')
    await settle()

    expect(createStockMovement).toHaveBeenCalledWith(6, { type: 'personal' })
    expect(wrapper.emitted('done')).toHaveLength(1)
  })

  it('déclare une perte', async () => {
    createStockMovement.mockResolvedValue({} as never)
    const wrapper = await openMenu(intact)

    await choose(wrapper, 'Déclarer une perte')
    await confirmButton('Déclarer la perte').trigger('click')
    await settle()

    expect(createStockMovement).toHaveBeenCalledWith(5, { type: 'loss' })
    expect(wrapper.emitted('done')).toHaveLength(1)
  })

  it('refuse une sortie sur une unité entamée dont il ne reste rien, et renvoie vers la clôture', async () => {
    const wrapper = await openMenu({ ...entame, remainingKg: 0, isEmptied: true })

    await choose(wrapper, 'Déclarer une perte')

    expect(document.body.textContent).toContain("il n'y a plus rien à sortir. Clôture-la plutôt.")
    expect(confirmButton('Déclarer la perte').attributes('disabled')).toBeDefined()
  })

  it('clôture une unité entamée', async () => {
    closeStockUnit.mockResolvedValue(undefined)
    const wrapper = await openMenu(entame)

    await choose(wrapper, 'Clôturer (vendue)')
    await confirmButton('Clôturer').trigger('click')
    await settle()

    expect(closeStockUnit).toHaveBeenCalledWith(6)
    expect(createStockMovement).not.toHaveBeenCalled()
    expect(wrapper.emitted('done')).toHaveLength(1)
  })

  it('remonte le refus du serveur', async () => {
    closeStockUnit.mockRejectedValue(new ApiError(409, "L'unité n'est plus entamée."))
    const wrapper = await openMenu(entame)

    await choose(wrapper, 'Clôturer (vendue)')
    await confirmButton('Clôturer').trigger('click')
    await settle()

    expect(wrapper.emitted('failed')).toEqual([["L'unité n'est plus entamée."]])
    expect(wrapper.emitted('done')).toBeUndefined()
  })
})
