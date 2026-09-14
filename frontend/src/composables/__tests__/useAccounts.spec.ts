import { describe, expect, it } from 'vitest'
import { accountRoleLabels, formatLastLogin, passwordRuleText } from '../useAccounts'

describe('useAccounts', () => {
  it('traduit les rôles, sans jamais laisser passer le code anglais', () => {
    expect(accountRoleLabels).toEqual({ admin: 'Administrateur', user: 'Utilisateur' })
  })

  it('annonce 32 caractères pour un administrateur et 20 pour un utilisateur, comme le serveur', () => {
    expect(passwordRuleText('admin')).toMatch(/^Au moins 32 caractères/)
    expect(passwordRuleText('user')).toMatch(/^Au moins 20 caractères/)
  })

  it('donne des exemples qui respectent eux-mêmes la règle annoncée', () => {
    for (const [role, min] of [['admin', 32], ['user', 20]] as const) {
      const example = passwordRuleText(role).split('Par exemple : ')[1]!
      expect(example.length).toBeGreaterThanOrEqual(min)
      expect(example).toMatch(/[A-Z]/)
      expect(example).toMatch(/[a-z]/)
      expect(example).toMatch(/\d/)
      expect(example).toMatch(/[^A-Za-z0-9]/)
    }
  })

  it('dit « jamais connecté » à défaut de connexion, et date la dernière sinon', () => {
    expect(formatLastLogin(null)).toBe('Jamais connecté')
    expect(formatLastLogin(new Date(2026, 8, 14, 9, 5).toISOString())).toBe('Dernière connexion le 14 septembre 2026 à 09:05')
  })
})
