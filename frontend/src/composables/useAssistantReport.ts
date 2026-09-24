import type { VoiceInputMode, VoiceRequestOutcome } from '@/api/types'

/**
 * Libellés et formats de l'usage de l'assistant vocal (RF-36), d'après la table de correspondance de
 * `docs/data-model.md` §4.2 : aucune valeur technique anglaise à l'écran.
 */
export const outcomeLabels: Record<VoiceRequestOutcome, string> = {
  stock_answer: 'Stock',
  sale_draft: 'Vente',
  not_understood: 'Pas compris',
  error: 'Erreur',
  rate_limited: 'Limite atteinte',
}

export const inputModeLabels: Record<VoiceInputMode, string> = {
  voice: 'Dictée',
  text: 'Écrite',
}

/** Couleur d'une issue : ce qui a abouti en succès, ce qui mérite d'être relu en alerte. */
export const outcomeTones: Record<VoiceRequestOutcome, 'success' | 'warning' | 'neutral' | 'error'> = {
  stock_answer: 'success',
  sale_draft: 'success',
  not_understood: 'warning',
  error: 'error',
  rate_limited: 'neutral',
}

/** « 1,5 s » ; un tiret quand aucune demande n'a été traitée. */
export function formatDuration(ms: number | null): string {
  if (ms === null) return '—'
  return `${(ms / 1000).toLocaleString('fr-FR', { minimumFractionDigits: 1, maximumFractionDigits: 1 })} s`
}

/** « Semaine du 21 septembre » : le jour est déjà celui de Paris, lu tel quel. */
export function weekLabel(weekStart: string): string {
  const [year, month, day] = weekStart.split('-').map(Number)
  const monday = new Date(year!, month! - 1, day!)
  return `Semaine du ${monday.toLocaleDateString('fr-FR', { day: 'numeric', month: 'long' })}`
}

/** « 23 sept., 14 h 05 », à l'heure de Paris. */
export function requestTime(iso: string): string {
  const date = new Date(iso)
  const day = date.toLocaleDateString('fr-FR', { day: 'numeric', month: 'short', timeZone: 'Europe/Paris' })
  const time = date
    .toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit', timeZone: 'Europe/Paris' })
    .replace(':', ' h ')
  return `${day}, ${time}`
}
