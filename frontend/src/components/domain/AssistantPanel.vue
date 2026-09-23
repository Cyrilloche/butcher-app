<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useDisplay } from 'vuetify'
import { VBottomSheet, VDialog } from 'vuetify/components'
import type { ProductStockDto } from '@/api/types'
import { useAssistant } from '@/composables/useAssistant'
import { setAssistantDraft } from '@/composables/useAssistantDraft'
import { formatWeight } from '@/composables/useStock'

/**
 * Affichage de l'assistant vocal (spike R&D), posé une fois dans la mise en page : la carte d'écoute
 * avec sa jauge, puis la réponse. On le lance depuis le bouton « + » (`ActionFab`).
 */
const router = useRouter()
const { mdAndUp } = useDisplay()
const { phase, open, level, reply, error, start, stop, cancelListening, askByText, close } = useAssistant()

const typed = ref('')

function sendTyped() {
  askByText(typed.value)
  typed.value = ''
}

function openSale() {
  if (!reply.value?.draft) return
  setAssistantDraft(reply.value.draft)
  close()
  router.push('/sales/add')
}

function stockLine(product: ProductStockDto): string {
  const units =
    product.openedCount > 0
      ? `${product.wholeCount} entier${product.wholeCount > 1 ? 's' : ''}, ${product.openedCount} entamé${product.openedCount > 1 ? 's' : ''}`
      : `${product.wholeCount} en stock`
  return product.remainingKg != null ? `${units} · ${formatWeight(Math.round(product.remainingKg * 1000))}` : units
}

function day(iso: string): string {
  return new Date(`${iso}T12:00:00`).toLocaleDateString('fr-FR', { day: 'numeric', month: 'long' })
}
</script>

<template>
  <div v-if="phase === 'listening' || phase === 'thinking'" class="assistant-listening" role="status">
    <template v-if="phase === 'listening'">
      <div class="assistant-listening__title">
        <v-icon size="22" color="error">phosphor:microphone</v-icon>
        Je t'écoute…
      </div>
      <div class="assistant-listening__meter" aria-hidden="true">
        <div class="assistant-listening__level" :style="{ transform: `scaleX(${Math.max(0.04, level)})` }" />
      </div>
      <div class="assistant-listening__hint text-secondary">Parle, puis tais-toi : j'envoie tout seul.</div>
      <div class="assistant-listening__actions">
        <v-btn variant="text" @click="cancelListening">Annuler</v-btn>
        <v-btn color="primary" variant="tonal" @click="stop">J'ai fini</v-btn>
      </div>
    </template>
    <div v-else class="assistant-listening__title">
      <v-progress-circular indeterminate size="22" width="3" color="primary" />
      Je réfléchis…
    </div>
  </div>

  <!-- Seule une fermeture voulue (clic à côté, retour) passe par close() : « Reparler » referme le
       panneau lui-même, et ne doit pas annuler l'écoute qu'il vient de lancer. -->
  <component
    :is="mdAndUp ? VDialog : VBottomSheet"
    :model-value="open"
    :max-width="mdAndUp ? 440 : undefined"
    @update:model-value="(value: boolean) => !value && close()"
  >
    <v-card class="assistant-sheet">
      <div class="assistant-sheet__body">
        <div v-if="phase === 'error'" class="assistant-sheet__speech text-error">{{ error }}</div>

        <template v-else-if="reply">
          <div class="text-secondary">Tu as dit : « {{ reply.heard }} »</div>
          <div class="assistant-sheet__speech">{{ reply.speech }}</div>

          <div v-if="reply.stock?.length" class="assistant-sheet__stock">
            <div v-for="product in reply.stock" :key="product.code">
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

        <div v-else class="assistant-sheet__speech">Écris ta demande.</div>

        <v-text-field
          v-model="typed"
          label="Écrire plutôt"
          placeholder="Ex. : il reste combien de saucissons ?"
          variant="outlined"
          hide-details
          @keyup.enter="sendTyped"
        >
          <template #append-inner>
            <v-btn icon variant="text" size="small" :disabled="!typed.trim()" aria-label="Envoyer" @click="sendTyped">
              <v-icon>phosphor:caret-right</v-icon>
            </v-btn>
          </template>
        </v-text-field>
      </div>

      <div class="assistant-sheet__actions">
        <v-btn v-if="reply?.draft" color="primary" size="large" block @click="openSale">Ouvrir la vente</v-btn>
        <div class="assistant-sheet__row">
          <v-btn variant="text" @click="close">Fermer</v-btn>
          <v-btn variant="tonal" color="primary" @click="start">
            <v-icon start>phosphor:microphone</v-icon>
            Reparler
          </v-btn>
        </div>
      </div>
    </v-card>
  </component>
</template>

<style scoped>
.assistant-listening {
  position: fixed;
  left: calc(var(--v-layout-left, 0px) + 16px);
  right: calc(var(--v-layout-right, 0px) + 16px);
  bottom: calc(var(--v-layout-bottom, 0px) + 16px);
  max-width: 440px;
  margin: 0 auto;
  background: rgb(var(--v-theme-surface));
  border-radius: 14px;
  padding: 14px 16px;
  box-shadow: 0 4px 16px rgba(43, 36, 30, 0.25);
  display: flex;
  flex-direction: column;
  gap: 10px;
  z-index: 1010;
}

.assistant-listening__title {
  display: flex;
  align-items: center;
  gap: 10px;
  font-family: 'Zilla Slab', serif;
  font-size: 20px;
}

.assistant-listening__meter {
  height: 8px;
  border-radius: 4px;
  background: rgb(var(--v-theme-status-neutral-container));
  overflow: hidden;
}

/* La jauge suit le volume du micro : elle montre qu'on est entendu. */
.assistant-listening__level {
  height: 100%;
  background: rgb(var(--v-theme-primary));
  transform-origin: left;
  transition: transform 0.1s linear;
}

.assistant-listening__hint {
  font-size: 15px;
}

.assistant-listening__actions,
.assistant-sheet__row {
  display: flex;
  justify-content: space-between;
  gap: 8px;
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
</style>
