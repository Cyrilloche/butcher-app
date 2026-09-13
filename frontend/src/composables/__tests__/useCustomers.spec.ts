import { describe, expect, it } from 'vitest'
import type { SaleDto } from '@/api/types'
import { customerPurchaseStats } from '../useCustomers'

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
