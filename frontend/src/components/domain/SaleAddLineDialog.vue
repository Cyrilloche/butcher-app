<script setup lang="ts">
import { ref, watch } from 'vue'
import SaleLineChooser from '@/components/domain/SaleLineChooser.vue'
import { createStockMovement } from '@/api/stockMovements'
import { apiErrorMessage } from '@/composables/useApiError'
import { listSellableLots, type SaleLineDraft, type SellableLot } from '@/composables/useSales'
import type { SaleDto } from '@/api/types'

/**
 * Ajout d'un produit à une vente enregistrée, quand le client demande un complément (RU-03).
 *
 * Même geste que la saisie d'une vente (`SaleLineChooser`), mais la ligne part aussitôt choisie :
 * un produit par ouverture, et la vente relue montre la nouvelle ligne et le nouveau total. Le
 * serveur date la ligne du jour de la vente et en revalide toutes les règles (poids restant,
 * unité encore en stock) ; son refus est affiché tel quel.
 */
const props = defineProps<{ modelValue: boolean; sale: SaleDto }>()
const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  added: []
}>()

const lots = ref<SellableLot[]>([])
const loading = ref(false)
const submitting = ref(false)
const error = ref<string | null>(null)
/** Remonté à chaque ouverture : le choix d'une ouverture précédente ne doit pas réapparaître. */
const chooserKey = ref(0)
const noExclusion = new Set<number>()

// Le stock se relit à chaque ouverture : il a pu bouger depuis la dernière.
watch(
  () => props.modelValue,
  async (open) => {
    if (!open) return
    error.value = null
    chooserKey.value++
    loading.value = true
    try {
      lots.value = await listSellableLots()
    } catch (err) {
      error.value = apiErrorMessage(err, 'Chargement du stock impossible, réessaie.')
    } finally {
      loading.value = false
    }
  },
  { immediate: true },
)

function close() {
  emit('update:modelValue', false)
}

async function add(line: SaleLineDraft) {
  submitting.value = true
  error.value = null
  try {
    await createStockMovement(line.stockUnitId, {
      type: 'sale',
      saleId: props.sale.id,
      isFullSale: line.isFullSale,
      soldWeight: line.weightKg ?? undefined,
      amount: line.amount,
    })
    close()
    emit('added')
  } catch (err) {
    error.value = apiErrorMessage(err, 'Ajout impossible, réessaie.')
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <v-dialog :model-value="modelValue" @update:model-value="(value) => emit('update:modelValue', value)">
    <v-card class="sale-add-line__dialog">
      <h2 class="text-h6 font-weight-bold mb-1">Ajouter un produit</h2>
      <p class="text-secondary sale-add-line__sale">{{ sale.saleNumber }} · {{ sale.customerName }}</p>

      <SaleLineChooser
        v-show="!submitting"
        :key="chooserKey"
        :lots="lots"
        :excluded-ids="noExclusion"
        :loading="loading"
        @add="add"
      />
      <p v-if="submitting" class="text-secondary mb-0">Ajout en cours...</p>

      <p v-if="sale.paid" class="text-secondary sale-add-line__hint">
        La vente reste marquée payée. Si le complément n'est pas encore réglé, passez-la à payer en
        corrigeant la vente.
      </p>

      <p v-if="error" class="sale-add-line__error text-error">{{ error }}</p>

      <div class="sale-add-line__actions">
        <v-btn variant="text" color="secondary" @click="close">Fermer</v-btn>
      </div>
    </v-card>
  </v-dialog>
</template>

<style scoped>
.sale-add-line__dialog {
  padding: 20px;
  border-radius: 16px;
}

.sale-add-line__sale {
  font-size: 15px;
  font-weight: 500;
  margin-bottom: 16px;
}

.sale-add-line__hint {
  font-size: 14px;
  font-weight: 500;
  margin: 12px 0 0;
}

.sale-add-line__error {
  font-size: 14px;
  font-weight: 500;
  margin: 12px 0 0;
}

.sale-add-line__actions {
  display: flex;
  justify-content: flex-end;
  margin-top: 16px;
}
</style>
