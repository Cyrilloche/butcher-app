/**
 * Période des rapports (clarification du 2026-09-13) : l'année en cours à l'ouverture, des raccourcis,
 * et des dates libres. Les jours sont des clés `YYYY-MM-DD` locales, comme un `<input type="date">`.
 * Fonctions pures, testées.
 */

export interface ReportPeriod {
  /** Jour de début inclus. */
  from: string
  /** Jour de fin inclus. */
  to: string
}

export type PeriodPreset = 'this_month' | 'last_month' | 'this_year' | 'last_year'

export const periodPresets: { value: PeriodPreset; label: string }[] = [
  { value: 'this_month', label: 'Ce mois' },
  { value: 'last_month', label: 'Mois dernier' },
  { value: 'this_year', label: 'Cette année' },
  { value: 'last_year', label: 'Année dernière' },
]

/** Jour local d'une date, au format d'un `<input type="date">`. */
export function toDateKey(date: Date): string {
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${date.getFullYear()}-${month}-${day}`
}

export function presetPeriod(preset: PeriodPreset, today: Date): ReportPeriod {
  const year = today.getFullYear()
  const month = today.getMonth()
  switch (preset) {
    case 'this_month':
      // Le jour 0 du mois suivant est le dernier jour du mois : 28, 29, 30 ou 31.
      return { from: toDateKey(new Date(year, month, 1)), to: toDateKey(new Date(year, month + 1, 0)) }
    case 'last_month':
      return { from: toDateKey(new Date(year, month - 1, 1)), to: toDateKey(new Date(year, month, 0)) }
    case 'this_year':
      return { from: `${year}-01-01`, to: `${year}-12-31` }
    case 'last_year':
      return { from: `${year - 1}-01-01`, to: `${year - 1}-12-31` }
  }
}

/** Raccourci qui correspond exactement à la période, `null` pour des dates libres. */
export function matchingPreset(period: ReportPeriod, today: Date): PeriodPreset | null {
  const match = periodPresets.find(({ value }) => {
    const candidate = presetPeriod(value, today)
    return candidate.from === period.from && candidate.to === period.to
  })
  return match?.value ?? null
}

function fromDateKey(key: string): Date {
  return new Date(`${key}T00:00:00`)
}

export function describePeriod(period: ReportPeriod): string {
  const format = (key: string) =>
    fromDateKey(key).toLocaleDateString('fr-FR', { day: 'numeric', month: 'long', year: 'numeric' })
  return `Du ${format(period.from)} au ${format(period.to)}`
}

/** « septembre 2026 » pour `2026-09`. */
export function monthLabel(month: string): string {
  const [year, monthNumber] = month.split('-').map(Number)
  return new Date(year!, monthNumber! - 1, 1).toLocaleDateString('fr-FR', { month: 'long', year: 'numeric' })
}

/** Jours de calendrier écoulés entre le jour d'un horodatage et aujourd'hui. */
export function daysSince(iso: string, today: Date): number {
  const date = new Date(iso)
  const start = new Date(date.getFullYear(), date.getMonth(), date.getDate())
  const end = new Date(today.getFullYear(), today.getMonth(), today.getDate())
  // Arrondi : un passage à l'heure d'été ou d'hiver fait un jour de 23 ou 25 heures.
  return Math.max(0, Math.round((end.getTime() - start.getTime()) / 86_400_000))
}

export function formatAge(days: number): string {
  if (days === 0) return "aujourd'hui"
  return `depuis ${days} jour${days > 1 ? 's' : ''}`
}
