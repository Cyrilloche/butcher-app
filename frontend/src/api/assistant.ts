import { apiFetch } from './http'
import type { AssistantReplyDto } from './types'

/** Assistant vocal (spike R&D) : l'audio part au backend, qui seul appelle Mistral (cadrage D-07). */
export function askAssistantByVoice(audio: Blob): Promise<AssistantReplyDto> {
  const form = new FormData()
  const extension = audio.type.includes('mp4') ? 'm4a' : audio.type.includes('ogg') ? 'ogg' : 'webm'
  form.append('audio', audio, `demande.${extension}`)
  return apiFetch<AssistantReplyDto>('/api/assistant/voice', { method: 'POST', body: form })
}

export function askAssistantByText(text: string): Promise<AssistantReplyDto> {
  return apiFetch<AssistantReplyDto>('/api/assistant/text', { method: 'POST', json: { text } })
}

/** La phrase de réponse lue par la voix de Mistral (MP3). Elle ne contient aucun nom de client. */
export function speakWithAssistantVoice(text: string): Promise<Blob> {
  return apiFetch<Blob>('/api/assistant/speech', { method: 'POST', json: { text }, blob: true })
}
