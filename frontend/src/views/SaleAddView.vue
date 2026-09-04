<!--
  Squelette : pas de statut de paiement (absent du backend, cf.
  docs/data-model.md §9, QM-04) donc pas de section "Payée / À payer" ici.
  Le panier de plusieurs lots n'est pas une "vente" atomique côté backend :
  save() enchaîne un appel POST par lot (un stock_movement chacun),
  partageant le même client — pas de transaction groupée tant que l'entité
  `sale` n'existe pas.
-->
<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import AppPageHeader from '@/components/base/AppPageHeader.vue'
import AppCard from '@/components/base/AppCard.vue'
import AppButton from '@/components/base/AppButton.vue'
import { listCustomers } from '@/api/customers'
import { createStockMovement } from '@/api/stockMovements'
import { listAvailableLots, type AvailableLot } from '@/composables/useSales'
import { useAsyncData } from '@/composables/useAsyncData'
import { customerFullName } from '@/composables/useCustomers'
import { ApiError } from '@/api/http'
import type { CustomerDto } from '@/api/types'

const router = useRouter()

const { data: customers } = useAsyncData(listCustomers, [])
const { data: lots, loading: loadingLots } = useAsyncData(listAvailableLots, [] as AvailableLot[])

const state = reactive({
  customerId: null as number | null,
  clientQuery: '',
  lotQuery: '',
  cart: [] as AvailableLot[],
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

function addToCart(lot: AvailableLot) {
  state.cart.push(lot)
  state.lotQuery = ''
}

function removeFromCart(index: number) {
  state.cart.splice(index, 1)
}

const total = computed(() => state.cart.reduce((sum, l) => sum + l.price, 0))
const canSave = computed(() => !!client.value && state.cart.length > 0)

const saving = ref(false)
const saveError = ref<string | null>(null)

async function save() {
  if (!client.value || !canSave.value) return
  saving.value = true
  saveError.value = null
  try {
    for (const lot of state.cart) {
      await createStockMovement(lot.stockUnitId, {
        type: 'sale',
        isFullSale: true,
        amount: lot.price,
        customerId: client.value.id,
      })
    }
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
          <div v-for="(lot, i) in state.cart" :key="lot.stockUnitId" class="sale-add-view__cart-row">
            <div class="sale-add-view__cart-info">
              <div class="font-weight-medium">{{ lot.productName }}</div>
              <div class="text-secondary">{{ lot.label }} · {{ lot.detail }}</div>
            </div>
            <div class="font-weight-medium">
              {{ lot.price.toLocaleString('fr-FR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) }} €
            </div>
            <v-btn icon variant="text" color="error" size="44" aria-label="Retirer" @click="removeFromCart(i)">
              <v-icon size="20">phosphor:trash</v-icon>
            </v-btn>
          </div>
        </div>

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
            @click="addToCart(lot)"
          >
            <span class="sale-add-view__result-name">{{ lot.label }}</span>
            <span class="text-secondary">{{ lot.detail }}</span>
            <span class="sale-add-view__result-price">
              {{ lot.price.toLocaleString('fr-FR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) }} €
            </span>
          </button>
        </div>
        <p v-else-if="state.lotQuery.trim().length >= 2" class="text-secondary sale-add-view__no-results">
          Aucun lot disponible ne correspond.
        </p>
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
