import { apiFetch } from './http'
import type {
  AssistantRequestDto,
  AssistantUsageDto,
  CustomerSalesDto,
  ProductSalesDto,
  ReceivablesDto,
  SalesSummaryDto,
} from './types'

// Rapports de ventes, réservés à l'administrateur (FR-027 à FR-029) : le serveur répond 403 à un
// utilisateur. `from` et `to` sont des jours `YYYY-MM-DD` inclus, lus en heure de Paris.

function period(from: string, to: string): string {
  return new URLSearchParams({ from, to }).toString()
}

export function getSalesSummary(from: string, to: string): Promise<SalesSummaryDto> {
  return apiFetch<SalesSummaryDto>(`/api/reports/sales-summary?${period(from, to)}`)
}

export function getSalesByCustomer(from: string, to: string): Promise<CustomerSalesDto[]> {
  return apiFetch<CustomerSalesDto[]>(`/api/reports/sales-by-customer?${period(from, to)}`)
}

export function getSalesByProduct(from: string, to: string): Promise<ProductSalesDto[]> {
  return apiFetch<ProductSalesDto[]>(`/api/reports/sales-by-product?${period(from, to)}`)
}

/** Toutes les ventes non payées, sans période. */
export function getReceivables(): Promise<ReceivablesDto> {
  return apiFetch<ReceivablesDto>('/api/reports/receivables')
}

/** Usage de l'assistant vocal par compte et par semaine (RF-36, FR-025). */
export function getAssistantUsage(from: string, to: string): Promise<AssistantUsageDto[]> {
  return apiFetch<AssistantUsageDto[]>(`/api/reports/assistant?${period(from, to)}`)
}

/** Dernières demandes à l'assistant, avec la phrase entendue, pour comprendre un raté. */
export function getAssistantRequests(from: string, to: string, limit = 30): Promise<AssistantRequestDto[]> {
  const query = new URLSearchParams({ from, to, limit: String(limit) }).toString()
  return apiFetch<AssistantRequestDto[]>(`/api/reports/assistant/requests?${query}`)
}
