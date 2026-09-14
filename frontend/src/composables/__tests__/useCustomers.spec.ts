import { describe, expect, it } from 'vitest'
import type { CustomerDto, SaleDto } from '@/api/types'
import {
  customerFullName,
  customerInitials,
  customerPurchaseStats,
  groupCustomersByLetter,
} from '../useCustomers'

function customer(id: number, lastName: string, firstName: string | null = null): CustomerDto {
  return { id, lastName, firstName, phone: null, notes: null }
}

describe('nom et initiales', () => {
  it('place le prénom avant le nom quand il existe', () => {
    expect(customerFullName(customer(1, 'Dupont', 'Jean'))).toBe('Jean Dupont')
    expect(customerFullName(customer(2, 'Boulangerie'))).toBe('Boulangerie')
  })

  it('tire des initiales majuscules et sans accent', () => {
    expect(customerInitials(customer(1, 'élise', 'Émile'))).toBe('EE')
    expect(customerInitials(customer(2, 'Perrin'))).toBe('P')
  })
})

describe('groupCustomersByLetter', () => {
  it('trie par nom de famille et regroupe sous la lettre, un accent rangé avec sa lettre', () => {
    const groups = groupCustomersByLetter([
      customer(1, 'Martin'),
      customer(2, 'Écuyer'),
      customer(3, 'Durand'),
      customer(4, 'Dupont'),
    ])

    expect(groups.map((g) => [g.letter, g.customers.map((c) => c.lastName)])).toEqual([
      ['D', ['Dupont', 'Durand']],
      ['E', ['Écuyer']],
      ['M', ['Martin']],
    ])
  })
})

let nextId = 1

function sale(overrides: { customerId: number; date: string; total: number; paid?: boolean }): SaleDto {
  const id = nextId++
  return {
    id,
    saleNumber: `V-${id}`,
    customerId: overrides.customerId,
    customerName: 'Client',
    date: new Date(`${overrides.date}T12:00:00`).toISOString(),
    paid: overrides.paid ?? true,
    notes: null,
    total: overrides.total,
    itemCount: 1,
    createdByName: null,
    lines: [],
  }
}

describe('customerPurchaseStats', () => {
  it('compte, additionne au centime et retient la vente la plus récente, par client', () => {
    const latest = sale({ customerId: 1, date: '2026-09-10', total: 0.2, paid: false })
    const stats = customerPurchaseStats([
      sale({ customerId: 1, date: '2026-09-01', total: 0.1 }),
      latest,
      sale({ customerId: 1, date: '2026-08-15', total: 12.5, paid: false }),
      sale({ customerId: 2, date: '2026-09-05', total: 7 }),
    ])

    expect(stats.get(1)).toEqual({ saleCount: 3, total: 12.8, pendingTotal: 12.7, lastSaleDate: latest.date })
    expect(stats.get(2)).toMatchObject({ saleCount: 1, total: 7, pendingTotal: 0 })
  })

  it("n'a pas d'entrée pour un client sans vente", () => {
    expect(customerPurchaseStats([]).size).toBe(0)
  })
})
