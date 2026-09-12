import type { SaleDto } from '@/api/types'

/**
 * Filtres et tri de la liste des ventes sur écran large (FR-017, FR-018 ; clôt l'écart E-06).
 *
 * Fonctions pures sur la liste déjà chargée : le volume d'une activité artisanale le permet, et les
 * filtres réagissent sans nouvel appel au serveur. Une vente appartient à la date **locale** de son
 * horodatage, comme dans `SalesView` et `useOverview`.
 */

export type PaymentFilter = 'all' | 'paid' | 'pending'

export interface SalesFilters {
  customerId: number | null
  payment: PaymentFilter
  /** Date locale `YYYY-MM-DD`, incluse ; vide = sans borne. */
  from: string
  /** Date locale `YYYY-MM-DD`, incluse ; vide = sans borne. */
  to: string
}

export type SalesSortKey = 'date' | 'customer' | 'status' | 'total'
export type SortDirection = 'asc' | 'desc'

export interface SalesSort {
  key: SalesSortKey
  direction: SortDirection
}

export function emptySalesFilters(): SalesFilters {
  return { customerId: null, payment: 'all', from: '', to: '' }
}

export function hasActiveFilters(filters: SalesFilters): boolean {
  return filters.customerId !== null || filters.payment !== 'all' || filters.from !== '' || filters.to !== ''
}

/** Date locale d'un horodatage, au format `YYYY-MM-DD` d'un champ `<input type="date">`. */
export function localDateKey(iso: string): string {
  const date = new Date(iso)
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${date.getFullYear()}-${month}-${day}`
}

export function filterSales(sales: SaleDto[], filters: SalesFilters): SaleDto[] {
  return sales.filter((sale) => {
    if (filters.customerId !== null && sale.customerId !== filters.customerId) return false
    if (filters.payment === 'paid' && !sale.paid) return false
    if (filters.payment === 'pending' && sale.paid) return false

    // Les clés `YYYY-MM-DD` se comparent dans l'ordre chronologique.
    const day = localDateKey(sale.date)
    if (filters.from !== '' && day < filters.from) return false
    if (filters.to !== '' && day > filters.to) return false
    return true
  })
}

function compareBy(key: SalesSortKey, a: SaleDto, b: SaleDto): number {
  switch (key) {
    case 'date':
      return new Date(a.date).getTime() - new Date(b.date).getTime()
    case 'customer':
      return a.customerName.localeCompare(b.customerName, 'fr', { sensitivity: 'base' })
    case 'status':
      // Ordre croissant : les ventes à payer d'abord, ce qu'on cherche en premier.
      return Number(a.paid) - Number(b.paid)
    case 'total':
      return a.total - b.total
  }
}

/**
 * Trie sans modifier la liste reçue. À égalité sur la clé choisie, la vente la plus récente passe en
 * premier, puis la dernière enregistrée : l'ordre reste stable d'un tri à l'autre.
 */
export function sortSales(sales: SaleDto[], sort: SalesSort): SaleDto[] {
  const sign = sort.direction === 'asc' ? 1 : -1
  return [...sales].sort(
    (a, b) =>
      sign * compareBy(sort.key, a, b) ||
      new Date(b.date).getTime() - new Date(a.date).getTime() ||
      b.id - a.id,
  )
}

/** Somme des montants saisis, arrondie au centime. */
export function salesTotal(sales: SaleDto[]): number {
  return Math.round(sales.reduce((sum, sale) => sum + sale.total, 0) * 100) / 100
}
