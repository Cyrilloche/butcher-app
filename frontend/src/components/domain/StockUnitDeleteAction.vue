<script setup lang="ts">
import { computed, ref } from 'vue'
import { deleteStockUnit } from '@/api/stockUnits'
import { apiErrorMessage } from '@/composables/useApiError'
import type { StockDetailUnit } from '@/composables/useStock'

/**
 * Suppression d'une unité saisie par erreur, depuis sa ligne dans le détail stock.
 *
 * Le pendant, à l'unité près, de la suppression d'une fournée : on efface une erreur de pesée, pas
 * un événement de gestion. Un sachet perdu ou consommé se déclare depuis le menu de la ligne, ce
 * qui garde sa trace ; le supprimer, lui, ne laisse rien.
 *
 * Le bouton s'éteint dès que l'unité n'est plus « disponible ». Tout mouvement — vente, perso,
 * perte — la fait quitter ce statut, donc « disponible » vaut « sans mouvement ». Ce n'est qu'une
 * politesse : le serveur refuse de toute façon (409), et c'est lui qui fait foi.
 *
 * Le numéro d'étiquette de l'unité supprimée n'est jamais réattribué (§3.9 du modèle de données) —
 * la confirmation le dit, parce que l'étiquette manuscrite, elle, peut déjà exister.
 */
const props = defineProps<{ unit: StockDetailUnit }>()
const emit = defineEmits<{ done: []; failed: [message: string] }>()

const confirming = ref(false)
const submitting = ref(false)

const canDelete = computed(() => props.unit.status === 'available')

async function confirm() {
  submitting.value = true
  try {
    await deleteStockUnit(props.unit.id)
    confirming.value = false
    emit('done')
  } catch (err) {
    emit('failed', apiErrorMessage(err, 'Suppression impossible, réessaie.'))
    confirming.value = false
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <button
    type="button"
    class="unit-delete__trigger"
    :disabled="!canDelete"
    :aria-label="
      canDelete
        ? `Supprimer l'unité ${unit.number}`
        : `L'unité ${unit.number} a déjà une sortie et ne peut pas être supprimée`
    "
    @click="confirming = true"
  >
    <v-icon size="18">phosphor:trash</v-icon>
  </button>

  <v-dialog v-model="confirming">
    <v-card class="unit-delete__dialog">
      <h2 class="text-h6 font-weight-bold mb-2">Supprimer cette unité ?</h2>
      <p class="text-secondary mb-2">
        L'unité {{ unit.number }}<span v-if="unit.weightLabel"> ({{ unit.weightLabel }})</span>
        disparaîtra du stock.
      </p>
      <p class="text-secondary unit-delete__hint">
        À réserver à une erreur de pesée. C'est définitif, et son numéro d'étiquette ne sera jamais
        réattribué.
      </p>

      <div class="unit-delete__actions">
        <v-btn variant="text" color="secondary" @click="confirming = false">Annuler</v-btn>
        <v-btn color="error" variant="flat" :loading="submitting" @click="confirm">Supprimer</v-btn>
      </div>
    </v-card>
  </v-dialog>
</template>

<style scoped>
.unit-delete__trigger {
  width: 38px;
  height: 38px;
  display: flex;
  align-items: center;
  justify-content: center;
  border: none;
  background: none;
  color: rgb(var(--v-theme-secondary));
  border-radius: 10px;
  cursor: pointer;
  flex-shrink: 0;
}

.unit-delete__trigger:hover:not(:disabled) {
  background: rgb(var(--v-theme-status-neutral-container));
}

.unit-delete__trigger:disabled {
  opacity: 0.28;
  cursor: default;
}

.unit-delete__dialog {
  padding: 20px;
  border-radius: 16px;
}

.unit-delete__hint {
  font-size: 14px;
  font-weight: 500;
  margin-bottom: 16px;
}

.unit-delete__actions {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
}
</style>
