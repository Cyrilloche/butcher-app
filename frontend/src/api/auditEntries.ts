import { apiFetch } from './http'
import type { AuditEntryPageDto, AuditEntryQuery } from './types'

/** Journal « qui a fait quoi », réservé à l'administrateur (FR-023) : le serveur répond 403 à un utilisateur. */
export function searchAuditEntries(query: AuditEntryQuery): Promise<AuditEntryPageDto> {
  const params = new URLSearchParams()
  for (const [key, value] of Object.entries(query)) {
    if (value !== undefined && value !== '') params.set(key, String(value))
  }
  const suffix = params.size > 0 ? `?${params}` : ''
  return apiFetch<AuditEntryPageDto>(`/api/audit-entries${suffix}`)
}
