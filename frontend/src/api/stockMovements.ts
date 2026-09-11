import { apiFetch } from './http'
import type {
  CreateStockMovementRequest,
  StockMovementDto,
  UpdateStockMovementRequest,
} from './types'

export function listStockMovements(
  filters: { stockUnitId?: number; customerId?: number; saleId?: number } = {},
): Promise<StockMovementDto[]> {
  const params = new URLSearchParams()
  if (filters.stockUnitId != null) params.set('stockUnitId', String(filters.stockUnitId))
  if (filters.customerId != null) params.set('customerId', String(filters.customerId))
  if (filters.saleId != null) params.set('saleId', String(filters.saleId))
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

/**
 * Correction d'une ligne : remplacement complet des trois champs corrigeables. L'appelant renvoie
 * donc aussi ceux qu'il n'a pas modifiés. Le serveur reste le garant des règles (poids vendu dans
 * la limite du poids pesé, montant positif) — l'écran affiche son refus, il ne l'anticipe pas.
 */
export function updateStockMovement(
  id: number,
  payload: UpdateStockMovementRequest,
): Promise<StockMovementDto> {
  return apiFetch<StockMovementDto>(`/api/stock-movements/${id}`, { method: 'PUT', json: payload })
}

/**
 * Retrait d'une ligne (RG-11). Le serveur refuse la dernière ligne d'une vente — c'est alors la
 * vente qu'il faut supprimer — et rend `disponible` toute unité qui ne porte plus aucun mouvement.
 */
export function deleteStockMovement(id: number): Promise<void> {
  return apiFetch<void>(`/api/stock-movements/${id}`, { method: 'DELETE' })
}
