import type { SaleDto } from '@/api/types'

/**
 * Chiffres de la vue d'ensemble (backoffice PC, `design/backoffice/Backoffice Overview.dc.html`).
 *
 * Fonctions pures sur la liste des ventes, sans appel réseau : faciles à éprouver, et remplaçables par
 * les rapports serveur quand ils existeront (US5). Deux conventions, partagées avec `SalesView` pour
 * que les écrans annoncent les mêmes chiffres :
 * - une vente appartient au mois et à l'année de sa date **locale** ;
 * - le chiffre d'affaires additionne le total de toutes les ventes, payées ou non. Ces totaux sont les
 *   montants réellement saisis, jamais recalculés (RG-05).
 */

/** Arrondi au centime : la somme de nombres flottants ne doit pas afficher « 12,300000000001 €». */
function roundToCents(value: number): number {
  return Math.round(value * 100) / 100
}

function sumTotals(sales: SaleDto[]): number {
  return roundToCents(sales.reduce((sum, sale) => sum + sale.total, 0))
}

function yearOf(sale: SaleDto): number {
  return new Date(sale.date).getFullYear()
}

function monthOf(sale: SaleDto): number {
  return new Date(sale.date).getMonth()
}

/** Années pour lesquelles il existe au moins une vente, plus l'année en cours, de la plus récente à la plus ancienne. */
export function availableYears(sales: SaleDto[], today: Date): number[] {
  const years = new Set(sales.map(yearOf))
  years.add(today.getFullYear())
  return [...years].sort((a, b) => b - a)
}

export interface YearSummary {
  revenue: number
  count: number
  /** `null` quand l'année ne compte aucune vente : un panier moyen n'a alors pas de sens. */
  averageBasket: number | null
}

export function yearSummary(sales: SaleDto[], year: number): YearSummary {
  const yearSales = sales.filter((sale) => yearOf(sale) === year)
  const revenue = sumTotals(yearSales)
  return {
    revenue,
    count: yearSales.length,
    averageBasket: yearSales.length > 0 ? roundToCents(revenue / yearSales.length) : null,
  }
}

/** Chiffre d'affaires de chaque mois de l'année, de janvier (index 0) à décembre (index 11). */
export function monthlyRevenue(sales: SaleDto[], year: number): number[] {
  const months = Array.from({ length: 12 }, () => 0)
  for (const sale of sales) {
    if (yearOf(sale) === year) months[monthOf(sale)]! += sale.total
  }
  return months.map(roundToCents)
}

/** Index du mois au plus fort chiffre d'affaires, ou `null` si aucun mois n'a de vente. */
export function bestMonthIndex(monthly: number[]): number | null {
  const best = Math.max(...monthly)
  return best > 0 ? monthly.indexOf(best) : null
}

export interface MonthComparison {
  current: number
  previous: number
  /**
   * Évolution en pourcentage, arrondie à l'unité. `null` quand le mois précédent est sans vente : une
   * hausse « infinie » n'apprend rien à l'exploitant.
   */
  changePercent: number | null
  /** Date au premier jour du mois précédent, pour en afficher le nom. */
  previousMonth: Date
}

export function monthComparison(sales: SaleDto[], today: Date): MonthComparison {
  const previousMonth = new Date(today.getFullYear(), today.getMonth() - 1, 1)
  const revenueOf = (year: number, month: number) =>
    sumTotals(sales.filter((sale) => yearOf(sale) === year && monthOf(sale) === month))

  const current = revenueOf(today.getFullYear(), today.getMonth())
  const previous = revenueOf(previousMonth.getFullYear(), previousMonth.getMonth())

  return {
    current,
    previous,
    changePercent: previous > 0 ? Math.round(((current - previous) / previous) * 100) : null,
    previousMonth,
  }
}

export interface Receivables {
  total: number
  count: number
}

/** Ventes marquées « À payer », toutes dates confondues. */
export function receivables(sales: SaleDto[]): Receivables {
  const unpaid = sales.filter((sale) => !sale.paid)
  return { total: sumTotals(unpaid), count: unpaid.length }
}

/** Les ventes les plus récentes d'abord ; à date égale, la dernière enregistrée d'abord. */
export function recentSales(sales: SaleDto[], limit: number): SaleDto[] {
  return [...sales]
    .sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime() || b.id - a.id)
    .slice(0, limit)
}
