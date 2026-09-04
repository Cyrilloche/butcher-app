import type { CustomerDto } from '@/api/types'

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
