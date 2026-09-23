import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type * as AssistantApi from '@/api/assistant'
import type { AssistantReplyDto } from '@/api/types'
import { createVoiceActivity, pickFrenchVoice, useAssistant } from '../useAssistant'

vi.mock('@/api/assistant', () => ({
  askAssistantByText: vi.fn<typeof AssistantApi.askAssistantByText>(),
  askAssistantByVoice: vi.fn<typeof AssistantApi.askAssistantByVoice>(),
  speakWithAssistantVoice: vi.fn<typeof AssistantApi.speakWithAssistantVoice>(),
}))

const assistantApi = vi.mocked(await import('@/api/assistant'))

/** Rejoue une suite de volumes, un échantillon tous les dixièmes de seconde, et rend le dernier état. */
function play(volumes: number[]) {
  const activity = createVoiceActivity()
  const states = volumes.map((rms, i) => activity.push(rms, i * 100))
  return { states, last: states[states.length - 1] }
}

const silence = (n: number) => Array<number>(n).fill(0.005)
const voice = (n: number) => Array<number>(n).fill(0.1)

describe('createVoiceActivity', () => {
  it('termine deux secondes après la fin de la phrase, pas avant', () => {
    const { states } = play([...silence(5), ...voice(20), ...silence(20)])

    expect(states.slice(0, -1)).not.toContain('done')
    expect(states[states.length - 1]).toBe('done')
  })

  it('laisse une hésitation d’une seconde au milieu de la phrase', () => {
    const { states } = play([...silence(5), ...voice(10), ...silence(10), ...voice(10)])

    expect(states).not.toContain('done')
  })

  it('abandonne au bout de sept secondes sans parole', () => {
    expect(play(silence(71)).last).toBe('nothing-heard')
  })

  it('entend une voix qui commence tout de suite, sans silence à mesurer', () => {
    const { states } = play([...voice(30), ...silence(21)])

    expect(states[states.length - 1]).toBe('done')
  })

  it('s’arrête à trente secondes même si on parle encore', () => {
    expect(play(voice(301)).last).toBe('done')
  })
})

describe('pickFrenchVoice', () => {
  const voice = (name: string, lang: string, localService: boolean) =>
    ({ name, lang, localService, default: false, voiceURI: name }) as SpeechSynthesisVoice

  it('préfère une voix française de Google en réseau à la voix locale par défaut', () => {
    const chosen = pickFrenchVoice([
      voice('English', 'en-US', false),
      voice('Français', 'fr-FR', true),
      voice('Google français', 'fr-FR', false),
      voice('Google français (Canada)', 'fr-CA', false),
    ])

    expect(chosen?.name).toBe('Google français')
  })

  it('ne choisit rien sans voix française', () => {
    expect(pickFrenchVoice([voice('English', 'en-US', false)])).toBeUndefined()
  })
})

describe('voix de la réponse (FR-008, FR-020)', () => {
  const reply: AssistantReplyDto = {
    requestId: 42,
    kind: 'stock_answer',
    speech: 'Saucisse curry : il t’en reste 11.',
    heard: 'il reste combien de saucisse curry',
    stock: [],
    draft: null,
  }
  const play = vi.fn<() => Promise<void>>()
  const phoneSpeak = vi.fn<(utterance: { text: string }) => void>()

  beforeEach(() => {
    vi.resetAllMocks()
    play.mockResolvedValue()
    vi.stubGlobal(
      'Audio',
      class {
        src: string
        onended: (() => void) | null = null
        constructor(src: string) {
          this.src = src
        }
        play = play
        pause() {}
      },
    )
    vi.stubGlobal(
      'SpeechSynthesisUtterance',
      class {
        text: string
        lang = ''
        rate = 1
        voice: unknown = null
        constructor(text: string) {
          this.text = text
        }
      },
    )
    vi.stubGlobal('speechSynthesis', {
      cancel: vi.fn<() => void>(),
      speak: phoneSpeak,
      getVoices: () => [{ name: 'Google français', lang: 'fr-FR', localService: false }],
      addEventListener: vi.fn<() => void>(),
    })
    URL.createObjectURL = vi.fn<(blob: Blob) => string>(() => 'blob:voix')
    assistantApi.askAssistantByText.mockResolvedValue(reply)
  })

  afterEach(() => {
    useAssistant().close()
    vi.unstubAllGlobals()
  })

  it('demande la voix de Mistral pour la demande, jamais pour un texte', async () => {
    assistantApi.speakWithAssistantVoice.mockResolvedValue(new Blob(['ID3']))

    useAssistant().askByText('il reste combien de saucisse curry')

    await vi.waitFor(() => expect(play).toHaveBeenCalledOnce())
    expect(assistantApi.speakWithAssistantVoice).toHaveBeenCalledWith(42)
    expect(phoneSpeak).not.toHaveBeenCalled()
  })

  it('prend la voix du téléphone quand celle de Mistral ne vient pas', async () => {
    assistantApi.speakWithAssistantVoice.mockRejectedValue(new Error('indisponible'))

    useAssistant().askByText('il reste combien de saucisse curry')

    await vi.waitFor(() => expect(phoneSpeak).toHaveBeenCalledOnce())
    expect(phoneSpeak.mock.calls[0]![0].text).toBe(reply.speech)
  })
})
