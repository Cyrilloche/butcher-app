import { describe, expect, it } from 'vitest'
import type { AuditEntryDto } from '@/api/types'
import { describeDeletedContent, entryAuthor, entryLink } from '../useJournal'

function entry(overrides: Partial<AuditEntryDto>): AuditEntryDto {
  return {
    id: 1,
    occurredAt: '2026-09-13T19:15:00Z',
    accountId: 'a1',
    accountName: 'Mireille',
    action: 'created',
    entityType: 'sale',
    entityId: '17',
    entityLabel: 'V-260913-2 (3 lignes)',
    deletedContent: null,
    ...overrides,
  }
}

describe('entryAuthor', () => {
  it("donne le nom de l'auteur", () => {
    expect(entryAuthor(entry({}))).toBe('Mireille')
  })

  it('dit pourquoi il manque', () => {
    expect(entryAuthor(entry({ accountId: null, accountName: null, action: 'login_failed', entityType: null }))).toBe(
      'Personne (adresse inconnue)',
    )
    expect(entryAuthor(entry({ accountId: null, accountName: null }))).toBe('Hors application')
  })
})

describe('entryLink', () => {
  it("ouvre une vente ou un client qui existent encore", () => {
    expect(entryLink(entry({}))).toBe('/sales/17')
    expect(entryLink(entry({ entityType: 'customer', entityId: '3' }))).toBe('/customers/3')
  })

  it("n'ouvre ni une suppression, ni un geste groupé, ni un objet sans écran", () => {
    expect(entryLink(entry({ action: 'deleted' }))).toBeNull()
    expect(entryLink(entry({ entityType: 'stock_unit', entityId: null }))).toBeNull()
    expect(entryLink(entry({ entityType: 'production_batch' }))).toBeNull()
  })
})

describe('describeDeletedContent', () => {
  it('lit une vente supprimée en français, lignes comprises', () => {
    const view = describeDeletedContent(
      entry({
        action: 'deleted',
        deletedContent: {
          saleNumber: 'V-260913-2',
          date: '2026-09-13T10:00:00Z',
          customerName: 'Jean Dupont',
          paid: false,
          total: 30,
          notes: null,
          lines: [
            { unitNumber: 'JB-260913-1', productName: 'Jambon sec', soldWeight: 1, amount: 20 },
            { unitNumber: 'TC-260913-1', productName: 'Terrine', soldWeight: null, amount: 10 },
          ],
        },
      }),
    )

    expect(view?.fields).toEqual([
      { label: 'Numéro', value: 'V-260913-2' },
      { label: 'Date', value: '13 septembre 2026' },
      { label: 'Client', value: 'Jean Dupont' },
      { label: 'Paiement', value: 'À payer' },
      { label: 'Montant total', value: '30,00 €' },
    ])
    expect(view?.items).toEqual(['JB-260913-1 · Jambon sec · 1 kg · 20,00 €', 'TC-260913-1 · Terrine · 10,00 €'])
  })

  it('lit une unité supprimée sans valeur technique anglaise', () => {
    const view = describeDeletedContent(
      entry({
        action: 'deleted',
        entityType: 'stock_unit',
        deletedContent: { unitNumber: 'JB-260913-3', productName: 'Jambon sec', productionDate: '2026-09-13', weight: 0.4, status: 'available' },
      }),
    )

    expect(view?.fields).toContainEqual({ label: 'Poids', value: '400 g' })
    expect(view?.fields).toContainEqual({ label: 'Statut', value: 'Disponible' })
    expect(view?.fields).toContainEqual({ label: 'Fabriquée le', value: '13 septembre 2026' })
  })

  it("n'a rien à lire hors suppression", () => {
    expect(describeDeletedContent(entry({}))).toBeNull()
  })
})
