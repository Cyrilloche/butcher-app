import { apiFetch, rawRequest } from './http'
import type { AuthResponseDto, ChangePasswordRequest, LoginRequest, MeDto } from './types'

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

/**
 * Compte connecté. Requête authentifiée ordinaire : elle ne sert pas au rafraîchissement, donc
 * `apiFetch` peut l'emprunter sans risque de boucle.
 */
export function me(): Promise<MeDto> {
  return apiFetch<MeDto>('/api/auth/me')
}

/**
 * Change son propre mot de passe. Un mot de passe actuel erroné répond 400 (et non 401) : c'est une
 * erreur de saisie, le message du serveur est à afficher tel quel. Les autres appareils sont
 * déconnectés, celui-ci reste connecté.
 */
export function changePassword(payload: ChangePasswordRequest): Promise<void> {
  return apiFetch<void>('/api/auth/change-password', { method: 'POST', json: payload })
}
