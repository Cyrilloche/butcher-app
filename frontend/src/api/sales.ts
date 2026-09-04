import { apiFetch } from './http'
import type { CreateSaleRequest, SaleDto, SetSalePaymentRequest, UpdateSaleRequest } from './types'

export function listSales(
  filters: { customerId?: number; paid?: boolean; from?: string; to?: string } = {},
): Promise<SaleDto[]> {
  const params = new URLSearchParams()
  if (filters.customerId != null) params.set('customerId', String(filters.customerId))
  if (filters.paid != null) params.set('paid', String(filters.paid))
  if (filters.from) params.set('from', filters.from)
  if (filters.to) params.set('to', filters.to)
  const query = params.toString()
  return apiFetch<SaleDto[]>(`/api/sales${query ? `?${query}` : ''}`)
}

export function getSale(id: number): Promise<SaleDto> {
  return apiFetch<SaleDto>(`/api/sales/${id}`)
}

export function createSale(payload: CreateSaleRequest): Promise<SaleDto> {
  return apiFetch<SaleDto>('/api/sales', { method: 'POST', json: payload })
}

export function updateSale(id: number, payload: UpdateSaleRequest): Promise<SaleDto> {
  return apiFetch<SaleDto>(`/api/sales/${id}`, { method: 'PUT', json: payload })
}

export function setSalePayment(id: number, payload: SetSalePaymentRequest): Promise<SaleDto> {
  return apiFetch<SaleDto>(`/api/sales/${id}/payment`, { method: 'POST', json: payload })
}

export function deleteSale(id: number): Promise<void> {
  return apiFetch<void>(`/api/sales/${id}`, { method: 'DELETE' })
}
