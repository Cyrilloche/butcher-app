import { apiFetch } from './http'
import type {
  AccountDto,
  CreateAccountRequest,
  ResetPasswordRequest,
  UpdateAccountRequest,
} from './types'

// Gestion des comptes (ADR-011). Toutes ces routes sont réservées à l'administrateur : le serveur
// répond 403 à un utilisateur, quoi que l'interface affiche.

export function listAccounts(): Promise<AccountDto[]> {
  return apiFetch<AccountDto[]>('/api/accounts')
}

export function createAccount(payload: CreateAccountRequest): Promise<AccountDto> {
  return apiFetch<AccountDto>('/api/accounts', { method: 'POST', json: payload })
}

/** Nom et rôle. Promouvoir un utilisateur exige `newPassword` conforme à la règle administrateur. */
export function updateAccount(id: string, payload: UpdateAccountRequest): Promise<AccountDto> {
  return apiFetch<AccountDto>(`/api/accounts/${id}`, { method: 'PUT', json: payload })
}

/** Refusé (409) sur son propre compte et sur le dernier administrateur actif. Ferme ses sessions. */
export function deactivateAccount(id: string): Promise<void> {
  return apiFetch<void>(`/api/accounts/${id}/deactivate`, { method: 'POST' })
}

export function reactivateAccount(id: string): Promise<void> {
  return apiFetch<void>(`/api/accounts/${id}/reactivate`, { method: 'POST' })
}

/** Remplace le mot de passe, lève un éventuel verrouillage et ferme les sessions du compte. */
export function resetAccountPassword(id: string, payload: ResetPasswordRequest): Promise<void> {
  return apiFetch<void>(`/api/accounts/${id}/reset-password`, { method: 'POST', json: payload })
}
