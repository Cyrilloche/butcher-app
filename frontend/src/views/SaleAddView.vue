<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import AppPageHeader from '@/components/base/AppPageHeader.vue'
import AppCard from '@/components/base/AppCard.vue'
import AppButton from '@/components/base/AppButton.vue'
import AppTextField from '@/components/base/AppTextField.vue'
import { listCustomers } from '@/api/customers'
import { createSale } from '@/api/sales'
import { listSellableLots, type SellableLot } from '@/composables/useSales'
import { formatWeight } from '@/composables/useStock'
import { useAsyncData } from '@/composables/useAsyncData'
import { customerFullName } from '@/composables/useCustomers'
import { ApiError } from '@/api/http'
import type { CustomerDto } from '@/api/types'

interface CartLine {
  stockUnitId: number
  productName: string
  label: string
  isFullSale: boolean
  /** Kilogrammes, null pour un produit à la pièce. */
  weightKg: number | null
  amount: number
}

const router = useRouter()

const { data: customers } = useAsyncData(listCustomers, [])
const { data: lots, loading: loadingLots } = useAsyncData(listSellableLots, [] as SellableLot[])

const state = reactive({
  customerId: null as number | null,
  clientQuery: '',
  lotQuery: '',
  cart: [] as CartLine[],
  paid: true,
})

const client = computed<CustomerDto | null>(() => customers.value.find((c) => c.id === state.customerId) ?? null)

const clientResults = computed(() => {
  const q = state.clientQuery.trim().toLowerCase()
  if (q.length < 2) return []
  return customers.value.filter((c) => customerFullName(c).toLowerCase().includes(q)).slice(0, 5)
})

function pickClient(id: number) {
  state.customerId = id
  state.clientQuery = ''
}

const inCartIds = computed(() => new Set(state.cart.map((l) => l.stockUnitId)))
const lotResults = computed(() => {
  const q = state.lotQuery.trim().toLowerCase()
  if (q.length < 2) return []
  return lots.value
    .filter((l) => !inCartIds.value.has(l.stockUnitId))
    .filter((l) => l.productName.toLowerCase().includes(q) || l.label.toLowerCase().includes(q))
    .slice(0, 8)
})

// Une unité `opened` (déjà entamée) ou d'un produit `allowPartialSale` demande une
// décision avant d'atterrir dans le panier — les autres s'ajoutent directement.
const pendingLot = ref<SellableLot | null>(null)
const pendingMode = ref<'choice' | 'weight' | null>(null)
const sliceGrams = ref('')

function pickLot(lot: SellableLot) {
  if (lot.status === 'opened') {
    pendingLot.value = lot
    pendingMode.value = 'weight'
  } else if (lot.allowPartialSale) {
    pendingLot.value = lot
    pendingMode.value = 'choice'
  } else {
    addFullSaleToCart(lot)
  }
  state.lotQuery = ''
}

function clearPending() {
  pendingLot.value = null
  pendingMode.value = null
  sliceGrams.value = ''
}

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
  if (!lot || !(grams > 0)) return
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
const canSave = computed(() => !!client.value && state.cart.length > 0)

const saving = ref(false)
const saveError = ref<string | null>(null)

async function save() {
  if (!client.value || !canSave.value) return
  saving.value = true
  saveError.value = null
  try {
    await createSale({
      customerId: client.value.id,
      paid: state.paid,
      lines: state.cart.map((line) => ({
        stockUnitId: line.stockUnitId,
        isFullSale: line.isFullSale,
        soldWeight: line.weightKg ?? undefined,
        amount: line.amount,
      })),
    })
    await router.push('/sales')
  } catch (err) {
    saveError.value = err instanceof ApiError ? err.message : "Erreur lors de l'enregistrement, réessaie."
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <v-container class="sale-add-view">
    <AppPageHeader to="/sales" back-label="Ventes" title="Nouvelle vente" />

    <div class="sale-add-view__sections">
      <AppCard>
        <div class="sale-add-view__section-title text-secondary">1. Le client</div>

        <template v-if="!client">
          <div class="sale-add-view__search">
            <v-icon size="20">phosphor:magnifying-glass</v-icon>
            <input v-model="state.clientQuery" type="text" placeholder="Nom ou téléphone" class="sale-add-view__search-input" />
          </div>
          <div v-if="clientResults.length > 0" class="sale-add-view__results">
            <button
              v-for="c in clientResults"
              :key="c.id"
              type="button"
              class="sale-add-view__result"
              @click="pickClient(c.id)"
            >
              <span class="sale-add-view__result-name">{{ customerFullName(c) }}</span>
              <span class="text-secondary">{{ c.phone }}</span>
            </button>
          </div>
        </template>
        <div v-else class="sale-add-view__client">
          <div class="sale-add-view__client-info">
            <div class="sale-add-view__client-name">{{ customerFullName(client) }}</div>
            <div class="text-secondary">{{ client.phone }}</div>
          </div>
          <button type="button" class="sale-add-view__change" @click="state.customerId = null">Changer</button>
        </div>
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
            <AppButton height="52" @click="pendingMode = 'weight'">Vendre une tranche</AppButton>
          </div>

          <div v-else class="sale-add-view__pending-weight">
            <AppTextField
              v-model="sliceGrams"
              type="number"
              inputmode="numeric"
              min="0"
              label="Poids de la tranche"
              suffix="g"
              hide-details
            />
            <div class="sale-add-view__pending-amount text-secondary">
              {{ sliceAmount > 0 ? `${sliceAmount.toLocaleString('fr-FR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} €` : '—' }}
            </div>
            <AppButton color="primary" height="52" :disabled="!(Number(sliceGrams) > 0)" @click="confirmSlice">
              Ajouter au panier
            </AppButton>
          </div>
        </div>

        <template v-else>
          <div class="sale-add-view__search">
            <v-icon size="20">phosphor:magnifying-glass</v-icon>
            <input
              v-model="state.lotQuery"
              type="text"
              placeholder="Produit ou n° de lot"
              class="sale-add-view__search-input"
            />
          </div>

          <p v-if="loadingLots" class="text-secondary mb-0">Chargement des lots disponibles...</p>
          <div v-else-if="lotResults.length > 0" class="sale-add-view__results">
            <button
              v-for="lot in lotResults"
              :key="lot.stockUnitId"
              type="button"
              class="sale-add-view__result"
              @click="pickLot(lot)"
            >
              <span class="sale-add-view__result-name">
                {{ lot.label }}
                <span v-if="lot.status === 'opened'" class="sale-add-view__result-opened">Entamé</span>
              </span>
              <span class="text-secondary">{{ lot.detail }}</span>
              <span v-if="lot.status === 'available'" class="sale-add-view__result-price">
                {{ lot.price.toLocaleString('fr-FR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) }} €
              </span>
            </button>
          </div>
          <p v-else-if="state.lotQuery.trim().length >= 2" class="text-secondary sale-add-view__no-results">
            Aucun lot disponible ne correspond.
          </p>
        </template>
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

    <div class="sale-add-view__footer">
      <p v-if="saveError" class="sale-add-view__save-error text-error">{{ saveError }}</p>
      <AppButton
        block
        height="60"
        :color="canSave ? 'primary' : undefined"
        :disabled="!canSave || saving"
        @click="save"
      >
        {{ saving ? 'Enregistrement...' : canSave ? `Enregistrer la vente — ${total.toLocaleString('fr-FR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} €` : 'Enregistrer la vente' }}
      </AppButton>
    </div>
  </v-container>
</template>

<style scoped>
.sale-add-view {
  padding-bottom: 110px;
}

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

.sale-add-view__search {
  display: flex;
  align-items: center;
  gap: 8px;
  background: rgb(var(--v-theme-field-surface));
  border: 1.5px solid rgb(var(--v-theme-field-border));
  border-radius: 10px;
  padding: 0 14px;
  height: 52px;
  color: rgb(var(--v-theme-secondary));
  margin-bottom: 10px;
}

.sale-add-view__search-input {
  flex: 1;
  min-width: 0;
  border: none;
  outline: none;
  background: none;
  font-family: var(--font-body);
  font-size: 17px;
  color: rgb(var(--v-theme-on-surface));
  height: 100%;
}

.sale-add-view__results {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.sale-add-view__result {
  display: flex;
  align-items: center;
  gap: 10px;
  border: none;
  background: rgb(var(--v-theme-status-neutral-container));
  border-radius: 10px;
  padding: 10px 14px;
  cursor: pointer;
  font-family: var(--font-body);
  text-align: left;
  min-height: 48px;
}

.sale-add-view__result-name {
  flex: 1;
  min-width: 0;
  font-size: 16px;
  font-weight: 500;
  color: rgb(var(--v-theme-on-surface));
}

.sale-add-view__result-price {
  font-weight: 600;
  color: rgb(var(--v-theme-success));
}

.sale-add-view__result-opened {
  background: rgb(var(--v-theme-warning-container));
  color: rgb(var(--v-theme-warning));
  font-size: 12px;
  font-weight: 600;
  padding: 2px 8px;
  border-radius: 999px;
  margin-left: 6px;
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

.sale-add-view__pending-weight {
  display: flex;
  align-items: flex-end;
  gap: 10px;
}

.sale-add-view__pending-weight > :first-child {
  flex: 1;
}

.sale-add-view__pending-amount {
  font-size: 16px;
  font-weight: 600;
  padding-bottom: 14px;
  white-space: nowrap;
}

.sale-add-view__no-results {
  text-align: center;
  padding: 6px 0;
  margin: 0;
}

.sale-add-view__client {
  display: flex;
  align-items: center;
  gap: 12px;
  background: rgb(var(--v-theme-status-neutral-container));
  border-radius: 10px;
  padding: 12px 14px;
}

.sale-add-view__client-info {
  flex: 1;
  min-width: 0;
}

.sale-add-view__client-name {
  font-family: var(--font-heading);
  font-weight: 600;
  font-size: 19px;
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

.sale-add-view__save-error {
  font-size: 14px;
  font-weight: 500;
  text-align: center;
  margin: 0 0 10px;
}

.sale-add-view__footer {
  position: fixed;
  left: 0;
  right: 0;
  bottom: 56px;
  padding: 14px 16px;
  background: linear-gradient(to top, rgb(var(--v-theme-background)) 70%, transparent);
}
</style>
