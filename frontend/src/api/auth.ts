import { rawRequest } from './http'
import type { AuthResponseDto, LoginRequest } from './types'

export function login(credentials: LoginRequest): Promise<AuthResponseDto> {
  return rawRequest<AuthResponseDto>('/api/auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(credentials),
  })
}

/** Lit le cookie httpOnly `refreshToken` (Path=/api/auth) — aucun corps à envoyer. */
export function refresh(): Promise<AuthResponseDto> {
  return rawRequest<AuthResponseDto>('/api/auth/refresh', { method: 'POST' })
}

export function logout(): Promise<void> {
  return rawRequest<void>('/api/auth/logout', { method: 'POST' })
}
