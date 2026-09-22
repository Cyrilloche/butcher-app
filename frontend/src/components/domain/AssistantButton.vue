<script setup lang="ts">
import { onBeforeUnmount, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useDisplay } from 'vuetify'
import { VBottomSheet, VDialog } from 'vuetify/components'
import { askAssistantByText, askAssistantByVoice } from '@/api/assistant'
import { ApiError } from '@/api/http'
import type { AssistantReplyDto, ProductStockDto } from '@/api/types'
import { setAssistantDraft } from '@/composables/useAssistantDraft'
import { formatWeight } from '@/composables/useStock'

/**
 * Assistant vocal (spike R&D, docs/spike-assistant-vocal.md) : on appuie, on parle, on appuie pour
 * finir. Une question de stock reçoit une réponse lue à voix haute et son détail ; une vente dictée
 * ouvre le formulaire « Nouvelle vente » pré-rempli, à vérifier avant d'enregistrer (cadrage D-02).
 */
const MAX_SECONDS = 15

type Phase = 'idle' | 'listening' | 'thinking' | 'answered' | 'error'

const router = useRouter()
const { mdAndUp } = useDisplay()

const phase = ref<Phase>('idle')
const open = ref(false)
const seconds = ref(0)
const reply = ref<AssistantReplyDto | null>(null)
const error = ref<string | null>(null)
const typing = ref(false)
const typed = ref('')

let stream: MediaStream | null = null
let recorder: MediaRecorder | null = null
let chunks: Blob[] = []
let timer: ReturnType<typeof setInterval> | null = null

function pickMimeType(): string {
  return ['audio/webm;codecs=opus', 'audio/webm', 'audio/ogg;codecs=opus', 'audio/mp4'].find(
    (t) => typeof MediaRecorder !== 'undefined' && MediaRecorder.isTypeSupported(t),
  ) ?? ''
}

async function onMicClick() {
  if (phase.value === 'listening') return stopListening()
  if (phase.value === 'thinking') return
  await startListening()
}

async function startListening() {
  window.speechSynthesis?.cancel()
  error.value = null
  reply.value = null
  if (!window.isSecureContext || !navigator.mediaDevices) {
    return fail("Le micro n'est accessible qu'en connexion sécurisée (https). Tu peux écrire ta demande.")
  }
  try {
    stream ??= await navigator.mediaDevices.getUserMedia({ audio: true })
  } catch {
    return fail('Le micro est refusé. Autorise-le dans le navigateur, ou écris ta demande.')
  }
  chunks = []
  const mimeType = pickMimeType()
  recorder = new MediaRecorder(stream, mimeType ? { mimeType } : undefined)
  recorder.ondataavailable = (event) => {
    if (event.data.size) chunks.push(event.data)
  }
  recorder.onstop = () => send(new Blob(chunks, { type: recorder?.mimeType || 'audio/webm' }))
  recorder.start()
  phase.value = 'listening'
  seconds.value = 0
  timer = setInterval(() => {
    seconds.value++
    if (seconds.value >= MAX_SECONDS) stopListening()
  }, 1000)
}

function stopListening() {
  if (timer) clearInterval(timer)
  timer = null
  if (recorder?.state === 'recording') {
    phase.value = 'thinking'
    recorder.stop()
  }
}

async function send(audio: Blob) {
  await ask(() => askAssistantByVoice(audio))
}

async function sendTyped() {
  const text = typed.value.trim()
  if (!text) return
  await ask(() => askAssistantByText(text))
}

async function ask(request: () => Promise<AssistantReplyDto>) {
  phase.value = 'thinking'
  error.value = null
  try {
    reply.value = await request()
    phase.value = 'answered'
    open.value = true
    typed.value = ''
    typing.value = false
    speak(reply.value.speech)
  } catch (err) {
    fail(err instanceof ApiError ? err.message : "L'assistant ne répond pas. Réessaie dans un instant.")
  }
}

function fail(message: string) {
  phase.value = 'error'
  error.value = message
  open.value = true
}

function speak(text: string) {
  const synthesis = window.speechSynthesis
  if (!synthesis || typeof SpeechSynthesisUtterance === 'undefined') return
  synthesis.cancel()
  const utterance = new SpeechSynthesisUtterance(text)
  utterance.lang = 'fr-FR'
  const voice = synthesis.getVoices().find((v) => v.lang.startsWith('fr'))
  if (voice) utterance.voice = voice
  synthesis.speak(utterance)
}

function openSale() {
  if (!reply.value?.draft) return
  setAssistantDraft(reply.value.draft)
  close()
  router.push('/sales/add')
}

function close() {
  window.speechSynthesis?.cancel()
  open.value = false
  phase.value = 'idle'
}

function releaseMicrophone() {
  stream?.getTracks().forEach((track) => track.stop())
  stream = null
}

onBeforeUnmount(() => {
  if (timer) clearInterval(timer)
  window.speechSynthesis?.cancel()
  releaseMicrophone()
})

function stockLine(product: ProductStockDto): string {
  const units = product.openedCount > 0
    ? `${product.wholeCount} entier${product.wholeCount > 1 ? 's' : ''}, ${product.openedCount} entamé${product.openedCount > 1 ? 's' : ''}`
    : `${product.wholeCount} en stock`
  return product.remainingKg != null ? `${units} · ${formatWeight(Math.round(product.remainingKg * 1000))}` : units
}

function day(iso: string): string {
  return new Date(`${iso}T12:00:00`).toLocaleDateString('fr-FR', { day: 'numeric', month: 'long' })
}
</script>

<template>
  <button
    type="button"
    class="assistant-button"
    :class="{
      'assistant-button--listening': phase === 'listening',
      'assistant-button--thinking': phase === 'thinking',
    }"
    :aria-label="phase === 'listening' ? 'Arrêter et envoyer' : 'Parler à l\'assistant'"
    @click="onMicClick"
  >
    <v-progress-circular v-if="phase === 'thinking'" indeterminate size="30" width="3" />
    <v-icon v-else size="30">phosphor:{{ phase === 'listening' ? 'stop' : 'microphone' }}</v-icon>
  </button>
  <div v-if="phase === 'listening'" class="assistant-button__listening" role="status">
    Je t'écoute… {{ seconds }} s — appuie pour finir
  </div>

  <component
    :is="mdAndUp ? VDialog : VBottomSheet"
    v-model="open"
    :max-width="mdAndUp ? 440 : undefined"
    @after-leave="close"
  >
    <v-card class="assistant-sheet">
      <div class="assistant-sheet__body">
        <template v-if="phase === 'error'">
          <div class="assistant-sheet__speech text-error">{{ error }}</div>
        </template>

        <template v-else-if="reply">
          <div class="assistant-sheet__heard text-secondary">Tu as dit : « {{ reply.heard }} »</div>
          <div class="assistant-sheet__speech">{{ reply.speech }}</div>

          <div v-if="reply.stock?.length" class="assistant-sheet__stock">
            <div v-for="product in reply.stock" :key="product.code" class="assistant-sheet__product">
              <div class="font-weight-medium">{{ product.name }} — {{ stockLine(product) }}</div>
              <div v-for="(batch, i) in product.batches" :key="i" class="text-secondary">
                Fabriqué le {{ day(batch.productionDate) }} : {{ batch.count }}
                <span v-if="batch.remainingKg != null">· {{ formatWeight(Math.round(batch.remainingKg * 1000)) }}</span>
              </div>
              <div v-for="unit in product.opened" :key="unit.unitNumber" class="text-secondary">
                Entamé {{ unit.unitNumber }}
                <span v-if="unit.remainingKg != null">: reste {{ formatWeight(Math.round(unit.remainingKg * 1000)) }}</span>
              </div>
            </div>
          </div>

          <ul v-if="reply.draft?.warnings.length" class="assistant-sheet__warnings text-secondary">
            <li v-for="warning in reply.draft.warnings" :key="warning">{{ warning }}</li>
          </ul>
        </template>

        <div v-if="typing" class="assistant-sheet__typing">
          <v-text-field
            v-model="typed"
            label="Ta demande"
            placeholder="Ex. : il reste combien de saucissons ?"
            variant="outlined"
            hide-details
            autofocus
            @keyup.enter="sendTyped"
          />
        </div>
      </div>

      <div class="assistant-sheet__actions">
        <v-btn v-if="reply?.draft" color="primary" size="large" block @click="openSale">Ouvrir la vente</v-btn>
        <div class="assistant-sheet__row">
          <v-btn variant="text" @click="close">Fermer</v-btn>
          <v-btn v-if="typing" variant="tonal" :disabled="!typed.trim()" @click="sendTyped">Envoyer</v-btn>
          <v-btn v-else variant="text" @click="typing = true">Écrire plutôt</v-btn>
          <v-btn variant="tonal" color="primary" @click="close(); startListening()">
            <v-icon start>phosphor:microphone</v-icon>
            Reparler
          </v-btn>
        </div>
      </div>
    </v-card>
  </component>
</template>

<style scoped>
.assistant-button {
  position: fixed;
  /* En bas à gauche : le coin droit est au bouton « + » des listes (AppFab). */
  left: calc(var(--v-layout-left, 0px) + 20px);
  bottom: calc(var(--v-layout-bottom, 0px) + 32px);
  width: 64px;
  height: 64px;
  border: none;
  border-radius: 50%;
  background: rgb(var(--v-theme-secondary));
  color: rgb(var(--v-theme-surface));
  box-shadow: 0 4px 12px rgba(43, 36, 30, 0.28);
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  z-index: 1005;
}

.assistant-button--listening {
  background: rgb(var(--v-theme-error));
  animation: assistant-pulse 1.2s infinite;
}

.assistant-button--thinking {
  cursor: progress;
}

@keyframes assistant-pulse {
  50% {
    box-shadow: 0 0 0 14px rgba(176, 54, 42, 0.18);
  }
}

.assistant-button__listening {
  position: fixed;
  left: calc(var(--v-layout-left, 0px) + 96px);
  bottom: calc(var(--v-layout-bottom, 0px) + 48px);
  background: rgb(var(--v-theme-surface));
  border-radius: 10px;
  padding: 8px 12px;
  font-size: 15px;
  box-shadow: 0 2px 8px rgba(43, 36, 30, 0.18);
  z-index: 1005;
}

.assistant-sheet__body {
  display: flex;
  flex-direction: column;
  gap: 12px;
  padding: 20px 20px 8px;
}

.assistant-sheet__speech {
  font-family: 'Zilla Slab', serif;
  font-size: 22px;
  line-height: 1.3;
}

.assistant-sheet__stock {
  display: flex;
  flex-direction: column;
  gap: 10px;
  font-size: 15px;
}

.assistant-sheet__warnings {
  margin: 0;
  padding-left: 20px;
  font-size: 15px;
}

.assistant-sheet__actions {
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 8px 16px 16px;
}

.assistant-sheet__row {
  display: flex;
  flex-wrap: wrap;
  justify-content: space-between;
  gap: 8px;
}
</style>
