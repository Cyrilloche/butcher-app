import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { defineComponent, h } from 'vue'
import { buttonByText, inDocument, mountWithVuetify, settle } from '@/__tests__/helpers'
import { ApiError } from '@/api/http'
import type { SellableLot } from '@/composables/useSales'
import type * as SalesApi from '@/api/sales'
import type * as UseSales from '@/composables/useSales'
import SaleAddView from '../SaleAddView.vue'

vi.mock('vue-router', () => ({ useRouter: () => ({ push: vi.fn<() => void>(), replace: vi.fn<() => void>() }) }))
vi.mock('@/api/sales', () => ({ createSale: vi.fn<typeof SalesApi.createSale>() }))
vi.mock('@/composables/useSales', () => ({ listSellableLots: vi.fn<typeof UseSales.listSellableLots>() }))

const { createSale } = vi.mocked(await import('@/api/sales'))
const { listSellableLots } = vi.mocked(await import('@/composables/useSales'))

/** Le choix du client a sa propre recherche : un bouton suffit à en donner un. */
const CustomerPickerStub = defineComponent({
  props: { modelValue: { type: Number, default: null } },
  emits: ['update:modelValue'],
  setup: (_, { emit }) => () => h('button', { class: 'pick-customer', onClick: () => emit('update:modelValue', 3) }, 'Choisir'),
})

const saucisson: SellableLot = {
  stockUnitId: 1,
  productId: 1,
  productName: 'Saucisson',
  productCode: 'SC',
  label: 'SC-260910-1',
  detail: '1,24 kg · 18,50 € / kg',
  status: 'available',
  price: 22.88,
  weight: 1.237,
  remainingWeight: 1.237,
  pricePerKg: 18.5,
  allowPartialSale: false,
}
const jambon: SellableLot = {
  stockUnitId: 2,
  productId: 2,
  productName: 'Jambon',
  productCode: 'JB',
  label: 'JB-260901-1',
  detail: '5,4 kg · 25,00 € / kg',
  status: 'available',
  price: 135,
  weight: 5.4,
  remainingWeight: 5.4,
  pricePerKg: 25,
  allowPartialSale: true,
}
const jambonEntame: SellableLot = { ...jambon, stockUnitId: 3, label: 'JB-260820-1', status: 'opened', remainingWeight: 2.15 }

async function mountView() {
  const wrapper = mountWithVuetify(SaleAddView, {
    props: { dialog: true },
    global: { stubs: { CustomerPicker: CustomerPickerStub } },
  })
  await settle()
  return wrapper
}

async function search(wrapper: ReturnType<typeof mountWithVuetify>, text: string) {
  await wrapper.find('.sellable-lot-search__input').setValue(text)
}

function results(wrapper: ReturnType<typeof mountWithVuetify>) {
  return wrapper.findAll('.sellable-lot-search__unit')
}

/** Champ du poids de la tranche : l'identifiant est posé sur l'enveloppe du champ Vuetify. */
function sliceInput(wrapper: ReturnType<typeof mountWithVuetify>) {
  return wrapper.find('.sale-add-view__pending-weight input')
}

function saveButton() {
  return buttonByText('Enregistrer la vente')
}

beforeEach(() => {
  vi.resetAllMocks()
  listSellableLots.mockResolvedValue([saucisson, jambon, jambonEntame])
})

afterEach(() => {
  document.body.innerHTML = ''
})

describe('SaleAddView', () => {
  it('garde la recherche après un choix, et la rend après une décision sur un jambon', async () => {
    const wrapper = await mountView()

    await search(wrapper, 'JB-2609')
    await results(wrapper)[0]!.trigger('click')
    expect(wrapper.find('.sellable-lot-search').isVisible()).toBe(false)

    await buttonByText('Vendre en entier').trigger('click')

    expect(wrapper.find('.sellable-lot-search').isVisible()).toBe(true)
    expect((wrapper.find('.sellable-lot-search__input').element as HTMLInputElement).value).toBe('JB-2609')
  })

  it('enregistre une vente en entier au prix pré-calculé, et ne propose plus l’unité déjà au panier', async () => {
    createSale.mockResolvedValue({} as never)
    const wrapper = await mountView()

    expect(saveButton().attributes('disabled')).toBeDefined()

    await wrapper.find('.pick-customer').trigger('click')
    await search(wrapper, 'saucisson')
    await results(wrapper)[0]!.trigger('click')

    expect(wrapper.find('.sale-add-view__cart').text()).toContain('22,88 €')
    await search(wrapper, 'saucisson')
    expect(results(wrapper)).toHaveLength(0)

    expect(saveButton().text()).toContain('22,88 €')
    await saveButton().trigger('click')
    await settle()

    expect(createSale).toHaveBeenCalledWith({
      customerId: 3,
      paid: true,
      lines: [{ stockUnitId: 1, isFullSale: true, soldWeight: 1.237, amount: 22.88 }],
    })
    expect(wrapper.emitted('saved')).toHaveLength(1)
  })

  it('ne permet pas d’enregistrer sans client, même avec un panier', async () => {
    const wrapper = await mountView()

    await search(wrapper, 'saucisson')
    await results(wrapper)[0]!.trigger('click')

    expect(saveButton().attributes('disabled')).toBeDefined()
  })

  it('demande en entier ou à la tranche pour un produit qui l’autorise, et chiffre la tranche', async () => {
    createSale.mockResolvedValue({} as never)
    const wrapper = await mountView()
    await wrapper.find('.pick-customer').trigger('click')

    await search(wrapper, 'JB-260901')
    await results(wrapper)[0]!.trigger('click')
    expect(buttonByText('Vendre en entier').text()).toContain('135,00 €')

    await buttonByText('Vendre une tranche').trigger('click')
    await sliceInput(wrapper).setValue('400')
    expect(wrapper.find('.sale-add-view__pending-amount').text()).toBe('10,00 €')

    await buttonByText('Ajouter').trigger('click')
    expect(wrapper.find('.sale-add-view__cart').text()).toContain('tranche, 400 g')

    await buttonByText('À payer').trigger('click')
    await saveButton().trigger('click')
    await settle()

    expect(createSale).toHaveBeenCalledWith({
      customerId: 3,
      paid: false,
      lines: [{ stockUnitId: 2, isFullSale: false, soldWeight: 0.4, amount: 10 }],
    })
  })

  it('va droit à la tranche sur une unité entamée, et refuse une tranche plus lourde que le restant', async () => {
    const wrapper = await mountView()

    await search(wrapper, 'JB-260820')
    await results(wrapper)[0]!.trigger('click')

    expect(wrapper.text()).not.toContain('Vendre en entier')
    expect(wrapper.text()).toContain('Poids restant : 2,15 kg')

    await sliceInput(wrapper).setValue('2200')

    expect(wrapper.text()).toContain('Ce poids dépasse le poids restant estimé sur cette unité.')
    expect(buttonByText('Ajouter').attributes('disabled')).toBeDefined()

    await sliceInput(wrapper).setValue('2150')
    expect(buttonByText('Ajouter').attributes('disabled')).toBeUndefined()
  })

  it('affiche le refus du serveur tel quel', async () => {
    createSale.mockRejectedValue(new ApiError(409, "L'unité SC-260910-1 n'est plus en stock."))
    const wrapper = await mountView()
    await wrapper.find('.pick-customer').trigger('click')
    await search(wrapper, 'saucisson')
    await results(wrapper)[0]!.trigger('click')

    await saveButton().trigger('click')
    await settle()

    expect(inDocument('.app-form-shell__error')[0]!.text()).toBe("L'unité SC-260910-1 n'est plus en stock.")
    expect(wrapper.emitted('saved')).toBeUndefined()
  })
})
