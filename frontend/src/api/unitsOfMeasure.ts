import { apiFetch } from './http'
import type { UnitOfMeasureDto } from './types'

export function listUnitsOfMeasure(includeInactive = false): Promise<UnitOfMeasureDto[]> {
  return apiFetch<UnitOfMeasureDto[]>(`/api/units-of-measure?includeInactive=${includeInactive}`)
}
