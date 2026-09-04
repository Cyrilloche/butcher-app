import { apiFetch } from './http'
import type { AddStockUnitsRequest, StockUnitDto, StockUnitStatus } from './types'

export function listStockUnits(filters: { batchId?: number; status?: StockUnitStatus } = {}): Promise<
  StockUnitDto[]
> {
  const params = new URLSearchParams()
  if (filters.batchId != null) params.set('batchId', String(filters.batchId))
  if (filters.status) params.set('status', filters.status)
  const query = params.toString()
  return apiFetch<StockUnitDto[]>(`/api/stock-units${query ? `?${query}` : ''}`)
}

export function getStockUnit(id: number): Promise<StockUnitDto> {
  return apiFetch<StockUnitDto>(`/api/stock-units/${id}`)
}

export function addStockUnits(
  batchId: number,
  payload: AddStockUnitsRequest,
): Promise<StockUnitDto[]> {
  return apiFetch<StockUnitDto[]>(`/api/production-batches/${batchId}/stock-units`, {
    method: 'POST',
    json: payload,
  })
}

export function deleteStockUnit(id: number): Promise<void> {
  return apiFetch<void>(`/api/stock-units/${id}`, { method: 'DELETE' })
}
