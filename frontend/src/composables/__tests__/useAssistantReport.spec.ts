import { describe, expect, it } from 'vitest'
import { formatDuration, outcomeLabels, requestTime, weekLabel } from '../useAssistantReport'

describe('useAssistantReport', () => {
  it('traduit chaque issue en français', () => {
    expect(Object.values(outcomeLabels)).toEqual(['Stock', 'Vente', 'Pas compris', 'Erreur', 'Limite atteinte'])
  })

  it('donne la durée en secondes, au dixième', () => {
    expect(formatDuration(1480)).toBe('1,5 s')
    expect(formatDuration(null)).toBe('—')
  })

  it('nomme la semaine par son lundi', () => {
    expect(weekLabel('2026-09-21')).toBe('Semaine du 21 septembre')
  })

  it('date une demande à l’heure de Paris', () => {
    // 22 h 30 UTC le 20 : 0 h 30 le 21 à Paris.
    expect(requestTime('2026-09-20T22:30:00Z')).toBe('21 sept., 00 h 30')
  })
})
