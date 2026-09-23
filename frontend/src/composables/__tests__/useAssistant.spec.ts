import { describe, expect, it } from 'vitest'
import { createVoiceActivity, pickFrenchVoice } from '../useAssistant'

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
