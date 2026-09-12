<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import AppPageHeader from '@/components/base/AppPageHeader.vue'
import AppCard from '@/components/base/AppCard.vue'
import AppButton from '@/components/base/AppButton.vue'
import AppTextField from '@/components/base/AppTextField.vue'
import AuthorLabel from '@/components/domain/AuthorLabel.vue'
import CustomerPicker from '@/components/domain/CustomerPicker.vue'
import SaleDeleteAction from '@/components/domain/SaleDeleteAction.vue'
import SaleLineEditDialog from '@/components/domain/SaleLineEditDialog.vue'
import { getSale, setSalePayment, updateSale } from '@/api/sales'
import { useAsyncData } from '@/composables/useAsyncData'
import { apiErrorMessage } from '@/composables/useApiError'
import { formatWeight } from '@/composables/useStock'
import type { StockMovementDto } from '@/api/types'

const props = defineProps<{ id: string }>()
const saleId = computed(() => Number(props.id))
const router = useRouter()

const { data: sale, loading, error, reload } = useAsyncData(() => getSale(saleId.value), null)

const lineViews = computed(
  () =>
    sale.value?.lines.map((movement) => ({
      movement,
      detail: `${movement.unitNumber} · ${movement.soldWeight != null ? formatWeight(Math.round(movement.soldWeight * 1000)) : 'À la pièce'}`,
    })) ?? [],
)

function formatEuros(value: number): string {
  return value.toLocaleString('fr-FR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

/** Date du jour civil, telle que l'utilisateur la lit, pour un champ `<input type="date">`. */
function toDateInput(iso: string): string {
  const d = new Date(iso)
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`
}

/** Rejoue la date saisie sur l'horodatage d'origine : on corrige le jour, pas l'heure. */
function toIsoKeepingTime(iso: string, dateInput: string): string {
  const [year = 0, month = 1, day = 1] = dateInput.split('-').map(Number)
  const d = new Date(iso)
  d.setFullYear(year, month - 1, day)
  return d.toISOString()
}

// --- Correction de l'en-tête (RG-14) ---------------------------------------------------------
// Mode explicite plutôt que champs perpétuellement ouverts : une vente se relit bien plus souvent
// qu'elle ne se corrige, et « Annuler » doit être un geste visible.

const editing = ref(false)
const draft = reactive({ customerId: null as number | null, date: '', paid: false, notes: '' })
const savingHeader = ref(false)
const headerError = ref<string | null>(null)

function openEdit() {
  if (!sale.value) return
  draft.customerId = sale.value.customerId
  draft.date = toDateInput(sale.value.date)
  draft.paid = sale.value.paid
  draft.notes = sale.value.notes ?? ''
  headerError.value = null
  editing.value = true
}

/** Abandon : le brouillon est jeté, aucun appel n'est parti, la vente est inchangée. */
function cancelEdit() {
  editing.value = false
  headerError.value = null
}

const canSaveHeader = computed(() => draft.customerId != null && draft.date.length > 0)

async function saveHeader() {
  if (!sale.value || !canSaveHeader.value || draft.customerId == null) return
  savingHeader.value = true
  headerError.value = null
  try {
    await updateSale(sale.value.id, {
      customerId: draft.customerId,
      date: toIsoKeepingTime(sale.value.date, draft.date),
      paid: draft.paid,
      notes: draft.notes.trim() || undefined,
    })
    await reload()
    editing.value = false
  } catch (err) {
    // Le refus reste à l'écran avec la saisie : un client supprimé entre-temps, une vente
    // disparue. On ne ferme pas le mode édition, sinon la correction serait perdue.
    headerError.value = apiErrorMessage(err, 'Correction impossible, réessaie.')
  } finally {
    savingHeader.value = false
  }
}

// --- Bascule rapide du paiement, depuis l'écran de lecture ------------------------------------

const togglingPayment = ref(false)
const paymentError = ref<string | null>(null)

async function markPaid() {
  if (!sale.value) return
  togglingPayment.value = true
  paymentError.value = null
  try {
    await setSalePayment(sale.value.id, { paid: true })
    await reload()
  } catch (err) {
    // Sans ça, un refus (vente supprimée ailleurs, serveur injoignable) se soldait par un bouton
    // qui s'éteint sans rien dire : l'utilisateur croyait la vente payée.
    paymentError.value = apiErrorMessage(err, 'Changement impossible, réessaie.')
  } finally {
    togglingPayment.value = false
  }
}

// --- Correction d'une ligne (RG-11) -----------------------------------------------------------

const editedLine = ref<StockMovementDto | null>(null)
const lineDialogOpen = ref(false)

function openLine(movement: StockMovementDto) {
  editedLine.value = movement
  lineDialogOpen.value = true
}

/** Toute correction repasse par le serveur : le total affiché est celui qu'il renvoie (RG-05). */
async function onLineChanged() {
  await reload()
}

async function onSaleDeleted() {
  await router.push('/sales')
}
</script>

<template>
  <v-container v-if="loading" class="sale-detail-view">
    <p class="text-secondary">Chargement...</p>
  </v-container>

  <v-container v-else-if="error" class="sale-detail-view">
    <p class="text-error">{{ error }}</p>
  </v-container>

  <v-container v-else-if="sale" class="sale-detail-view">
    <AppPageHeader to="/sales" back-label="Ventes" :title="sale.saleNumber">
      <template #badge>
        <span
          :class="sale.paid ? 'sale-detail-view__badge--paid' : 'sale-detail-view__badge--pending'"
          class="sale-detail-view__badge"
        >
          {{ sale.paid ? 'Payée' : 'À payer' }}
        </span>
      </template>
    </AppPageHeader>
    <div class="sale-detail-view__date text-secondary">
      {{ new Date(sale.date).toLocaleDateString('fr-FR', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' }) }}
    </div>
    <AuthorLabel :name="sale.createdByName" class="sale-detail-view__author" />

    <div class="sale-detail-view__sections">
      <!-- En-tête : lecture, puis correction sur demande -->
      <template v-if="!editing">
        <RouterLink :to="`/customers/${sale.customerId}`" class="sale-detail-view__customer">
          <div class="sale-detail-view__customer-info">
            <div class="sale-detail-view__customer-label text-secondary">Client</div>
            <div class="sale-detail-view__customer-name">{{ sale.customerName }}</div>
          </div>
          <v-icon size="16">phosphor:caret-right</v-icon>
        </RouterLink>

        <AppCard v-if="sale.notes">
          <div class="sale-detail-view__section-title text-secondary">Note</div>
          <p class="sale-detail-view__notes">{{ sale.notes }}</p>
        </AppCard>
      </template>

      <AppCard v-else>
        <div class="sale-detail-view__section-title text-secondary">Corriger la vente</div>

        <div class="sale-detail-view__field-label">Client</div>
        <CustomerPicker v-model="draft.customerId" />

        <AppTextField v-model="draft.date" type="date" label="Date de la vente" class="mt-3" />

        <div class="sale-detail-view__field-label mt-3">Paiement</div>
        <div class="sale-detail-view__payment">
          <button
            type="button"
            class="sale-detail-view__payment-option"
            :class="{ 'sale-detail-view__payment-option--paid': draft.paid }"
            @click="draft.paid = true"
          >
            <v-icon size="20">phosphor:check-circle</v-icon>
            Payée
          </button>
          <button
            type="button"
            class="sale-detail-view__payment-option"
            :class="{ 'sale-detail-view__payment-option--pending': !draft.paid }"
            @click="draft.paid = false"
          >
            <v-icon size="20">phosphor:clock</v-icon>
            À payer
          </button>
        </div>

        <AppTextField v-model="draft.notes" label="Note (facultatif)" class="mt-3" />

        <p v-if="headerError" class="sale-detail-view__error text-error">{{ headerError }}</p>

        <div class="sale-detail-view__edit-actions">
          <v-btn variant="text" color="secondary" @click="cancelEdit">Annuler</v-btn>
          <v-btn
            color="primary"
            variant="flat"
            :disabled="!canSaveHeader"
            :loading="savingHeader"
            @click="saveHeader"
          >
            Enregistrer
          </v-btn>
        </div>
      </AppCard>

      <!-- Les lignes : un appui ouvre la correction de la ligne -->
      <AppCard>
        <div class="sale-detail-view__section-title text-secondary">
          {{ sale.itemCount }} article{{ sale.itemCount > 1 ? 's' : '' }} vendu{{ sale.itemCount > 1 ? 's' : '' }}
        </div>
        <button
          v-for="line in lineViews"
          :key="line.movement.id"
          type="button"
          class="sale-detail-view__line"
          :aria-label="`Corriger la ligne ${line.movement.productName} ${line.movement.unitNumber}`"
          @click="openLine(line.movement)"
        >
          <div class="sale-detail-view__line-info">
            <div class="font-weight-medium">
              {{ line.movement.productName }}
              <span v-if="!line.movement.productIsActive" class="text-secondary sale-detail-view__retired">
                (produit désactivé)
              </span>
            </div>
            <div class="text-secondary">{{ line.detail }}</div>
            <!-- Une ligne ajoutée après coup par un autre compte le dit ; sinon l'auteur de la vente suffit. -->
            <AuthorLabel
              v-if="line.movement.createdByName !== sale.createdByName"
              :name="line.movement.createdByName"
            />
          </div>
          <div class="font-weight-medium">{{ formatEuros(line.movement.amount ?? 0) }} €</div>
          <v-icon size="16" class="text-secondary">phosphor:pencil-simple</v-icon>
        </button>
        <div class="sale-detail-view__total-row">
          <div class="font-weight-medium">Total</div>
          <div class="sale-detail-view__total">{{ formatEuros(sale.total) }} €</div>
        </div>
      </AppCard>

      <template v-if="!editing">
        <AppButton
          v-if="!sale.paid"
          block
          height="56"
          color="success"
          :disabled="togglingPayment"
          @click="markPaid"
        >
          <v-icon start size="18">phosphor:check-circle</v-icon>
          Marquer comme payée
        </AppButton>
        <p v-if="paymentError" class="sale-detail-view__error text-error">{{ paymentError }}</p>

        <AppButton block height="56" color="secondary" variant="outlined" @click="openEdit">
          <v-icon start size="18">phosphor:pencil-simple</v-icon>
          Corriger cette vente
        </AppButton>

        <SaleDeleteAction :sale="sale" @deleted="onSaleDeleted" />

        <p class="sale-detail-view__hint text-secondary">
          Une ligne se corrige en appuyant dessus. Supprimer la vente rend au stock les unités qui
          n'ont pas d'autre sortie.
        </p>
      </template>
    </div>

    <SaleLineEditDialog
      v-model="lineDialogOpen"
      :line="editedLine"
      @saved="onLineChanged"
      @removed="onLineChanged"
    />
  </v-container>
</template>

<style scoped>
.sale-detail-view {
  padding-bottom: 40px;
}

.sale-detail-view__date {
  font-size: 16px;
  font-weight: 500;
  padding: 0 4px 4px;
  text-transform: capitalize;
}

.sale-detail-view__author {
  padding: 0 4px 16px;
}

.sale-detail-view__badge {
  font-size: 15px;
  font-weight: 600;
  padding: 5px 14px;
  border-radius: 999px;
  flex-shrink: 0;
}

.sale-detail-view__badge--paid {
  background: rgb(var(--v-theme-success-container));
  color: rgb(var(--v-theme-success));
}

.sale-detail-view__badge--pending {
  background: rgb(var(--v-theme-warning-container));
  color: rgb(var(--v-theme-warning));
}

.sale-detail-view__sections {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.sale-detail-view__customer {
  background: rgb(var(--v-theme-surface));
  border-radius: 14px;
  padding: 14px 16px;
  display: flex;
  align-items: center;
  gap: 12px;
  text-decoration: none;
  color: rgb(var(--v-theme-on-surface));
  box-shadow: 0 1px 2px rgba(43, 36, 30, 0.06);
}

.sale-detail-view__customer-info {
  flex: 1;
  min-width: 0;
}

.sale-detail-view__customer-label {
  font-size: 13px;
  font-weight: 600;
  letter-spacing: 0.04em;
  text-transform: uppercase;
}

.sale-detail-view__customer-name {
  font-family: var(--font-heading);
  font-weight: 600;
  font-size: 19px;
}

.sale-detail-view__section-title {
  font-size: 16px;
  font-weight: 600;
  margin-bottom: 8px;
}

.sale-detail-view__field-label {
  font-size: 15px;
  font-weight: 500;
  margin-bottom: 6px;
}

.sale-detail-view__notes {
  font-size: 16px;
  margin: 0;
  white-space: pre-wrap;
}

.sale-detail-view__retired {
  font-size: 13px;
  font-weight: 400;
}

.sale-detail-view__line {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  width: 100%;
  min-height: 52px;
  border: none;
  border-top: 1px solid rgb(var(--v-theme-status-neutral-container));
  background: none;
  padding: 8px 0;
  cursor: pointer;
  font-family: var(--font-body);
  font-size: 16px;
  color: rgb(var(--v-theme-on-surface));
  text-align: left;
}

.sale-detail-view__line:first-of-type {
  border-top: none;
}

.sale-detail-view__line-info {
  flex: 1;
  min-width: 0;
}

.sale-detail-view__payment {
  display: flex;
  gap: 10px;
}

.sale-detail-view__payment-option {
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

.sale-detail-view__payment-option--paid {
  border-color: rgb(var(--v-theme-success));
  background: rgb(var(--v-theme-success));
  color: rgb(var(--v-theme-surface));
}

.sale-detail-view__payment-option--pending {
  border-color: rgb(var(--v-theme-warning));
  background: rgb(var(--v-theme-warning-container));
  color: rgb(var(--v-theme-warning));
}

.sale-detail-view__edit-actions {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
  margin-top: 16px;
}

.sale-detail-view__error {
  font-size: 14px;
  font-weight: 500;
  margin: 12px 0 0;
}

.sale-detail-view__hint {
  font-size: 14px;
  font-weight: 500;
  text-align: center;
  margin: 0;
}

.sale-detail-view__total-row {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  border-top: 2px solid rgb(var(--v-theme-field-border));
  padding-top: 12px;
  margin-top: 4px;
}

.sale-detail-view__total {
  font-family: var(--font-heading);
  font-weight: 700;
  font-size: 28px;
  color: rgb(var(--v-theme-success));
}
</style>
