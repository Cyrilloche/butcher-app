import { apiFetch } from './http'
import type { AssistantReplyDto } from './types'

/** Assistant vocal (ADR-012) : l'audio part au backend, qui seul appelle Mistral. */
export function askAssistantByVoice(audio: Blob): Promise<AssistantReplyDto> {
  const form = new FormData()
  const extension = audio.type.includes('mp4') ? 'm4a' : audio.type.includes('ogg') ? 'ogg' : 'webm'
  form.append('audio', audio, `demande.${extension}`)
  return apiFetch<AssistantReplyDto>('/api/assistant/voice', { method: 'POST', body: form })
}

export function askAssistantByText(text: string): Promise<AssistantReplyDto> {
  return apiFetch<AssistantReplyDto>('/api/assistant/text', { method: 'POST', json: { text } })
}

/**
 * La réponse d'une demande, lue par la voix de Mistral (MP3). Le serveur relit la phrase qu'il a
 * journalisée : aucun texte ne lui est envoyé (FR-020).
 */
export function speakWithAssistantVoice(requestId: number): Promise<Blob> {
  return apiFetch<Blob>(`/api/assistant/requests/${requestId}/speech`, { blob: true })
}
