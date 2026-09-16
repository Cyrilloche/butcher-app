<!--
  Choix d'une ligne de vente : recherche de l'unité, puis, pour un jambon, vente en entier ou à la
  tranche et son poids. Émet la ligne prête, montant pré-calculé.

  Partagé par la saisie d'une vente, qui la met au panier, et par le détail d'une vente, qui
  l'ajoute à la vente enregistrée (RU-03) : les deux offrent le même geste.
-->
<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import AppButton from '@/components/base/AppButton.vue'
import AppTextField from '@/components/base/AppTextField.vue'
import SellableLotSearch from '@/components/domain/SellableLotSearch.vue'
import { formatWeight } from '@/composables/useStock'
import type { SaleLineDraft, SellableLot } from '@/composables/useSales'

defineProps<{
  lots: SellableLot[]
  /** Unités déjà choisies, à ne plus proposer. */
  excludedIds: Set<number>
  loading: boolean
}>()
const emit = defineEmits<{
  add: [line: SaleLineDraft]
  /** Vrai tant qu'une unité attend sa décision (en entier, tranche) : elle n'est pas encore ajoutée. */
  'update:pending': [pending: boolean]
}>()

// Une unité `opened` (déjà entamée) ou d'un produit `allowPartialSale` demande une
// décision avant d'être ajoutée — les autres le sont directement.
const pendingLot = ref<SellableLot | null>(null)
const pendingMode = ref<'choice' | 'weight' | null>(null)
const sliceGrams = ref('')
/** Poids encore vendable (kg) sur l'unité en cours, tel que le serveur l'a calculé (RG-05).
 *  Le garde-fou serveur revalide à l'écriture : c'est lui qui fait foi. */
const remainingWeightKg = ref<number | null>(null)

watch(pendingLot, (lot) => emit('update:pending', lot != null))

function loadRemainingWeight(lot: SellableLot) {
  remainingWeightKg.value = lot.remainingWeight
}

function pickLot(lot: SellableLot) {
  if (lot.status === 'opened') {
    pendingLot.value = lot
    pendingMode.value = 'weight'
    loadRemainingWeight(lot)
  } else if (lot.allowPartialSale) {
    pendingLot.value = lot
    pendingMode.value = 'choice'
  } else {
    addFullSale(lot)
  }
}

function startSlice() {
  if (!pendingLot.value) return
  pendingMode.value = 'weight'
  loadRemainingWeight(pendingLot.value)
}

function clearPending() {
  pendingLot.value = null
  pendingMode.value = null
  sliceGrams.value = ''
  remainingWeightKg.value = null
}

const exceedsRemaining = computed(() => {
  const grams = Number(sliceGrams.value)
  if (remainingWeightKg.value == null || !(grams > 0)) return false
  return grams / 1000 > remainingWeightKg.value
})

function addFullSale(lot: SellableLot) {
  emit('add', {
    stockUnitId: lot.stockUnitId,
    productName: lot.productName,
    label: lot.label,
    isFullSale: true,
    weightKg: lot.weight,
    amount: lot.price,
  })
  clearPending()
}

const sliceAmount = computed(() => {
  const grams = Number(sliceGrams.value)
  if (!pendingLot.value?.pricePerKg || !(grams > 0)) return 0
  return Math.round((grams / 1000) * pendingLot.value.pricePerKg * 100) / 100
})

function confirmSlice() {
  const lot = pendingLot.value
  const grams = Number(sliceGrams.value)
  if (!lot || !(grams > 0) || exceedsRemaining.value) return
  emit('add', {
    stockUnitId: lot.stockUnitId,
    productName: lot.productName,
    label: lot.label,
    isFullSale: false,
    weightKg: grams / 1000,
    amount: sliceAmount.value,
  })
  clearPending()
}
</script>

<template>
  <div class="sale-line-chooser">
    <!-- Unité opened, ou available d'un produit vendable à la tranche : décision à prendre. -->
    <div v-if="pendingLot" class="sale-line-chooser__pending">
      <div class="sale-line-chooser__pending-header">
        <div>
          <div class="font-weight-medium">{{ pendingLot.productName }}</div>
          <div class="text-secondary">{{ pendingLot.label }} · {{ pendingLot.detail }}</div>
        </div>
        <button type="button" class="sale-line-chooser__change" @click="clearPending">Annuler</button>
      </div>

      <div v-if="pendingMode === 'choice'" class="sale-line-chooser__pending-choice">
        <AppButton color="primary" height="52" @click="addFullSale(pendingLot)">
          Vendre en entier — {{ pendingLot.price.toLocaleString('fr-FR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) }} €
        </AppButton>
        <AppButton height="52" @click="startSlice">Vendre une tranche</AppButton>
      </div>

      <div v-else class="sale-line-chooser__pending-weight-block">
        <p v-if="remainingWeightKg != null" class="sale-line-chooser__remaining" :class="{ 'text-error': exceedsRemaining }">
          Poids restant : {{ formatWeight(Math.round(remainingWeightKg * 1000)) }}
        </p>

        <!-- Libellé hors de la rangée : dans le flex, il se faisait écraser mot par mot. -->
        <label for="slice-grams" class="sale-line-chooser__pending-label">Poids de la tranche</label>
        <div class="sale-line-chooser__pending-weight">
          <AppTextField
            id="slice-grams"
            v-model="sliceGrams"
            type="number"
            inputmode="numeric"
            min="0"
            suffix="g"
            hide-details
          />
          <div class="sale-line-chooser__pending-amount text-secondary">
            {{ sliceAmount > 0 ? `${sliceAmount.toLocaleString('fr-FR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} €` : '—' }}
          </div>
          <AppButton
            color="primary"
            height="52"
            :disabled="!(Number(sliceGrams) > 0) || exceedsRemaining"
            @click="confirmSlice"
          >
            Ajouter
          </AppButton>
        </div>

        <p v-if="exceedsRemaining" class="text-error sale-line-chooser__remaining-warning">
          Ce poids dépasse le poids restant estimé sur cette unité.
        </p>
      </div>
    </div>

    <!-- Masquée et non démontée pendant une décision : la saisie en cours y est gardée. -->
    <SellableLotSearch
      v-show="!pendingLot"
      :lots="lots"
      :excluded-ids="excludedIds"
      :loading="loading"
      @pick="pickLot"
    />
  </div>
</template>

<style scoped>
.sale-line-chooser__pending {
  display: flex;
  flex-direction: column;
  gap: 12px;
  background: rgb(var(--v-theme-status-neutral-container));
  border-radius: 10px;
  padding: 14px;
  margin-bottom: 10px;
}

.sale-line-chooser__pending-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 10px;
}

.sale-line-chooser__pending-choice {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.sale-line-chooser__pending-weight-block {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.sale-line-chooser__remaining {
  font-size: 14px;
  font-weight: 500;
  margin: 0;
}

.sale-line-chooser__remaining-warning {
  font-size: 14px;
  font-weight: 500;
  margin: 0;
}

.sale-line-chooser__pending-weight {
  display: flex;
  align-items: flex-end;
  gap: 10px;
}

.sale-line-chooser__pending-label {
  font-size: 15px;
  font-weight: 500;
  color: rgb(var(--v-theme-on-surface));
}

.sale-line-chooser__pending-weight > :first-child {
  flex: 1;
  min-width: 0;
}

.sale-line-chooser__pending-amount {
  font-size: 16px;
  font-weight: 600;
  padding-bottom: 14px;
  white-space: nowrap;
}

.sale-line-chooser__change {
  border: none;
  background: none;
  color: rgb(var(--v-theme-primary));
  font-size: 15px;
  font-weight: 600;
  cursor: pointer;
  font-family: var(--font-body);
  min-height: 44px;
}
</style>
