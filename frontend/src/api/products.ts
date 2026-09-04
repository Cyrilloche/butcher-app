import { apiFetch } from './http'
import type { CreateProductRequest, ProductDto, UpdateProductRequest } from './types'

export function listProducts(includeInactive = false): Promise<ProductDto[]> {
  return apiFetch<ProductDto[]>(`/api/products?includeInactive=${includeInactive}`)
}

export function getProduct(id: number): Promise<ProductDto> {
  return apiFetch<ProductDto>(`/api/products/${id}`)
}

export function createProduct(payload: CreateProductRequest): Promise<ProductDto> {
  return apiFetch<ProductDto>('/api/products', { method: 'POST', json: payload })
}

export function updateProduct(id: number, payload: UpdateProductRequest): Promise<ProductDto> {
  return apiFetch<ProductDto>(`/api/products/${id}`, { method: 'PUT', json: payload })
}

export function deactivateProduct(id: number): Promise<void> {
  return apiFetch<void>(`/api/products/${id}/deactivate`, { method: 'POST' })
}

export function reactivateProduct(id: number): Promise<void> {
  return apiFetch<void>(`/api/products/${id}/reactivate`, { method: 'POST' })
}
