import type { AccountRole } from '@/api/types'

/** Correspondance code ↔ affichage des rôles (docs/data-model.md §4.2). */
export const accountRoleLabels: Record<AccountRole, string> = {
  admin: 'Administrateur',
  user: 'Utilisateur',
}

/**
 * Règle de mot de passe par rôle (FR-034), énoncée à l'écran pour guider la saisie. Le serveur reste
 * seul juge : son refus, en français, est affiché tel quel.
 */
const passwordRules: Record<AccountRole, { minLength: number; example: string }> = {
  admin: { minLength: 32, example: 'Finlike-Scorer4-Wildfire-Grazing-Unbiased-Sessions' },
  user: { minLength: 20, example: 'Jambon-Saloir-2026-Mamie' },
}

export function passwordRuleText(role: AccountRole): string {
  const { minLength, example } = passwordRules[role]
  return `Au moins ${minLength} caractères, avec une majuscule, une minuscule, un chiffre et un caractère spécial. Par exemple : ${example}`
}

export function formatLastLogin(lastLoginAt: string | null): string {
  if (!lastLoginAt) return 'Jamais connecté'
  const date = new Date(lastLoginAt)
  const day = date.toLocaleDateString('fr-FR', { day: 'numeric', month: 'long', year: 'numeric' })
  const time = date.toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit' })
  return `Dernière connexion le ${day} à ${time}`
}
