import type { AuditAction, AuditEntityType, AuditEntryDto } from '@/api/types'

/**
 * Lecture française du journal (FR-023, FR-032) : libellés des natures et des types d'objet, auteur,
 * lien vers l'objet, contenu d'une suppression. Fonctions pures, testées.
 */

export const auditActionLabels: Record<AuditAction, string> = {
  created: 'Création',
  updated: 'Modification',
  deleted: 'Suppression',
  login_succeeded: 'Connexion',
  login_failed: 'Connexion refusée',
  locked_out: 'Compte verrouillé',
  password_changed: 'Mot de passe changé',
}

export const auditEntityTypeLabels: Record<AuditEntityType, string> = {
  product: 'Produit',
  production_batch: 'Fournée',
  stock_unit: 'Unité',
  sale: 'Vente',
  stock_movement: 'Sortie de stock',
  customer: 'Client',
  account: 'Compte',
}

export type AuditTone = 'success' | 'warning' | 'error' | 'neutral'

/** Couleur du badge de nature : les suppressions et les verrouillages se voient d'abord. */
export const auditActionTones: Record<AuditAction, AuditTone> = {
  created: 'success',
  updated: 'neutral',
  deleted: 'error',
  login_succeeded: 'neutral',
  login_failed: 'warning',
  locked_out: 'error',
  password_changed: 'neutral',
}

export function formatOccurredAt(occurredAt: string): string {
  const date = new Date(occurredAt)
  const day = date.toLocaleDateString('fr-FR', { day: '2-digit', month: '2-digit', year: 'numeric' })
  const time = date.toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit' })
  return `${day} à ${time}`
}

/** Auteur affiché ; sans compte, dit pourquoi plutôt que de laisser un vide. */
export function entryAuthor(entry: AuditEntryDto): string {
  if (entry.accountName) return entry.accountName
  return entry.action === 'login_failed' ? 'Personne (adresse inconnue)' : 'Hors application'
}

/**
 * Lien vers l'objet quand il existe encore et qu'un écran le montre. Une suppression n'en a pas : son
 * contenu est dans l'entrée.
 */
export function entryLink(entry: AuditEntryDto): string | null {
  if (entry.action === 'deleted' || !entry.entityId) return null
  switch (entry.entityType) {
    case 'sale':
      return `/sales/${entry.entityId}`
    case 'customer':
      return `/customers/${entry.entityId}`
    case 'account':
      return '/accounts'
    default:
      return null
  }
}

export interface DeletedContentView {
  fields: { label: string; value: string }[]
  itemsTitle: string | null
  items: string[]
}

type Content = Record<string, unknown>

const movementTypeLabels: Record<string, string> = { sale: 'Vente', personal: 'Perso', loss: 'Perte' }
const unitStatusLabels: Record<string, string> = {
  available: 'Disponible',
  opened: 'Entamée',
  sold: 'Vendue',
  personal: 'Perso',
  lost: 'Perdue',
}

function text(value: unknown): string | null {
  if (value === null || value === undefined || value === '') return null
  return String(value)
}

function euros(value: unknown): string | null {
  return typeof value === 'number'
    ? `${value.toLocaleString('fr-FR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} €`
    : null
}

/** Poids en kilogrammes (decimal(10,3) côté serveur). */
function weight(value: unknown): string | null {
  if (typeof value !== 'number') return null
  const grams = Math.round(value * 1000)
  return grams >= 1000 ? `${(grams / 1000).toLocaleString('fr-FR', { maximumFractionDigits: 3 })} kg` : `${grams} g`
}

function day(value: unknown): string | null {
  if (typeof value !== 'string') return null
  // Une date seule (`YYYY-MM-DD`) se lit telle quelle, sans décalage de fuseau.
  const date = /^\d{4}-\d{2}-\d{2}$/.test(value) ? new Date(`${value}T00:00:00`) : new Date(value)
  return Number.isNaN(date.getTime()) ? null : date.toLocaleDateString('fr-FR', { day: 'numeric', month: 'long', year: 'numeric' })
}

function fields(...pairs: [string, string | null][]): DeletedContentView['fields'] {
  return pairs.filter((pair): pair is [string, string] => pair[1] !== null).map(([label, value]) => ({ label, value }))
}

function list(value: unknown): Content[] {
  return Array.isArray(value) ? (value.filter((item) => item && typeof item === 'object') as Content[]) : []
}

/** Contenu d'une suppression, en français, suffisant pour ressaisir l'objet (FR-022). */
export function describeDeletedContent(entry: AuditEntryDto): DeletedContentView | null {
  const c = entry.deletedContent
  if (entry.action !== 'deleted' || !c) return null

  switch (entry.entityType) {
    case 'sale':
      return {
        fields: fields(
          ['Numéro', text(c.saleNumber)],
          ['Date', day(c.date)],
          ['Client', text(c.customerName)],
          ['Paiement', c.paid === true ? 'Payée' : c.paid === false ? 'À payer' : null],
          ['Montant total', euros(c.total)],
          ['Note', text(c.notes)],
        ),
        itemsTitle: 'Lignes',
        items: list(c.lines).map((line) =>
          [text(line.unitNumber), text(line.productName), weight(line.soldWeight), euros(line.amount)]
            .filter(Boolean)
            .join(' · '),
        ),
      }
    case 'stock_movement':
      return {
        fields: fields(
          ['Nature', movementTypeLabels[String(c.type)] ?? null],
          ['Vente', text(c.saleNumber)],
          ['Date', day(c.date)],
          ['Unité', text(c.unitNumber)],
          ['Produit', text(c.productName)],
          ['Poids', weight(c.soldWeight)],
          ['Montant', euros(c.amount)],
          ['Note', text(c.notes)],
        ),
        itemsTitle: null,
        items: [],
      }
    case 'production_batch':
      return {
        fields: fields(
          ['Produit', text(c.productName)],
          ['Fabriquée le', day(c.productionDate)],
          ['Prix de vente', euros(c.salePrice)],
          ['Matière première', text(c.rawMaterialRef)],
          ['DLC', day(c.expiryDate)],
          ['Note', text(c.notes)],
        ),
        itemsTitle: 'Unités',
        items: list(c.units).map((unit) => [text(unit.unitNumber), weight(unit.weight)].filter(Boolean).join(' · ')),
      }
    case 'stock_unit':
      return {
        fields: fields(
          ['Unité', text(c.unitNumber)],
          ['Produit', text(c.productName)],
          ['Fabriquée le', day(c.productionDate)],
          ['Poids', weight(c.weight)],
          ['Statut', unitStatusLabels[String(c.status)] ?? null],
        ),
        itemsTitle: null,
        items: [],
      }
    case 'customer':
      return {
        fields: fields(
          ['Nom', text(c.lastName)],
          ['Prénom', text(c.firstName)],
          ['Téléphone', text(c.phone)],
          ['Notes', text(c.notes)],
        ),
        itemsTitle: null,
        items: [],
      }
    case 'product':
      return {
        fields: fields(
          ['Nom', text(c.name)],
          ['Code', text(c.code)],
          ['Vente', c.saleMode === 'by_weight' ? 'Au poids' : c.saleMode === 'by_piece' ? 'À la pièce' : null],
          ['À la tranche', c.allowPartialSale === true ? 'Oui' : null],
        ),
        itemsTitle: null,
        items: [],
      }
    default:
      return null
  }
}
