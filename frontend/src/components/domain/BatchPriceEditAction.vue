<script setup lang="ts">
import { computed, ref } from 'vue'
import AppTextField from '@/components/base/AppTextField.vue'
import { updateProductionBatch } from '@/api/productionBatches'
import { apiErrorMessage } from '@/composables/useApiError'
import type { StockDetailBatch } from '@/composables/useStock'

/**
 * Correction du prix de vente d'une fournée (RG-10), depuis son en-tête dans le détail stock.
 *
 * Seul le prix est exposé : la référence matière première et la DLC sont reportées en V2
 * (RF-08/RF-09). Le serveur remplace le lot en entier, donc ces champs repartent tels qu'ils ont
 * été lus. Les ventes déjà faites ne bougent pas : leur montant est stocké, jamais recalculé.
 */
const props = defineProps<{ batch: StockDetailBatch }>()
const emit = defineEmits<{ done: [] }>()

const open = ref(false)
const price = ref('')
const submitting = ref(false)
const error = ref<string | null>(null)

const batchLabel = computed(() =>
  props.batch.dayRankLabel
    ? `du ${props.batch.dateLabel}, ${props.batch.dayRankLabel}`
    : `du ${props.batch.dateLabel}`,
)

const canSave = computed(() => Number(price.value) > 0)

function start() {
  price.value = String(props.batch.salePrice)
  error.value = null
  open.value = true
}

async function save() {
  submitting.value = true
  error.value = null
  try {
    await updateProductionBatch(props.batch.id, {
      salePrice: Number(price.value),
      ...props.batch.untouched,
    })
    open.value = false
    emit('done')
  } catch (err) {
    error.value = apiErrorMessage(err, 'Correction impossible, réessaie.')
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <button
    type="button"
    class="batch-price-edit__trigger"
    :aria-label="`Corriger le prix de la fournée ${batchLabel}`"
    @click="start"
  >
    <v-icon size="18">phosphor:pencil-simple</v-icon>
  </button>

  <v-dialog v-model="open">
    <v-card class="batch-price-edit__dialog">
      <h2 class="text-h6 font-weight-bold mb-1">Corriger le prix</h2>
      <p class="text-secondary batch-price-edit__subtitle">Fournée {{ batchLabel }}</p>

      <AppTextField
        v-model="price"
        type="number"
        inputmode="decimal"
        min="0"
        step="0.5"
        label="Prix de vente"
        :suffix="`€ / ${batch.priceUnit}`"
      />
      <p class="text-secondary batch-price-edit__hint">
        Le nouveau prix vaut pour les prochaines ventes. Celles déjà enregistrées gardent leur
        montant.
      </p>

      <p v-if="error" class="batch-price-edit__error text-error">{{ error }}</p>

      <div class="batch-price-edit__actions">
        <v-btn variant="text" color="secondary" @click="open = false">Annuler</v-btn>
        <v-btn
          color="primary"
          variant="flat"
          :disabled="!canSave"
          :loading="submitting"
          @click="save"
          >Enregistrer</v-btn
        >
      </div>
    </v-card>
  </v-dialog>
</template>

<style scoped>
.batch-price-edit__trigger {
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

.batch-price-edit__trigger:hover {
  background: rgb(var(--v-theme-status-neutral-container));
}

.batch-price-edit__dialog {
  padding: 20px;
  border-radius: 16px;
}

.batch-price-edit__subtitle {
  font-size: 15px;
  font-weight: 500;
  margin-bottom: 16px;
}

.batch-price-edit__hint {
  font-size: 14px;
  font-weight: 500;
  margin: 10px 0 16px;
}

.batch-price-edit__error {
  font-size: 14px;
  font-weight: 500;
  margin: 0 0 12px;
}

.batch-price-edit__actions {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
}
</style>
