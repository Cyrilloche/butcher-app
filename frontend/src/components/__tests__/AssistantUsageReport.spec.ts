import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { mountWithVuetify, settle } from '@/__tests__/helpers'
import type * as ReportsApi from '@/api/reports'
import AssistantUsageReport from '../domain/AssistantUsageReport.vue'

vi.mock('@/api/reports', () => ({
  getAssistantUsage: vi.fn<typeof ReportsApi.getAssistantUsage>(),
  getAssistantRequests: vi.fn<typeof ReportsApi.getAssistantRequests>(),
}))

const { getAssistantUsage, getAssistantRequests } = vi.mocked(await import('@/api/reports'))

beforeEach(() => {
  vi.resetAllMocks()
  getAssistantUsage.mockResolvedValue([
    {
      accountId: 'g',
      accountName: 'Gérard',
      weekStart: '2026-09-21',
      requests: 5,
      stockAnswers: 2,
      saleDrafts: 2,
      notUnderstood: 1,
      errors: 0,
      rateLimited: 0,
      medianDurationMs: 1480,
    },
  ])
  getAssistantRequests.mockResolvedValue([
    {
      id: 7,
      occurredAt: '2026-09-21T08:05:00Z',
      accountName: 'Gérard',
      inputMode: 'voice',
      heardText: 'Vent de saucisson.',
      outcome: 'not_understood',
      replySpeech: 'Je n’ai pas compris.',
      durationMs: 900,
    },
  ])
})

afterEach(() => {
  document.body.innerHTML = ''
})

describe('AssistantUsageReport (RF-36, FR-025)', () => {
  it('montre l’usage de la semaine et ce qui a été entendu, en français', async () => {
    const wrapper = mountWithVuetify(AssistantUsageReport, { props: { from: '2026-09-01', to: '2026-09-30' } })
    await settle()

    const text = wrapper.text()
    expect(text).toContain('Semaine du 21 septembre')
    expect(text).toContain('1,5 s')
    expect(text).toContain('Pas compris')
    expect(text).toContain('Dictée')
    expect(text).toContain('« Vent de saucisson. »')
    expect(text).not.toMatch(/not_understood|voice|stock_answer/)
  })

  it('recharge quand la période change', async () => {
    const wrapper = mountWithVuetify(AssistantUsageReport, { props: { from: '2026-09-01', to: '2026-09-30' } })
    await settle()

    await wrapper.setProps({ from: '2026-01-01', to: '2026-12-31' })
    await settle()

    expect(getAssistantUsage).toHaveBeenLastCalledWith('2026-01-01', '2026-12-31')
    expect(getAssistantRequests).toHaveBeenLastCalledWith('2026-01-01', '2026-12-31')
  })
})
