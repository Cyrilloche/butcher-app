import { ref } from 'vue'
import { askAssistantByText, askAssistantByVoice, speakWithAssistantVoice } from '@/api/assistant'
import { ApiError } from '@/api/http'
import type { AssistantReplyDto } from '@/api/types'

/**
 * Assistant vocal (spike R&D, docs/assistant-vocal-fonctionnement.md). Un seul assistant pour toute
 * l'application : on le lance depuis le bouton « + » (`ActionFab`), et `AssistantPanel`, posé une fois
 * dans la mise en page, affiche l'écoute puis la réponse.
 */

export type AssistantPhase = 'idle' | 'listening' | 'thinking' | 'answered' | 'error'

export interface VoiceActivityOptions {
  /** Début de l'écoute consacré à mesurer le bruit de fond. */
  calibrationMs?: number
  /** Silence après la parole qui termine la demande. */
  silenceMs?: number
  /** Sans parole au bout de ce délai, on abandonne. */
  noSpeechMs?: number
  /** Durée maximale d'une demande. */
  maxMs?: number
}

export type VoiceActivity = 'listening' | 'done' | 'nothing-heard'

/**
 * Détecte la fin d'une phrase à partir du volume (RMS) du micro, échantillon après échantillon,
 * comme un assistant de téléphone : on parle, on se tait, c'est envoyé. Le seuil de parole se règle
 * sur le bruit de fond mesuré au début, borné pour qu'une voix qui commence tout de suite ou une
 * cuisine bruyante ne le rendent pas inatteignable.
 */
export function createVoiceActivity(options: VoiceActivityOptions = {}) {
  const { calibrationMs = 400, silenceMs = 2000, noSpeechMs = 7000, maxMs = 30000 } = options
  let startedAt: number | null = null
  let noiseFloor = Infinity
  let speaking = false
  let lastVoiceAt = 0

  return {
    push(rms: number, now: number): VoiceActivity {
      startedAt ??= now
      const elapsed = now - startedAt
      if (elapsed < calibrationMs) noiseFloor = Math.min(noiseFloor, rms)
      const threshold = Math.min(0.08, Math.max(0.015, (Number.isFinite(noiseFloor) ? noiseFloor : 0) * 3))

      if (rms > threshold) {
        speaking = true
        lastVoiceAt = now
      }
      if (elapsed >= maxMs) return 'done'
      if (speaking && now - lastVoiceAt >= silenceMs) return 'done'
      if (!speaking && elapsed >= noSpeechMs) return 'nothing-heard'
      return 'listening'
    },
  }
}

const phase = ref<AssistantPhase>('idle')
const open = ref(false)
/** Niveau sonore du micro, entre 0 et 1, pour la jauge d'écoute. */
const level = ref(0)
const reply = ref<AssistantReplyDto | null>(null)
const error = ref<string | null>(null)

let stream: MediaStream | null = null
let recorder: MediaRecorder | null = null
let audioContext: AudioContext | null = null
let sampler: ReturnType<typeof setInterval> | null = null
let chunks: Blob[] = []
/** Écoute abandonnée (rien entendu, panneau fermé) : l'enregistrement ne part pas. */
let discard = false

function pickMimeType(): string {
  return (
    ['audio/webm;codecs=opus', 'audio/webm', 'audio/ogg;codecs=opus', 'audio/mp4'].find(
      (t) => typeof MediaRecorder !== 'undefined' && MediaRecorder.isTypeSupported(t),
    ) ?? ''
  )
}

/** Libère le micro : le voyant « micro actif » du téléphone ne reste pas allumé entre deux demandes. */
function releaseMicrophone() {
  if (sampler) clearInterval(sampler)
  sampler = null
  void audioContext?.close()
  audioContext = null
  stream?.getTracks().forEach((track) => track.stop())
  stream = null
  level.value = 0
}

function fail(message: string) {
  releaseMicrophone()
  phase.value = 'error'
  error.value = message
  open.value = true
}

async function start() {
  if (phase.value === 'listening' || phase.value === 'thinking') return
  stopSpeaking()
  releaseMicrophone()
  error.value = null
  reply.value = null
  open.value = false
  if (!window.isSecureContext || !navigator.mediaDevices) {
    return fail("Le micro n'est accessible qu'en connexion sécurisée (https). Tu peux écrire ta demande.")
  }
  try {
    stream = await navigator.mediaDevices.getUserMedia({ audio: true })
  } catch {
    return fail('Le micro est refusé. Autorise-le dans le navigateur, ou écris ta demande.')
  }

  chunks = []
  discard = false
  const mimeType = pickMimeType()
  recorder = new MediaRecorder(stream, mimeType ? { mimeType } : undefined)
  recorder.ondataavailable = (event) => {
    if (event.data.size) chunks.push(event.data)
  }
  recorder.onstop = () => {
    const audio = new Blob(chunks, { type: recorder?.mimeType || 'audio/webm' })
    releaseMicrophone()
    if (!discard) void ask(() => askAssistantByVoice(audio))
  }
  recorder.start()
  phase.value = 'listening'
  listenForSilence(stream)
}

/** Mesure le volume tous les dixièmes de seconde et arrête l'écoute quand la phrase est finie. */
function listenForSilence(source: MediaStream) {
  audioContext = new AudioContext()
  const analyser = audioContext.createAnalyser()
  analyser.fftSize = 1024
  audioContext.createMediaStreamSource(source).connect(analyser)
  const samples = new Float32Array(analyser.fftSize)
  const activity = createVoiceActivity()

  sampler = setInterval(() => {
    analyser.getFloatTimeDomainData(samples)
    const rms = Math.sqrt(samples.reduce((sum, s) => sum + s * s, 0) / samples.length)
    level.value = Math.min(1, rms / 0.15)
    const state = activity.push(rms, performance.now())
    if (state === 'done') stop()
    if (state === 'nothing-heard') {
      cancelListening()
      fail("Je n'ai rien entendu. Réessaie en parlant près du téléphone.")
    }
  }, 100)
}

/** Termine l'écoute et envoie ce qui a été dit (bouton « J'ai fini », ou silence détecté). */
function stop() {
  if (sampler) clearInterval(sampler)
  sampler = null
  if (recorder?.state === 'recording') {
    phase.value = 'thinking'
    recorder.stop()
  }
}

/** Abandonne l'écoute sans rien envoyer. */
function cancelListening() {
  discard = true
  if (recorder?.state === 'recording') recorder.stop()
  else releaseMicrophone()
  if (phase.value === 'listening') phase.value = 'idle'
}

async function ask(request: () => Promise<AssistantReplyDto>) {
  phase.value = 'thinking'
  error.value = null
  try {
    reply.value = await request()
    phase.value = 'answered'
    open.value = true
    void speak(reply.value.speech)
  } catch (err) {
    fail(err instanceof ApiError ? err.message : "L'assistant ne répond pas. Réessaie dans un instant.")
  }
}

function askByText(text: string) {
  const trimmed = text.trim()
  if (trimmed) void ask(() => askAssistantByText(trimmed))
}

/**
 * La voix française la plus naturelle du téléphone : les voix « réseau » de Google (non locales)
 * sonnent bien moins robotiques que la voix par défaut. Plus le score est bas, mieux c'est.
 */
export function pickFrenchVoice(voices: SpeechSynthesisVoice[]): SpeechSynthesisVoice | undefined {
  const score = (v: SpeechSynthesisVoice) =>
    (v.lang.replace('_', '-') === 'fr-FR' ? 0 : 4) +
    (/google|natural|neural|online|network/i.test(v.name) ? 0 : 2) +
    (v.localService ? 1 : 0)
  return voices.filter((v) => v.lang.toLowerCase().startsWith('fr')).sort((a, b) => score(a) - score(b))[0]
}

/** La liste des voix arrive après coup sur Android : vide au premier appel, on l'attend (1 s au plus). */
function loadVoices(synthesis: SpeechSynthesis): Promise<SpeechSynthesisVoice[]> {
  const voices = synthesis.getVoices()
  if (voices.length) return Promise.resolve(voices)
  return new Promise((resolve) => {
    const done = () => resolve(synthesis.getVoices())
    synthesis.addEventListener('voiceschanged', done, { once: true })
    setTimeout(done, 1000)
  })
}

let playing: HTMLAudioElement | null = null

/** Coupe la voix en cours, celle de Mistral comme celle du téléphone. */
function stopSpeaking() {
  playing?.pause()
  playing = null
  window.speechSynthesis?.cancel()
}

/**
 * Lit la réponse avec la voix de Mistral (Voxtral TTS, environ 1 s de plus), et se rabat sur la voix
 * du téléphone si elle ne vient pas. Le texte, lui, s'affiche tout de suite.
 */
async function speak(text: string) {
  stopSpeaking()
  try {
    const audio = new Audio(URL.createObjectURL(await speakWithAssistantVoice(text)))
    playing = audio
    audio.onended = () => URL.revokeObjectURL(audio.src)
    await audio.play()
  } catch {
    // Voix de Mistral indisponible : celle du téléphone, sauf si le panneau a été fermé entre-temps.
    if (open.value) void speakWithPhone(text)
  }
}

async function speakWithPhone(text: string) {
  const synthesis = window.speechSynthesis
  if (!synthesis || typeof SpeechSynthesisUtterance === 'undefined') return
  synthesis.cancel()
  const utterance = new SpeechSynthesisUtterance(text)
  utterance.lang = 'fr-FR'
  utterance.rate = 0.95
  const voice = pickFrenchVoice(await loadVoices(synthesis))
  if (voice) utterance.voice = voice
  synthesis.speak(utterance)
}

function close() {
  stopSpeaking()
  if (phase.value === 'listening') cancelListening()
  open.value = false
  if (phase.value !== 'thinking') phase.value = 'idle'
}

/** Ouvre le panneau sur la saisie au clavier, sans passer par le micro. */
function write() {
  stopSpeaking()
  reply.value = null
  error.value = null
  phase.value = 'idle'
  open.value = true
}

export function useAssistant() {
  return { phase, open, level, reply, error, start, stop, cancelListening, askByText, close, write }
}
