import { apiFetch } from './http'
import type { CreateStockMovementRequest, StockMovementDto } from './types'

export function listStockMovements(
  filters: { stockUnitId?: number; customerId?: number } = {},
): Promise<StockMovementDto[]> {
  const params = new URLSearchParams()
  if (filters.stockUnitId != null) params.set('stockUnitId', String(filters.stockUnitId))
  if (filters.customerId != null) params.set('customerId', String(filters.customerId))
  const query = params.toString()
  return apiFetch<StockMovementDto[]>(`/api/stock-movements${query ? `?${query}` : ''}`)
}

export function getStockMovement(id: number): Promise<StockMovementDto> {
  return apiFetch<StockMovementDto>(`/api/stock-movements/${id}`)
}

export function createStockMovement(
  stockUnitId: number,
  payload: CreateStockMovementRequest,
): Promise<StockMovementDto> {
  return apiFetch<StockMovementDto>(`/api/stock-units/${stockUnitId}/movements`, {
    method: 'POST',
    json: payload,
  })
}
