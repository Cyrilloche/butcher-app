import { apiFetch } from './http'
import type { CreateCustomerRequest, CustomerDto, UpdateCustomerRequest } from './types'

export function listCustomers(): Promise<CustomerDto[]> {
  return apiFetch<CustomerDto[]>('/api/customers')
}

export function getCustomer(id: number): Promise<CustomerDto> {
  return apiFetch<CustomerDto>(`/api/customers/${id}`)
}

export function createCustomer(payload: CreateCustomerRequest): Promise<CustomerDto> {
  return apiFetch<CustomerDto>('/api/customers', { method: 'POST', json: payload })
}

export function updateCustomer(id: number, payload: UpdateCustomerRequest): Promise<CustomerDto> {
  return apiFetch<CustomerDto>(`/api/customers/${id}`, { method: 'PUT', json: payload })
}

export function deleteCustomer(id: number): Promise<void> {
  return apiFetch<void>(`/api/customers/${id}`, { method: 'DELETE' })
}
