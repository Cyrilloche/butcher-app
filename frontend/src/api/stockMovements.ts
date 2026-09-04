import { apiFetch } from './http'
import type { StockMovementDto } from './types'

export function listStockMovements(
  filters: { stockUnitId?: number; customerId?: number } = {},
): Promise<StockMovementDto[]> {
  const params = new URLSearchParams()
  if (filters.stockUnitId != null) params.set('stockUnitId', String(filters.stockUnitId))
  if (filters.customerId != null) params.set('customerId', String(filters.customerId))
  const query = params.toString()
  return apiFetch<StockMovementDto[]>(`/api/stock-movements${query ? `?${query}` : ''}`)
}
