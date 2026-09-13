import type { CustomerDto, SaleDto } from '@/api/types'

export interface CustomerPurchaseStats {
  saleCount: number
  /** Somme des montants saisis, au centime. */
  total: number
  /** Montant des ventes non payées, au centime. */
  pendingTotal: number
  /** Horodatage de la vente la plus récente, `null` sans vente. */
  lastSaleDate: string | null
}

export const NO_PURCHASES: CustomerPurchaseStats = { saleCount: 0, total: 0, pendingTotal: 0, lastSaleDate: null }

const cents = (value: number) => Math.round(value * 100) / 100

/**
 * Achats de chaque client, tirés de la liste des ventes (tableau des clients sur écran large). Les
 * montants sont ceux qui ont été saisis, jamais recalculés.
 */
export function customerPurchaseStats(sales: SaleDto[]): Map<number, CustomerPurchaseStats> {
  const stats = new Map<number, CustomerPurchaseStats>()
  for (const sale of sales) {
    const current = stats.get(sale.customerId) ?? { ...NO_PURCHASES }
    stats.set(sale.customerId, {
      saleCount: current.saleCount + 1,
      total: cents(current.total + sale.total),
      pendingTotal: cents(current.pendingTotal + (sale.paid ? 0 : sale.total)),
      lastSaleDate:
        current.lastSaleDate === null || new Date(sale.date) > new Date(current.lastSaleDate)
          ? sale.date
          : current.lastSaleDate,
    })
  }
  return stats
}

export function customerFullName(customer: CustomerDto): string {
  return customer.firstName ? `${customer.firstName} ${customer.lastName}` : customer.lastName
}

function stripDiacritics(value: string): string {
  return value.normalize('NFD').replace(/[̀-ͯ]/g, '')
}

export function customerInitials(customer: CustomerDto): string {
  const parts = [customer.firstName, customer.lastName].filter((p): p is string => !!p)
  return parts
    .map((p) => stripDiacritics(p)[0])
    .join('')
    .toUpperCase()
}

/** Clé de tri alphabétique : sur le nom de famille (cohérent avec un annuaire). */
export function customerSortKey(customer: CustomerDto): string {
  return stripDiacritics(customer.lastName).toUpperCase()
}

export interface CustomerGroup {
  letter: string
  customers: CustomerDto[]
}

/** Groupe une liste de clients déjà triée par première lettre du nom de famille. */
export function groupCustomersByLetter(customers: CustomerDto[]): CustomerGroup[] {
  const sorted = [...customers].sort((a, b) => customerSortKey(a).localeCompare(customerSortKey(b)))
  const groups: CustomerGroup[] = []
  for (const customer of sorted) {
    const letter = customerSortKey(customer)[0] ?? '?'
    const last = groups[groups.length - 1]
    if (last?.letter === letter) last.customers.push(customer)
    else groups.push({ letter, customers: [customer] })
  }
  return groups
}
