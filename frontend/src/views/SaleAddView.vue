<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import AppFormShell from '@/components/base/AppFormShell.vue'
import AppCard from '@/components/base/AppCard.vue'
import AppButton from '@/components/base/AppButton.vue'
import AppTextField from '@/components/base/AppTextField.vue'
import { createSale } from '@/api/sales'
import { listSellableLots, type SellableLot } from '@/composables/useSales'
import { formatWeight } from '@/composables/useStock'
import { useAsyncData } from '@/composables/useAsyncData'
import CustomerPicker from '@/components/domain/CustomerPicker.vue'
import SellableLotSearch from '@/components/domain/SellableLotSearch.vue'
import { ApiError } from '@/api/http'

interface CartLine {
  stockUnitId: number
  productName: string
  label: string
  isFullSale: boolean
  /** Kilogrammes, null pour un produit à la pièce. */
  weightKg: number | null
  amount: number
}

/**
 * `dialog` : ouvert en fenêtre depuis la liste (écran large), qui se recharge sur `saved`.
 * `customerId` : vente lancée depuis la fiche d'un client (RU-04). Le client est déjà choisi, reste
 * modifiable, et la page ramène à sa fiche plutôt qu'à la liste des ventes.
 */
const props = defineProps<{ dialog?: boolean; customerId?: number }>()
const emit = defineEmits<{ saved: []; cancel: [] }>()

const router = useRouter()

const backTo = props.customerId != null ? `/customers/${props.customerId}` : '/sales'
const backLabel = props.customerId != null ? 'Client' : 'Ventes'

const { data: lots, loading: loadingLots } = useAsyncData(listSellableLots, [] as SellableLot[])

const state = reactive({
  customerId: props.customerId ?? (null as number | null),
  cart: [] as CartLine[],
  paid: true,
})

const inCartIds = computed(() => new Set(state.cart.map((l) => l.stockUnitId)))

// Une unité `opened` (déjà entamée) ou d'un produit `allowPartialSale` demande une
// décision avant d'atterrir dans le panier — les autres s'ajoutent directement.
const pendingLot = ref<SellableLot | null>(null)
const pendingMode = ref<'choice' | 'weight' | null>(null)
const sliceGrams = ref('')
/** Poids encore vendable (kg) sur l'unité en cours, tel que le serveur l'a calculé (RG-05).
 *  Le garde-fou serveur revalide à l'écriture : c'est lui qui fait foi. */
const remainingWeightKg = ref<number | null>(null)

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
    addFullSaleToCart(lot)
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

function addFullSaleToCart(lot: SellableLot) {
  state.cart.push({
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
  state.cart.push({
    stockUnitId: lot.stockUnitId,
    productName: lot.productName,
    label: lot.label,
    isFullSale: false,
    weightKg: grams / 1000,
    amount: sliceAmount.value,
  })
  clearPending()
}

function removeFromCart(index: number) {
  state.cart.splice(index, 1)
}

const total = computed(() => state.cart.reduce((sum, l) => sum + l.amount, 0))
const canSave = computed(() => state.customerId != null && state.cart.length > 0)

const saving = ref(false)
const saveError = ref<string | null>(null)

async function save() {
  if (state.customerId == null || !canSave.value) return
  saving.value = true
  saveError.value = null
  try {
    await createSale({
      customerId: state.customerId,
      paid: state.paid,
      lines: state.cart.map((line) => ({
        stockUnitId: line.stockUnitId,
        isFullSale: line.isFullSale,
        soldWeight: line.weightKg ?? undefined,
        amount: line.amount,
      })),
    })
    if (props.dialog) emit('saved')
    else await router.push(backTo)
  } catch (err) {
    saveError.value = err instanceof ApiError ? err.message : "Erreur lors de l'enregistrement, réessaie."
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <AppFormShell
    title="Nouvelle vente"
    :back-to="backTo"
    :back-label="backLabel"
    :dialog="dialog"
    :save-label="saving ? 'Enregistrement...' : canSave ? `Enregistrer la vente — ${total.toLocaleString('fr-FR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} €` : 'Enregistrer la vente'"
    :can-save="canSave"
    :saving="saving"
    :error="saveError"
    @save="save"
    @cancel="emit('cancel')"
  >

    <div class="sale-add-view__sections">
      <AppCard>
        <div class="sale-add-view__section-title text-secondary">1. Le client</div>

        <CustomerPicker v-model="state.customerId" />
      </AppCard>

      <AppCard>
        <div class="sale-add-view__section-title text-secondary">2. Les lots vendus</div>

        <div v-if="state.cart.length > 0" class="sale-add-view__cart">
          <div v-for="(line, i) in state.cart" :key="line.stockUnitId" class="sale-add-view__cart-row">
            <div class="sale-add-view__cart-info">
              <div class="font-weight-medium">{{ line.productName }}</div>
              <div class="text-secondary">
                {{ line.label }}
                <span v-if="!line.isFullSale">· tranche, {{ formatWeight(Math.round((line.weightKg ?? 0) * 1000)) }}</span>
              </div>
            </div>
            <div class="font-weight-medium">
              {{ line.amount.toLocaleString('fr-FR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) }} €
            </div>
            <v-btn icon variant="text" color="error" size="44" aria-label="Retirer" @click="removeFromCart(i)">
              <v-icon size="20">phosphor:trash</v-icon>
            </v-btn>
          </div>
        </div>

        <!-- Unité opened, ou available d'un produit vendable à la tranche : décision à prendre. -->
        <div v-if="pendingLot" class="sale-add-view__pending">
          <div class="sale-add-view__pending-header">
            <div>
              <div class="font-weight-medium">{{ pendingLot.productName }}</div>
              <div class="text-secondary">{{ pendingLot.label }} · {{ pendingLot.detail }}</div>
            </div>
            <button type="button" class="sale-add-view__change" @click="clearPending">Annuler</button>
          </div>

          <div v-if="pendingMode === 'choice'" class="sale-add-view__pending-choice">
            <AppButton color="primary" height="52" @click="addFullSaleToCart(pendingLot)">
              Vendre en entier — {{ pendingLot.price.toLocaleString('fr-FR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) }} €
            </AppButton>
            <AppButton height="52" @click="startSlice">Vendre une tranche</AppButton>
          </div>

          <div v-else class="sale-add-view__pending-weight-block">
            <p v-if="remainingWeightKg != null" class="sale-add-view__remaining" :class="{ 'text-error': exceedsRemaining }">
              Poids restant : {{ formatWeight(Math.round(remainingWeightKg * 1000)) }}
            </p>

            <!-- Libellé hors de la rangée : dans le flex, il se faisait écraser mot par mot. -->
            <label for="slice-grams" class="sale-add-view__pending-label">Poids de la tranche</label>
            <div class="sale-add-view__pending-weight">
              <AppTextField
                id="slice-grams"
                v-model="sliceGrams"
                type="number"
                inputmode="numeric"
                min="0"
                suffix="g"
                hide-details
              />
              <div class="sale-add-view__pending-amount text-secondary">
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

            <p v-if="exceedsRemaining" class="text-error sale-add-view__remaining-warning">
              Ce poids dépasse le poids restant estimé sur cette unité.
            </p>
          </div>
        </div>

        <!-- Masquée et non démontée pendant une décision : la saisie en cours y est gardée. -->
        <SellableLotSearch
          v-show="!pendingLot"
          :lots="lots"
          :excluded-ids="inCartIds"
          :loading="loadingLots"
          @pick="pickLot"
        />
      </AppCard>

      <AppCard>
        <div class="sale-add-view__section-title text-secondary">3. Le paiement</div>
        <div class="sale-add-view__payment">
          <button
            type="button"
            class="sale-add-view__payment-option"
            :class="{ 'sale-add-view__payment-option--paid': state.paid }"
            @click="state.paid = true"
          >
            <v-icon size="22">phosphor:check-circle</v-icon>
            Payée
          </button>
          <button
            type="button"
            class="sale-add-view__payment-option"
            :class="{ 'sale-add-view__payment-option--pending': !state.paid }"
            @click="state.paid = false"
          >
            <v-icon size="22">phosphor:clock</v-icon>
            À payer
          </button>
        </div>
      </AppCard>
    </div>

  </AppFormShell>
</template>

<style scoped>
.sale-add-view__sections {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.sale-add-view__section-title {
  font-size: 16px;
  font-weight: 600;
  margin-bottom: 12px;
}

.sale-add-view__pending {
  display: flex;
  flex-direction: column;
  gap: 12px;
  background: rgb(var(--v-theme-status-neutral-container));
  border-radius: 10px;
  padding: 14px;
  margin-bottom: 10px;
}

.sale-add-view__pending-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 10px;
}

.sale-add-view__pending-choice {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.sale-add-view__pending-weight-block {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.sale-add-view__remaining {
  font-size: 14px;
  font-weight: 500;
  margin: 0;
}

.sale-add-view__remaining-warning {
  font-size: 14px;
  font-weight: 500;
  margin: 0;
}

.sale-add-view__pending-weight {
  display: flex;
  align-items: flex-end;
  gap: 10px;
}

.sale-add-view__pending-label {
  font-size: 15px;
  font-weight: 500;
  color: rgb(var(--v-theme-on-surface));
}

.sale-add-view__pending-weight > :first-child {
  flex: 1;
  min-width: 0;
}

.sale-add-view__pending-amount {
  font-size: 16px;
  font-weight: 600;
  padding-bottom: 14px;
  white-space: nowrap;
}

.sale-add-view__change {
  border: none;
  background: none;
  color: rgb(var(--v-theme-primary));
  font-size: 15px;
  font-weight: 600;
  cursor: pointer;
  font-family: var(--font-body);
  min-height: 44px;
}

.sale-add-view__cart {
  display: flex;
  flex-direction: column;
  gap: 6px;
  margin-bottom: 10px;
}

.sale-add-view__cart-row {
  display: flex;
  align-items: center;
  gap: 10px;
  background: rgb(var(--v-theme-field-surface));
  border: 1.5px solid rgb(var(--v-theme-field-border));
  border-radius: 10px;
  padding: 8px 4px 8px 14px;
}

.sale-add-view__cart-info {
  flex: 1;
  min-width: 0;
}

.sale-add-view__payment {
  display: flex;
  gap: 10px;
}

.sale-add-view__payment-option {
  flex: 1;
  border: 2px solid rgb(var(--v-theme-field-border));
  background: rgb(var(--v-theme-field-surface));
  color: rgb(var(--v-theme-on-surface));
  font-family: var(--font-body);
  font-size: 16px;
  font-weight: 500;
  padding: 12px 10px;
  border-radius: 12px;
  cursor: pointer;
  min-height: 52px;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
}

.sale-add-view__payment-option--paid {
  border-color: rgb(var(--v-theme-success));
  background: rgb(var(--v-theme-success));
  color: rgb(var(--v-theme-surface));
}

.sale-add-view__payment-option--pending {
  border-color: rgb(var(--v-theme-warning));
  background: rgb(var(--v-theme-warning-container));
  color: rgb(var(--v-theme-warning));
}
</style>
