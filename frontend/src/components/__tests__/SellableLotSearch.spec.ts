import { afterEach, describe, expect, it } from 'vitest'
import { mountWithVuetify } from '@/__tests__/helpers'
import type { SellableLot } from '@/composables/useSales'
import SellableLotSearch from '../domain/SellableLotSearch.vue'

function lot(stockUnitId: number, productId: number, productName: string, productCode: string, n: number, overrides: Partial<SellableLot> = {}): SellableLot {
  return {
    stockUnitId,
    productId,
    productName,
    productCode,
    label: `${productCode}-260910-${n}`,
    detail: '500 g · 16,00 € / kg',
    status: 'available',
    price: 8,
    weight: 0.5,
    remainingWeight: 0.5,
    pricePerKg: 16,
    allowPartialSale: false,
    ...overrides,
  }
}

// Le cas de la recette du 2026-09-16 (RU-02) : la première chipo avait plus de 8 sachets, les autres étaient invisibles.
const chipoNature = Array.from({ length: 10 }, (_, i) => lot(i + 1, 1, 'Chipolata nature', 'CN', i + 1))
const chipoHerbes = [lot(11, 2, 'Chipolata aux herbes', 'CH', 1), lot(12, 2, 'Chipolata aux herbes', 'CH', 2, { status: 'opened' })]
const saucisson = [lot(13, 3, 'Saucisson', 'SC', 1)]
const allLots = [...chipoHerbes, ...chipoNature, ...saucisson]

function mountSearch(excludedIds = new Set<number>()) {
  return mountWithVuetify(SellableLotSearch, { props: { lots: allLots, excludedIds, loading: false } })
}

type Wrapper = ReturnType<typeof mountSearch>

async function search(wrapper: Wrapper, text: string) {
  await wrapper.find('.sellable-lot-search__input').setValue(text)
}

const products = (wrapper: Wrapper) => wrapper.findAll('.sellable-lot-search__product')
const units = (wrapper: Wrapper) => wrapper.findAll('.sellable-lot-search__unit')

afterEach(() => {
  document.body.innerHTML = ''
})

describe('SellableLotSearch', () => {
  it('ne cherche qu’à partir de deux caractères', async () => {
    const wrapper = mountSearch()

    await search(wrapper, 'c')

    expect(products(wrapper)).toHaveLength(0)
    expect(units(wrapper)).toHaveLength(0)
  })

  it('propose tous les produits qui correspondent, avec leur stock, puis toutes les unités du produit choisi', async () => {
    const wrapper = mountSearch()

    await search(wrapper, 'chipo')

    expect(products(wrapper).map((p) => p.text())).toEqual([
      expect.stringContaining('Chipolata aux herbes'),
      expect.stringContaining('Chipolata nature'),
    ])
    expect(products(wrapper)[0]!.text()).toContain('2 en stock, dont 1 entamé')
    expect(products(wrapper)[1]!.text()).toContain('10 en stock')
    expect(units(wrapper)).toHaveLength(0)

    await products(wrapper)[1]!.trigger('click')

    expect(units(wrapper)).toHaveLength(10)
    await wrapper.find('.sellable-lot-search__back').trigger('click')
    expect(products(wrapper)).toHaveLength(2)
  })

  it('montre directement les unités quand un seul produit correspond, l’entamée en tête', async () => {
    const wrapper = mountSearch()

    await search(wrapper, 'herbes')

    expect(products(wrapper)).toHaveLength(0)
    expect(wrapper.find('.sellable-lot-search__back').exists()).toBe(false)
    expect(units(wrapper).map((u) => u.text())).toEqual([
      expect.stringContaining('CH-260910-2'),
      expect.stringContaining('CH-260910-1'),
    ])
  })

  it('reconnaît le code exact d’un produit', async () => {
    const wrapper = mountSearch()

    await search(wrapper, 'sc')

    expect(units(wrapper).map((u) => u.text())).toEqual([expect.stringContaining('SC-260910-1')])
  })

  it('cherche par numéro d’étiquette quand aucun nom ne correspond', async () => {
    const wrapper = mountSearch()

    await search(wrapper, 'cn-260910-1')

    expect(units(wrapper).map((u) => u.text())).toEqual([
      expect.stringContaining('CN-260910-1'),
      expect.stringContaining('CN-260910-10'),
    ])
    expect(units(wrapper)[0]!.text()).toContain('Chipolata nature')
  })

  it('ne propose plus les unités au panier, ni un produit dont tout le stock y est', async () => {
    const wrapper = mountSearch(new Set([11, 12, 1]))

    await search(wrapper, 'chipo')

    expect(products(wrapper)).toHaveLength(0)
    expect(units(wrapper)).toHaveLength(9)
  })

  it('émet l’unité choisie', async () => {
    const wrapper = mountSearch()

    await search(wrapper, 'saucisson')
    await units(wrapper)[0]!.trigger('click')

    expect(wrapper.emitted('pick')).toEqual([[saucisson[0]]])
  })

  it('le dit quand rien ne correspond', async () => {
    const wrapper = mountSearch()

    await search(wrapper, 'zz')

    expect(wrapper.text()).toContain('Aucun produit en stock ne correspond.')
  })
})
