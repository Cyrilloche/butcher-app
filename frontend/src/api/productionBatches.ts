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

/**
 * Supprime un lot et les unités de stock qu'il a générées. Refusé par le serveur (409) dès qu'une
 * unité du lot est déjà sortie du stock. Le numéro du lot n'est pas libéré : il ne sera jamais
 * réattribué.
 */
export function deleteProductionBatch(id: number): Promise<void> {
  return apiFetch<void>(`/api/production-batches/${id}`, { method: 'DELETE' })
}
