import { describe, expect, it } from 'vitest'
import {
  daysSince,
  describePeriod,
  formatAge,
  matchingPreset,
  monthLabel,
  presetPeriod,
  toDateKey,
} from '../useReportPeriod'

describe('presetPeriod', () => {
  const september13 = new Date(2026, 8, 13)

  it('couvre le mois et l’année en cours, bornes incluses', () => {
    expect(presetPeriod('this_month', september13)).toEqual({ from: '2026-09-01', to: '2026-09-30' })
    expect(presetPeriod('this_year', september13)).toEqual({ from: '2026-01-01', to: '2026-12-31' })
    expect(presetPeriod('last_year', september13)).toEqual({ from: '2025-01-01', to: '2025-12-31' })
  })

  it('franchit l’année : en janvier, le mois dernier est décembre', () => {
    expect(presetPeriod('last_month', new Date(2026, 0, 15))).toEqual({ from: '2025-12-01', to: '2025-12-31' })
  })

  it('connaît le dernier jour d’un février bissextile', () => {
    expect(presetPeriod('this_month', new Date(2028, 1, 10))).toEqual({ from: '2028-02-01', to: '2028-02-29' })
  })
})

describe('matchingPreset', () => {
  const today = new Date(2026, 8, 13)

  it('reconnaît un raccourci, et pas des dates libres', () => {
    expect(matchingPreset({ from: '2026-08-01', to: '2026-08-31' }, today)).toBe('last_month')
    expect(matchingPreset({ from: '2026-08-02', to: '2026-08-31' }, today)).toBeNull()
  })
})

describe('libellés', () => {
  it('écrit les jours et les mois en français', () => {
    expect(toDateKey(new Date(2026, 0, 5))).toBe('2026-01-05')
    expect(describePeriod({ from: '2026-01-01', to: '2026-12-31' })).toBe('Du 1 janvier 2026 au 31 décembre 2026')
    expect(monthLabel('2026-09')).toBe('septembre 2026')
  })

  it("dit depuis combien de jours une vente attend d'être payée", () => {
    const today = new Date(2026, 8, 13, 9, 0)
    expect(daysSince(new Date(2026, 8, 13, 8, 0).toISOString(), today)).toBe(0)
    expect(daysSince(new Date(2026, 8, 3, 23, 0).toISOString(), today)).toBe(10)
    expect(formatAge(0)).toBe("aujourd'hui")
    expect(formatAge(1)).toBe('depuis 1 jour')
    expect(formatAge(10)).toBe('depuis 10 jours')
  })
})
