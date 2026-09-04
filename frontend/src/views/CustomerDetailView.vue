<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue'
import AppPageHeader from '@/components/base/AppPageHeader.vue'
import AppCard from '@/components/base/AppCard.vue'
import AppTextField from '@/components/base/AppTextField.vue'
import AppButton from '@/components/base/AppButton.vue'
import { getCustomer, updateCustomer } from '@/api/customers'
import { listSales } from '@/api/sales'
import { useAsyncData } from '@/composables/useAsyncData'
import { ApiError } from '@/api/http'
import type { SaleDto } from '@/api/types'

const props = defineProps<{ id: string }>()
const customerId = computed(() => Number(props.id))

const { data: customer, loading, error, reload } = useAsyncData(() => getCustomer(customerId.value), null)

const { data: sales } = useAsyncData(() => listSales({ customerId: customerId.value }), [] as SaleDto[])

watch(customerId, reload)

const salesSorted = computed(() =>
  [...sales.value].sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime()),
)
const salesTotal = computed(() => sales.value.reduce((sum, s) => sum + s.total, 0))
const lastSaleLabel = computed(() => {
  if (salesSorted.value.length === 0) return '—'
  return new Date(salesSorted.value[0]!.date).toLocaleDateString('fr-FR', { day: 'numeric', month: 'short' })
})

const state = reactive({ firstName: '', lastName: '', phone: '', notes: '' })

watch(
  customer,
  (c) => {
    if (!c) return
    state.firstName = c.firstName ?? ''
    state.lastName = c.lastName
    state.phone = c.phone ?? ''
    state.notes = c.notes ?? ''
  },
  { immediate: true },
)

const dirty = computed(
  () =>
    !!customer.value &&
    (state.firstName !== (customer.value.firstName ?? '') ||
      state.lastName !== customer.value.lastName ||
      state.phone !== (customer.value.phone ?? '') ||
      state.notes !== (customer.value.notes ?? '')),
)
const canSave = computed(() => state.lastName.trim().length > 1)
const saving = ref(false)
const saveError = ref<string | null>(null)

async function save() {
  if (!customer.value || !canSave.value) return
  saving.value = true
  saveError.value = null
  try {
    await updateCustomer(customer.value.id, {
      lastName: state.lastName.trim(),
      firstName: state.firstName.trim() || undefined,
      phone: state.phone.trim() || undefined,
      notes: state.notes.trim() || undefined,
    })
    await reload()
  } catch (err) {
    saveError.value = err instanceof ApiError ? err.message : "Erreur lors de l'enregistrement, réessaie."
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <v-container v-if="loading" class="customer-detail-view">
    <p class="text-secondary">Chargement...</p>
  </v-container>

  <v-container v-else-if="error" class="customer-detail-view">
    <p class="text-error">{{ error }}</p>
  </v-container>

  <v-container v-else-if="customer" class="customer-detail-view">
    <AppPageHeader
      to="/customers"
      back-label="Clients"
      :title="state.firstName ? `${state.firstName} ${state.lastName}` : state.lastName"
    />

    <div class="customer-detail-view__sections">
      <AppCard class="customer-detail-view__stats">
        <div class="customer-detail-view__stat">
          <div class="customer-detail-view__stat-value">{{ sales.length }}</div>
          <div class="customer-detail-view__stat-label text-secondary">ventes</div>
        </div>
        <div class="customer-detail-view__divider" />
        <div class="customer-detail-view__stat">
          <div class="customer-detail-view__stat-value customer-detail-view__stat-value--accent">
            {{ salesTotal.toLocaleString('fr-FR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) }} €
          </div>
          <div class="customer-detail-view__stat-label text-secondary">total</div>
        </div>
        <div class="customer-detail-view__divider" />
        <div class="customer-detail-view__stat">
          <div class="customer-detail-view__stat-value">{{ lastSaleLabel }}</div>
          <div class="customer-detail-view__stat-label text-secondary">dernière vente</div>
        </div>
      </AppCard>

      <AppCard>
        <div class="customer-detail-view__section-title text-secondary">Coordonnées</div>
        <div class="customer-detail-view__form">
          <AppTextField v-model="state.firstName" label="Prénom" />
          <AppTextField v-model="state.lastName" label="Nom" />
          <AppTextField v-model="state.phone" type="tel" inputmode="tel" label="Téléphone" />
          <div class="customer-detail-view__notes">
            <label class="customer-detail-view__notes-label">Notes</label>
            <textarea
              v-model="state.notes"
              rows="4"
              placeholder="Préférences, allergies, habitudes de commande…"
              class="customer-detail-view__textarea"
            />
          </div>
        </div>
      </AppCard>

      <AppCard v-if="salesSorted.length > 0">
        <div class="customer-detail-view__section-title text-secondary">Dernières ventes</div>
        <div>
          <RouterLink
            v-for="sale in salesSorted"
            :key="sale.id"
            :to="`/sales/${sale.id}`"
            class="customer-detail-view__sale-row"
          >
            <div class="customer-detail-view__sale-info">
              <div class="customer-detail-view__sale-date">
                {{ new Date(sale.date).toLocaleDateString('fr-FR', { day: 'numeric', month: 'long', year: 'numeric' }) }}
              </div>
              <div class="text-secondary">{{ sale.saleNumber }}</div>
            </div>
            <span v-if="!sale.paid" class="customer-detail-view__pending">À payer</span>
            <div class="customer-detail-view__sale-amount">
              {{ sale.total.toLocaleString('fr-FR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) }} €
            </div>
            <v-icon size="16" class="customer-detail-view__sale-caret">phosphor:caret-right</v-icon>
          </RouterLink>
        </div>
      </AppCard>
    </div>

    <div v-if="dirty" class="customer-detail-view__footer">
      <p v-if="saveError" class="customer-detail-view__save-error text-error">{{ saveError }}</p>
      <AppButton block height="60" color="primary" :disabled="!canSave || saving" @click="save">
        {{ saving ? 'Enregistrement...' : 'Enregistrer les modifications' }}
      </AppButton>
    </div>
  </v-container>
</template>

<style scoped>
.customer-detail-view {
  padding-bottom: 130px;
}

.customer-detail-view__sections {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.customer-detail-view__stats {
  display: flex;
  gap: 10px;
}

.customer-detail-view__stat {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 3px;
  text-align: center;
}

.customer-detail-view__stat-value {
  font-family: var(--font-heading);
  font-weight: 700;
  font-size: 26px;
  line-height: 1;
}

.customer-detail-view__stat-value--accent {
  color: rgb(var(--v-theme-success));
}

.customer-detail-view__stat-label {
  font-size: 13px;
  font-weight: 500;
}

.customer-detail-view__divider {
  width: 1px;
  background: rgb(var(--v-theme-status-neutral-container));
}

.customer-detail-view__section-title {
  font-size: 16px;
  font-weight: 600;
  margin-bottom: 12px;
}

.customer-detail-view__form {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.customer-detail-view__notes {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.customer-detail-view__notes-label {
  font-size: 15px;
  font-weight: 500;
}

.customer-detail-view__textarea {
  border: 1.5px solid rgb(var(--v-theme-field-border));
  border-radius: 10px;
  background: rgb(var(--v-theme-field-surface));
  padding: 12px 14px;
  font-family: var(--font-body);
  font-size: 17px;
  color: rgb(var(--v-theme-on-surface));
  resize: none;
  line-height: 1.4;
}

.customer-detail-view__sale-row {
  display: flex;
  align-items: center;
  gap: 12px;
  min-height: 52px;
  border-top: 1px solid rgb(var(--v-theme-status-neutral-container));
  padding: 8px 0;
  text-decoration: none;
  color: rgb(var(--v-theme-on-surface));
}

.customer-detail-view__sale-row:first-child {
  border-top: none;
}

.customer-detail-view__sale-info {
  flex: 1;
  min-width: 0;
}

.customer-detail-view__sale-date {
  font-size: 16px;
  font-weight: 600;
}

.customer-detail-view__pending {
  background: rgb(var(--v-theme-warning-container));
  color: rgb(var(--v-theme-warning));
  font-size: 13px;
  font-weight: 600;
  padding: 3px 10px;
  border-radius: 999px;
  flex-shrink: 0;
}

.customer-detail-view__sale-amount {
  font-size: 17px;
  font-weight: 600;
  color: rgb(var(--v-theme-success));
  flex-shrink: 0;
}

.customer-detail-view__sale-caret {
  color: rgb(var(--v-theme-status-neutral));
  flex-shrink: 0;
}

.customer-detail-view__save-error {
  font-size: 14px;
  font-weight: 500;
  text-align: center;
  margin: 0 0 10px;
}

.customer-detail-view__footer {
  position: fixed;
  left: 0;
  right: 0;
  bottom: 0;
  padding: 14px 16px 34px;
  background: linear-gradient(to top, rgb(var(--v-theme-background)) 70%, transparent);
}
</style>
