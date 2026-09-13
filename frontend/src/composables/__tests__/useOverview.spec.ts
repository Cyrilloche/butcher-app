import { describe, expect, it } from 'vitest'
import type { SaleDto, SalesSummaryDto } from '@/api/types'
import {
  availableYears,
  bestMonthIndex,
  comparisonPeriod,
  monthComparison,
  monthlyRevenue,
  receivables,
  recentSales,
  yearPeriod,
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

/** Synthèse telle que le serveur la renvoie : mois portant des ventes seulement. */
function summary(months: [string, number][]): SalesSummaryDto {
  const total = months.reduce((sum, [, value]) => sum + value, 0)
  return {
    saleCount: months.length,
    total,
    paidTotal: total,
    pendingTotal: 0,
    months: months.map(([month, value]) => ({ month, saleCount: 1, total: value, paidTotal: value, pendingTotal: 0 })),
  }
}

describe('yearSummary', () => {
  it('reprend le total du serveur et en déduit le panier moyen, au centime', () => {
    expect(yearSummary({ ...summary([]), saleCount: 3, total: 10 })).toEqual({ revenue: 10, count: 3, averageBasket: 3.33 })
  })

  it('ne donne pas de panier moyen pour une année sans vente', () => {
    expect(yearSummary(summary([]))).toEqual({ revenue: 0, count: 0, averageBasket: null })
  })

  it('demande l’année entière', () => {
    expect(yearPeriod(2026)).toEqual({ from: '2026-01-01', to: '2026-12-31' })
  })
})

describe('availableYears', () => {
  it('liste les années des ventes et l’année en cours, de la plus récente à la plus ancienne', () => {
    const sales = [sale('2024-05-01', 1), sale('2025-05-01', 1), sale('2025-06-01', 1)]

    expect(availableYears(sales, new Date(2026, 8, 13))).toEqual([2026, 2025, 2024])
  })
})

describe('monthlyRevenue et bestMonthIndex', () => {
  it('place chaque mois du serveur à sa place parmi les douze et désigne le meilleur', () => {
    const monthly = monthlyRevenue(summary([['2026-02', 12], ['2026-09', 11]]), 2026)

    expect(monthly).toHaveLength(12)
    expect(monthly[1]).toBe(12)
    expect(monthly[8]).toBe(11)
    expect(monthly[0]).toBe(0)
    expect(bestMonthIndex(monthly)).toBe(1)
  })

  it('ignore un mois d’une autre année et ne désigne aucun meilleur mois sur une année vide', () => {
    expect(bestMonthIndex(monthlyRevenue(summary([['2025-02', 100]]), 2026))).toBeNull()
  })
})

describe('monthComparison', () => {
  const today = new Date(2026, 8, 13) // 13 septembre 2026

  it('demande le mois précédent et le mois en cours', () => {
    expect(comparisonPeriod(today)).toEqual({ from: '2026-08-01', to: '2026-09-30' })
  })

  it('compare le mois en cours au précédent, en pourcentage arrondi', () => {
    const comparison = monthComparison(summary([['2026-08', 40], ['2026-09', 50]]), today)

    expect(comparison.current).toBe(50)
    expect(comparison.previous).toBe(40)
    expect(comparison.changePercent).toBe(25)
    expect(comparison.previousMonth.getMonth()).toBe(7)
  })

  it('franchit l’année : en janvier, le mois précédent est décembre', () => {
    const january = new Date(2026, 0, 20)

    const comparison = monthComparison(summary([['2025-12', 10], ['2026-01', 5]]), january)

    expect(comparisonPeriod(january)).toEqual({ from: '2025-12-01', to: '2026-01-31' })
    expect(comparison.previous).toBe(10)
    expect(comparison.changePercent).toBe(-50)
    expect(comparison.previousMonth.getFullYear()).toBe(2025)
  })

  it('ne donne pas d’évolution quand le mois précédent est sans vente', () => {
    expect(monthComparison(summary([['2026-09', 30]]), today).changePercent).toBeNull()
  })
})

describe('receivables', () => {
  it('reprend le montant dû et compte les ventes en attente', () => {
    const report = {
      total: 15.75,
      customers: [
        { customerId: 1, customerName: 'Jean Dupont', pendingTotal: 12.5, oldestUnpaidDate: '2026-09-01T10:00:00Z', sales: [{ id: 1, saleNumber: 'V-1', date: '2026-09-01T10:00:00Z', total: 12.5 }] },
        { customerId: 2, customerName: 'Marie Perrin', pendingTotal: 3.25, oldestUnpaidDate: '2025-01-01T10:00:00Z', sales: [{ id: 2, saleNumber: 'V-2', date: '2025-01-01T10:00:00Z', total: 1 }, { id: 3, saleNumber: 'V-3', date: '2025-02-01T10:00:00Z', total: 2.25 }] },
      ],
    }

    expect(receivables(report)).toEqual({ total: 15.75, count: 3 })
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
