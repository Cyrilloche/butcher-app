import { describe, expect, it } from 'vitest'
import type { SaleDto } from '@/api/types'
import {
  emptySalesFilters,
  filterSales,
  hasActiveFilters,
  localDateKey,
  salesTotal,
  sortSales,
} from '../useSalesFilters'

let nextId = 1

/** Vente minimale à midi, heure locale, pour que la date ne dépende pas du fuseau de la machine. */
function sale(overrides: { date: string; total?: number; paid?: boolean; customerId?: number; customerName?: string }): SaleDto {
  const id = nextId++
  return {
    id,
    saleNumber: `V-${id}`,
    customerId: overrides.customerId ?? 1,
    customerName: overrides.customerName ?? 'Jean Dupont',
    date: new Date(`${overrides.date}T12:00:00`).toISOString(),
    paid: overrides.paid ?? true,
    notes: null,
    total: overrides.total ?? 10,
    itemCount: 1,
    createdByName: null,
    lines: [],
  }
}

const ids = (sales: SaleDto[]) => sales.map((s) => s.id)

describe('filterSales', () => {
  const marie = sale({ date: '2026-09-01', customerId: 2, customerName: 'Marie Perrin', paid: false })
  const jean = sale({ date: '2026-09-05', customerId: 1, paid: true })
  const jeanAugust = sale({ date: '2026-08-31', customerId: 1, paid: false })
  const all = [marie, jean, jeanAugust]

  it('sans filtre, garde toutes les ventes', () => {
    expect(filterSales(all, emptySalesFilters())).toHaveLength(3)
  })

  it('filtre par client', () => {
    expect(ids(filterSales(all, { ...emptySalesFilters(), customerId: 1 }))).toEqual([jean.id, jeanAugust.id])
  })

  it('filtre les ventes à payer, puis les ventes payées', () => {
    expect(ids(filterSales(all, { ...emptySalesFilters(), payment: 'pending' }))).toEqual([marie.id, jeanAugust.id])
    expect(ids(filterSales(all, { ...emptySalesFilters(), payment: 'paid' }))).toEqual([jean.id])
  })

  it('filtre par période, bornes incluses', () => {
    const september = { ...emptySalesFilters(), from: '2026-09-01', to: '2026-09-05' }

    expect(ids(filterSales(all, september))).toEqual([marie.id, jean.id])
  })

  it('accepte une période ouverte d’un côté', () => {
    expect(ids(filterSales(all, { ...emptySalesFilters(), to: '2026-08-31' }))).toEqual([jeanAugust.id])
  })

  it('combine les filtres', () => {
    const filters = { customerId: 1, payment: 'pending' as const, from: '2026-08-01', to: '' }

    expect(ids(filterSales(all, filters))).toEqual([jeanAugust.id])
  })
})

describe('hasActiveFilters', () => {
  it('ne signale aucun filtre sur des filtres vides, et signale le moindre critère', () => {
    expect(hasActiveFilters(emptySalesFilters())).toBe(false)
    expect(hasActiveFilters({ ...emptySalesFilters(), payment: 'paid' })).toBe(true)
  })
})

describe('localDateKey', () => {
  it('donne la date locale au format des champs de date', () => {
    expect(localDateKey(new Date(2026, 0, 5, 23, 30).toISOString())).toBe('2026-01-05')
  })
})

describe('sortSales', () => {
  const a = sale({ date: '2026-09-01', total: 30, customerName: 'Émile Zola', paid: true })
  const b = sale({ date: '2026-09-03', total: 10, customerName: 'anne Martin', paid: false })
  const c = sale({ date: '2026-09-02', total: 20, customerName: 'Bernard Aubry', paid: true })

  it('trie par date, dans les deux sens', () => {
    expect(ids(sortSales([a, b, c], { key: 'date', direction: 'desc' }))).toEqual([b.id, c.id, a.id])
    expect(ids(sortSales([a, b, c], { key: 'date', direction: 'asc' }))).toEqual([a.id, c.id, b.id])
  })

  it('trie par client sans tenir compte des accents ni des majuscules', () => {
    expect(ids(sortSales([a, b, c], { key: 'customer', direction: 'asc' }))).toEqual([b.id, c.id, a.id])
  })

  it('trie par montant', () => {
    expect(ids(sortSales([a, b, c], { key: 'total', direction: 'desc' }))).toEqual([a.id, c.id, b.id])
  })

  it('met les ventes à payer d’abord en ordre croissant, et départage par la plus récente', () => {
    expect(ids(sortSales([a, b, c], { key: 'status', direction: 'asc' }))).toEqual([b.id, c.id, a.id])
  })

  it('ne modifie pas la liste reçue', () => {
    const original = [a, b, c]
    sortSales(original, { key: 'total', direction: 'asc' })

    expect(ids(original)).toEqual([a.id, b.id, c.id])
  })
})

describe('salesTotal', () => {
  it('additionne les montants au centime près', () => {
    expect(salesTotal([sale({ date: '2026-09-01', total: 0.1 }), sale({ date: '2026-09-01', total: 0.2 })])).toBe(0.3)
  })
})
