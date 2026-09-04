import { apiFetch } from './http'
import type {
  CreateProductionBatchRequest,
  ProductionBatchDto,
  UpdateProductionBatchRequest,
} from './types'

export function listProductionBatches(productId?: number): Promise<ProductionBatchDto[]> {
  const query = productId != null ? `?productId=${productId}` : ''
  return apiFetch<ProductionBatchDto[]>(`/api/production-batches${query}`)
}

export function getProductionBatch(id: number): Promise<ProductionBatchDto> {
  return apiFetch<ProductionBatchDto>(`/api/production-batches/${id}`)
}

export function createProductionBatch(
  payload: CreateProductionBatchRequest,
): Promise<ProductionBatchDto> {
  return apiFetch<ProductionBatchDto>('/api/production-batches', { method: 'POST', json: payload })
}

export function updateProductionBatch(
  id: number,
  payload: UpdateProductionBatchRequest,
): Promise<ProductionBatchDto> {
  return apiFetch<ProductionBatchDto>(`/api/production-batches/${id}`, { method: 'PUT', json: payload })
}
