<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue'
import AppTextField from '@/components/base/AppTextField.vue'
import { updateStockMovement, deleteStockMovement } from '@/api/stockMovements'
import { apiErrorMessage } from '@/composables/useApiError'
import type { StockMovementDto } from '@/api/types'

/**
 * Correction ou retrait d'une ligne de vente (RG-11), depuis le détail de la vente.
 *
 * Deux champs seulement : le montant réellement encaissé et, pour une vente au poids, le poids
 * vendu. Corriger le poids ne retouche pas le montant : celui-ci est stocké tel qu'il a été saisi
 * et n'est jamais recalculé (RG-05). Le prix au kilo du lot n'est d'ailleurs pas dans le contrat
 * de la ligne, et c'est cohérent — ce calcul n'a pas à vivre ici.
 *
 * Aucune règle n'est répliquée : le poids au-delà de l'unité, le montant nul, la dernière ligne
 * d'une vente sont refusés par le serveur, dont le message est affiché tel quel.
 */
const props = defineProps<{ modelValue: boolean; line: StockMovementDto | null }>()
const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  saved: []
  removed: []
}>()

/** Le poids ne se saisit que sur une ligne qui en porte un, donc sur un produit vendu au poids. */
const hasWeight = computed(() => props.line?.soldWeight != null)

const draft = reactive({ amount: '', grams: '' })
const confirmingRemoval = ref(false)
const submitting = ref(false)
const error = ref<string | null>(null)

watch(
  () => props.modelValue,
  (open) => {
    if (!open || !props.line) return
    draft.amount = props.line.amount != null ? String(props.line.amount) : ''
    draft.grams =
      props.line.soldWeight != null ? String(Math.round(props.line.soldWeight * 1000)) : ''
    confirmingRemoval.value = false
    error.value = null
  },
)

function close() {
  emit('update:modelValue', false)
}

async function save() {
  if (!props.line) return
  submitting.value = true
  error.value = null
  try {
    // Remplacement complet : les trois champs partent, y compris ceux qu'on n'a pas touchés.
    await updateStockMovement(props.line.id, {
      soldWeight: hasWeight.value ? Number(draft.grams) / 1000 : undefined,
      amount: Number(draft.amount),
      notes: props.line.notes ?? undefined,
    })
    close()
    emit('saved')
  } catch (err) {
    error.value = apiErrorMessage(err, 'Correction impossible, réessaie.')
  } finally {
    submitting.value = false
  }
}

async function remove() {
  if (!props.line) return
  submitting.value = true
  error.value = null
  try {
    await deleteStockMovement(props.line.id)
    close()
    emit('removed')
  } catch (err) {
    // Cas nominal du refus : c'était la dernière ligne. Le serveur le dit et renvoie vers la
    // suppression de la vente — on revient au formulaire pour que sa phrase reste lisible.
    error.value = apiErrorMessage(err, 'Retrait impossible, réessaie.')
    confirmingRemoval.value = false
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <v-dialog
    :model-value="modelValue"
    @update:model-value="(value) => emit('update:modelValue', value)"
  >
    <v-card v-if="line" class="sale-line-edit__dialog">
      <template v-if="!confirmingRemoval">
        <h2 class="text-h6 font-weight-bold mb-1">{{ line.productName }}</h2>
        <p class="text-secondary sale-line-edit__unit">{{ line.unitNumber }}</p>

        <AppTextField
          v-if="hasWeight"
          v-model="draft.grams"
          type="number"
          inputmode="numeric"
          label="Poids vendu (g)"
          class="mb-3"
        />
        <AppTextField
          v-model="draft.amount"
          type="number"
          inputmode="decimal"
          step="0.01"
          label="Montant encaissé (€)"
        />
        <p class="text-secondary sale-line-edit__hint">
          Le montant est celui que vous avez réellement encaissé. Il n'est pas recalculé depuis le
          poids.
        </p>

        <p v-if="error" class="sale-line-edit__error text-error">{{ error }}</p>

        <div class="sale-line-edit__actions">
          <v-btn variant="text" color="error" @click="confirmingRemoval = true">Retirer</v-btn>
          <v-spacer />
          <v-btn variant="text" color="secondary" @click="close">Annuler</v-btn>
          <v-btn color="primary" variant="flat" :loading="submitting" @click="save"
            >Enregistrer</v-btn
          >
        </div>
      </template>

      <template v-else>
        <h2 class="text-h6 font-weight-bold mb-2">Retirer cette ligne ?</h2>
        <p class="text-secondary mb-2">
          {{ line.productName }} {{ line.unitNumber }} sortira de cette vente, et le total sera
          diminué d'autant.
        </p>
        <p class="text-secondary sale-line-edit__hint">
          L'unité revient en stock si elle n'a pas d'autre sortie.
        </p>

        <div class="sale-line-edit__actions">
          <v-spacer />
          <v-btn variant="text" color="secondary" @click="confirmingRemoval = false">Annuler</v-btn>
          <v-btn color="error" variant="flat" :loading="submitting" @click="remove">Retirer</v-btn>
        </div>
      </template>
    </v-card>
  </v-dialog>
</template>

<style scoped>
.sale-line-edit__dialog {
  padding: 20px;
  border-radius: 16px;
}

.sale-line-edit__unit {
  font-size: 15px;
  font-weight: 500;
  margin-bottom: 16px;
}

.sale-line-edit__hint {
  font-size: 14px;
  font-weight: 500;
  margin: 10px 0 16px;
}

.sale-line-edit__error {
  font-size: 14px;
  font-weight: 500;
  margin: 0 0 12px;
}

.sale-line-edit__actions {
  display: flex;
  align-items: center;
  gap: 8px;
}
</style>
