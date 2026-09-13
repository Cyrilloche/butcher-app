import type { ReceivablesDto, SaleDto, SalesSummaryDto } from '@/api/types'
import { toDateKey, type ReportPeriod } from '@/composables/useReportPeriod'

/**
 * Chiffres de la vue d'ensemble (backoffice PC, `design/backoffice/Backoffice Overview.dc.html`).
 *
 * Les montants viennent des rapports du serveur (US5) : l'écran Rapports et la vue d'ensemble annoncent
 * donc les mêmes chiffres, calculés sur les montants saisis (FR-030) et les mois de Paris. La liste des
 * ventes ne sert plus qu'aux années disponibles et aux ventes récentes. Fonctions pures, testées.
 */

/** Arrondi au centime : une division ne doit pas afficher « 12,300000000001 € ». */
function roundToCents(value: number): number {
  return Math.round(value * 100) / 100
}

function yearOf(sale: SaleDto): number {
  return new Date(sale.date).getFullYear()
}

/** Années pour lesquelles il existe au moins une vente, plus l'année en cours, de la plus récente à la plus ancienne. */
export function availableYears(sales: SaleDto[], today: Date): number[] {
  const years = new Set(sales.map(yearOf))
  years.add(today.getFullYear())
  return [...years].sort((a, b) => b - a)
}

export function yearPeriod(year: number): ReportPeriod {
  return { from: `${year}-01-01`, to: `${year}-12-31` }
}

export interface YearSummary {
  revenue: number
  count: number
  /** `null` quand l'année ne compte aucune vente : un panier moyen n'a alors pas de sens. */
  averageBasket: number | null
}

/** Chiffre d'affaires de l'année : toutes les ventes, payées ou non. */
export function yearSummary(summary: SalesSummaryDto): YearSummary {
  return {
    revenue: summary.total,
    count: summary.saleCount,
    averageBasket: summary.saleCount > 0 ? roundToCents(summary.total / summary.saleCount) : null,
  }
}

/** Chiffre d'affaires de chaque mois de l'année, de janvier (index 0) à décembre (index 11). */
export function monthlyRevenue(summary: SalesSummaryDto, year: number): number[] {
  const months = Array.from({ length: 12 }, () => 0)
  for (const { month, total } of summary.months) {
    const [monthYear, monthNumber] = month.split('-').map(Number)
    if (monthYear === year && monthNumber! >= 1 && monthNumber! <= 12) months[monthNumber! - 1] = total
  }
  return months
}

/** Index du mois au plus fort chiffre d'affaires, ou `null` si aucun mois n'a de vente. */
export function bestMonthIndex(monthly: number[]): number | null {
  const best = Math.max(...monthly)
  return best > 0 ? monthly.indexOf(best) : null
}

/** Du premier jour du mois précédent au dernier jour du mois en cours : ce que la comparaison demande. */
export function comparisonPeriod(today: Date): ReportPeriod {
  return {
    from: toDateKey(new Date(today.getFullYear(), today.getMonth() - 1, 1)),
    to: toDateKey(new Date(today.getFullYear(), today.getMonth() + 1, 0)),
  }
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

/** Compare le mois en cours au précédent, à partir de la synthèse de `comparisonPeriod`. */
export function monthComparison(summary: SalesSummaryDto, today: Date): MonthComparison {
  const previousMonth = new Date(today.getFullYear(), today.getMonth() - 1, 1)
  const totalOf = (date: Date) => summary.months.find((m) => m.month === toDateKey(date).slice(0, 7))?.total ?? 0

  const current = totalOf(today)
  const previous = totalOf(previousMonth)

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
export function receivables(report: ReceivablesDto): Receivables {
  return {
    total: report.total,
    count: report.customers.reduce((count, customer) => count + customer.sales.length, 0),
  }
}

/** Les ventes les plus récentes d'abord ; à date égale, la dernière enregistrée d'abord. */
export function recentSales(sales: SaleDto[], limit: number): SaleDto[] {
  return [...sales]
    .sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime() || b.id - a.id)
    .slice(0, limit)
}
