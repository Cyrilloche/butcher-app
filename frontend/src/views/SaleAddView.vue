<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import AppFormShell from '@/components/base/AppFormShell.vue'
import AppCard from '@/components/base/AppCard.vue'
import { createSale } from '@/api/sales'
import { listSellableLots, type SaleLineDraft, type SellableLot } from '@/composables/useSales'
import { formatWeight } from '@/composables/useStock'
import { useAsyncData } from '@/composables/useAsyncData'
import CustomerPicker from '@/components/domain/CustomerPicker.vue'
import SaleLineChooser from '@/components/domain/SaleLineChooser.vue'
import { ApiError } from '@/api/http'

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
  cart: [] as SaleLineDraft[],
  paid: false,
})

const inCartIds = computed(() => new Set(state.cart.map((l) => l.stockUnitId)))

function removeFromCart(index: number) {
  state.cart.splice(index, 1)
}

const total = computed(() => state.cart.reduce((sum, l) => sum + l.amount, 0))
const canSave = computed(() => state.customerId != null && state.cart.length > 0)

/** Une unité choisie qui attend encore « en entier » ou « une tranche » n'est pas au panier. */
const choosingLine = ref(false)

// RU-01 : un bouton grisé sans explication a été lu comme « vente impossible ». On dit ce qui manque,
// dans l'ordre où l'écran le demande.
const saveHint = computed(() => {
  if (state.customerId == null) return 'Pour enregistrer, choisis le client dans la liste.'
  if (choosingLine.value) return 'Pour enregistrer, termine le choix du produit : en entier ou une tranche.'
  if (state.cart.length === 0) return 'Pour enregistrer, ajoute au moins un produit.'
  return null
})

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
    :hint="saveHint"
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

        <SaleLineChooser
          :lots="lots"
          :excluded-ids="inCartIds"
          :loading="loadingLots"
          @update:pending="(pending) => (choosingLine = pending)"
          @add="(line) => state.cart.push(line)"
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
