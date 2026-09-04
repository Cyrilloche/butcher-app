import { apiFetch } from './http'
import type { CreateUnitOfMeasureRequest, UnitOfMeasureDto } from './types'

export function listUnitsOfMeasure(includeInactive = false): Promise<UnitOfMeasureDto[]> {
  return apiFetch<UnitOfMeasureDto[]>(`/api/units-of-measure?includeInactive=${includeInactive}`)
}

export function createUnitOfMeasure(payload: CreateUnitOfMeasureRequest): Promise<UnitOfMeasureDto> {
  return apiFetch<UnitOfMeasureDto>('/api/units-of-measure', { method: 'POST', json: payload })
}
