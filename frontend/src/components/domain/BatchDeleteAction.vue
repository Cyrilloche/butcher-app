<script setup lang="ts">
import { computed, ref } from 'vue'
import { deleteProductionBatch } from '@/api/productionBatches'
import { ApiError } from '@/api/http'
import type { StockDetailBatch } from '@/composables/useStock'

/**
 * Suppression d'une fournée créée par erreur, depuis son en-tête dans le détail stock.
 *
 * C'est la soupape qui rend vivable le gel du code produit : sans elle, une fournée créée par
 * mégarde condamne le produit à garder un code erroné. Le serveur refuse (409) dès qu'une de ses
 * unités est déjà sortie du stock — l'interface n'anticipe pas ce refus, elle l'affiche.
 *
 * La fournée n'a pas de numéro : on la désigne par sa date de fabrication et, si plusieurs
 * fournées partagent cette date, par son rang dans la journée.
 */
const props = defineProps<{ batch: StockDetailBatch }>()
const emit = defineEmits<{ done: []; failed: [message: string] }>()

const confirming = ref(false)
const submitting = ref(false)

const unitCount = computed(() => props.batch.units.length)

/** « du 10/09 » ou « du 10/09, 2ᵉ fournée » : la fournée se nomme sans numéro. */
const batchLabel = computed(() =>
  props.batch.dayRankLabel
    ? `du ${props.batch.dateLabel}, ${props.batch.dayRankLabel}`
    : `du ${props.batch.dateLabel}`,
)

async function confirm() {
  submitting.value = true
  try {
    await deleteProductionBatch(props.batch.id)
    confirming.value = false
    emit('done')
  } catch (err) {
    emit('failed', err instanceof ApiError ? err.message : 'Erreur, réessaie.')
    confirming.value = false
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <button
    type="button"
    class="batch-delete__trigger"
    :aria-label="`Supprimer la fournée ${batchLabel}`"
    @click="confirming = true"
  >
    <v-icon size="18">phosphor:trash</v-icon>
  </button>

  <v-dialog v-model="confirming" max-width="380">
    <v-card class="batch-delete__dialog">
      <h2 class="text-h6 font-weight-bold mb-2">Supprimer cette fournée ?</h2>
      <p class="text-secondary mb-2">
        La fabrication {{ batchLabel }} et
        {{ unitCount === 1 ? 'son unité' : `ses ${unitCount} unités` }} disparaîtront du stock.
      </p>
      <p class="text-secondary batch-delete__hint">
        À réserver à une fournée créée par erreur. C'est définitif, et les numéros de ses étiquettes
        ne seront jamais réattribués.
      </p>

      <div class="batch-delete__actions">
        <v-btn variant="text" color="secondary" @click="confirming = false">Annuler</v-btn>
        <v-btn color="error" variant="flat" :loading="submitting" @click="confirm">Supprimer</v-btn>
      </div>
    </v-card>
  </v-dialog>
</template>

<style scoped>
.batch-delete__trigger {
  width: 32px;
  height: 32px;
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

.batch-delete__trigger:hover {
  background: rgb(var(--v-theme-status-neutral-container));
}

.batch-delete__dialog {
  padding: 20px;
  border-radius: 16px;
}

.batch-delete__hint {
  font-size: 14px;
  font-weight: 500;
  margin-bottom: 16px;
}

.batch-delete__actions {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
}
</style>
