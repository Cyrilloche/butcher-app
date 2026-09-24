import { describe, expect, it } from 'vitest'
import type { SaleDraftDto } from '@/api/types'
import type { SellableLot } from '@/composables/useSales'
import { draftToCart, setAssistantDraft, takeAssistantDraft } from '../useAssistantDraft'

const lot = (overrides: Partial<SellableLot>): SellableLot => ({
  stockUnitId: 1,
  productId: 1,
  productName: 'Saucisson',
  productCode: 'SC',
  label: 'SC-260910-1',
  detail: '',
  status: 'available',
  price: 7.8,
  weight: 0.312,
  remainingWeight: 0.312,
  pricePerKg: 25,
  allowPartialSale: false,
  ...overrides,
})

const draft = (lines: SaleDraftDto['lines'], warnings: string[] = []): SaleDraftDto => ({
  customerId: 4,
  paid: false,
  lines,
  warnings,
})

describe('draftToCart', () => {
  it('prend le prix de l’unité entière, comme le choix à la main', () => {
    const { cart } = draftToCart(draft([{ stockUnitId: 1, isFullSale: true, soldWeight: null }]), [lot({})])

    expect(cart).toEqual([
      { stockUnitId: 1, productName: 'Saucisson', label: 'SC-260910-1', isFullSale: true, weightKg: 0.312, amount: 7.8 },
    ])
  })

  it('chiffre une tranche au prix du kilo du lot', () => {
    const jambon = lot({ stockUnitId: 9, productName: 'Jambon', label: 'JB-260901-1', status: 'opened', pricePerKg: 28 })

    const { cart } = draftToCart(draft([{ stockUnitId: 9, isFullSale: false, soldWeight: 0.25 }]), [jambon])

    expect(cart[0]).toMatchObject({ isFullSale: false, weightKg: 0.25, amount: 7 })
  })

  it('laisse à choisir une tranche sans poids, sans doubler l’avertissement du serveur', () => {
    const { cart, warnings } = draftToCart(
      draft([{ stockUnitId: 1, isFullSale: false, soldWeight: null }], ['Poids de la tranche de jambon à saisir.']),
      [lot({})],
    )

    expect(cart).toEqual([])
    expect(warnings).toEqual(['Poids de la tranche de jambon à saisir.'])
  })

  it('signale une unité qui n’est plus en stock', () => {
    const { cart, warnings } = draftToCart(draft([{ stockUnitId: 42, isFullSale: true, soldWeight: null }]), [lot({})])

    expect(cart).toEqual([])
    expect(warnings).toHaveLength(1)
  })
})

describe('brouillon en attente', () => {
  it('se lit une seule fois', () => {
    setAssistantDraft(draft([]))

    expect(takeAssistantDraft()).not.toBeNull()
    expect(takeAssistantDraft()).toBeNull()
  })
})
