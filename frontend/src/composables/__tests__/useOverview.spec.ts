import { describe, expect, it } from 'vitest'
import type { SaleDto } from '@/api/types'
import {
  availableYears,
  bestMonthIndex,
  monthComparison,
  monthlyRevenue,
  receivables,
  recentSales,
  yearSummary,
} from '../useOverview'

let nextId = 1

/** Vente minimale ; la date est locale (midi) pour que le mois ne dépende pas du fuseau de la machine. */
function sale(date: string, total: number, paid = true): SaleDto {
  const id = nextId++
  return {
    id,
    saleNumber: `V-${id}`,
    customerId: 1,
    customerName: 'Jean Dupont',
    date: new Date(`${date}T12:00:00`).toISOString(),
    paid,
    notes: null,
    total,
    itemCount: 1,
    createdByName: null,
    lines: [],
  }
}

describe('yearSummary', () => {
  it('additionne les ventes de l’année, payées ou non, et en déduit le panier moyen', () => {
    const sales = [sale('2026-03-01', 10.1), sale('2026-09-04', 20.2, false), sale('2025-12-31', 99)]

    expect(yearSummary(sales, 2026)).toEqual({ revenue: 30.3, count: 2, averageBasket: 15.15 })
  })

  it('arrondit au centime une somme de flottants', () => {
    const sales = [sale('2026-01-01', 0.1), sale('2026-01-02', 0.2)]

    expect(yearSummary(sales, 2026).revenue).toBe(0.3)
  })

  it('ne donne pas de panier moyen pour une année sans vente', () => {
    expect(yearSummary([sale('2025-06-01', 12)], 2026)).toEqual({ revenue: 0, count: 0, averageBasket: null })
  })
})

describe('availableYears', () => {
  it('liste les années des ventes et l’année en cours, de la plus récente à la plus ancienne', () => {
    const sales = [sale('2024-05-01', 1), sale('2025-05-01', 1), sale('2025-06-01', 1)]

    expect(availableYears(sales, new Date(2026, 8, 13))).toEqual([2026, 2025, 2024])
  })
})

describe('monthlyRevenue et bestMonthIndex', () => {
  it('répartit le chiffre d’affaires sur les douze mois et désigne le meilleur', () => {
    const sales = [sale('2026-02-10', 5), sale('2026-02-20', 7), sale('2026-09-04', 11), sale('2025-02-01', 100)]

    const monthly = monthlyRevenue(sales, 2026)

    expect(monthly).toHaveLength(12)
    expect(monthly[1]).toBe(12)
    expect(monthly[8]).toBe(11)
    expect(bestMonthIndex(monthly)).toBe(1)
  })

  it('ne désigne aucun meilleur mois sur une année vide', () => {
    expect(bestMonthIndex(monthlyRevenue([], 2026))).toBeNull()
  })
})

describe('monthComparison', () => {
  const today = new Date(2026, 8, 13) // 13 septembre 2026

  it('compare le mois en cours au précédent, en pourcentage arrondi', () => {
    const sales = [sale('2026-08-10', 40), sale('2026-09-02', 30), sale('2026-09-10', 20)]

    const comparison = monthComparison(sales, today)

    expect(comparison.current).toBe(50)
    expect(comparison.previous).toBe(40)
    expect(comparison.changePercent).toBe(25)
    expect(comparison.previousMonth.getMonth()).toBe(7)
  })

  it('franchit l’année : en janvier, le mois précédent est décembre', () => {
    const sales = [sale('2025-12-15', 10), sale('2026-01-05', 5)]

    const comparison = monthComparison(sales, new Date(2026, 0, 20))

    expect(comparison.previous).toBe(10)
    expect(comparison.changePercent).toBe(-50)
    expect(comparison.previousMonth.getFullYear()).toBe(2025)
  })

  it('ne donne pas d’évolution quand le mois précédent est sans vente', () => {
    expect(monthComparison([sale('2026-09-02', 30)], today).changePercent).toBeNull()
  })
})

describe('receivables', () => {
  it('additionne les seules ventes à payer', () => {
    const sales = [sale('2026-09-01', 12.5, false), sale('2026-09-02', 7, true), sale('2025-01-01', 3.25, false)]

    expect(receivables(sales)).toEqual({ total: 15.75, count: 2 })
  })
})

describe('recentSales', () => {
  it('trie de la plus récente à la plus ancienne et limite le nombre', () => {
    const old = sale('2026-01-01', 1)
    const recent = sale('2026-09-10', 1)
    const middle = sale('2026-05-01', 1)

    expect(recentSales([old, recent, middle], 2).map((s) => s.id)).toEqual([recent.id, middle.id])
  })

  it('à date égale, place la dernière vente enregistrée en premier', () => {
    const first = sale('2026-09-10', 1)
    const second = sale('2026-09-10', 1)

    expect(recentSales([first, second], 2).map((s) => s.id)).toEqual([second.id, first.id])
  })
})
