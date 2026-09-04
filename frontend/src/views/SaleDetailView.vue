<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import AppPageHeader from '@/components/base/AppPageHeader.vue'
import AppCard from '@/components/base/AppCard.vue'
import AppButton from '@/components/base/AppButton.vue'
import { getSale, setSalePayment } from '@/api/sales'
import { getStockUnit } from '@/api/stockUnits'
import { listProducts } from '@/api/products'
import { useAsyncData } from '@/composables/useAsyncData'
import { formatWeight } from '@/composables/useStock'
import type { StockMovementDto } from '@/api/types'

const props = defineProps<{ id: string }>()
const saleId = computed(() => Number(props.id))

interface SaleLineView {
  movement: StockMovementDto
  productName: string
  detail: string
}

const { data: sale, loading, error, reload } = useAsyncData(() => getSale(saleId.value), null)

const lineViews = ref<SaleLineView[]>([])
watch(sale, async (s) => {
  if (!s) {
    lineViews.value = []
    return
  }
  const products = await listProducts(true)
  lineViews.value = await Promise.all(
    s.lines.map(async (movement) => {
      const unit = await getStockUnit(movement.stockUnitId)
      const productCode = unit.batchNumber.split('-')[0]
      const product = products.find((p) => p.code === productCode)
      return {
        movement,
        productName: product?.name ?? productCode ?? '—',
        detail: `${unit.batchNumber} · ${unit.weight != null ? formatWeight(Math.round(unit.weight * 1000)) : 'À la pièce'}`,
      }
    }),
  )
})

const togglingPayment = ref(false)
async function markPaid() {
  if (!sale.value) return
  togglingPayment.value = true
  try {
    await setSalePayment(sale.value.id, { paid: true })
    await reload()
  } finally {
    togglingPayment.value = false
  }
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
        <span :class="sale.paid ? 'sale-detail-view__badge--paid' : 'sale-detail-view__badge--pending'" class="sale-detail-view__badge">
          {{ sale.paid ? 'Payée' : 'À payer' }}
        </span>
      </template>
    </AppPageHeader>
    <div class="sale-detail-view__date text-secondary">
      {{ new Date(sale.date).toLocaleDateString('fr-FR', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' }) }}
    </div>

    <div class="sale-detail-view__sections">
      <RouterLink :to="`/customers/${sale.customerId}`" class="sale-detail-view__customer">
        <div class="sale-detail-view__customer-info">
          <div class="sale-detail-view__customer-label text-secondary">Client</div>
          <div class="sale-detail-view__customer-name">{{ sale.customerName }}</div>
        </div>
        <v-icon size="16">phosphor:caret-right</v-icon>
      </RouterLink>

      <AppCard>
        <div class="sale-detail-view__section-title text-secondary">
          {{ sale.itemCount }} lot{{ sale.itemCount > 1 ? 's' : '' }} vendu{{ sale.itemCount > 1 ? 's' : '' }}
        </div>
        <div v-for="line in lineViews" :key="line.movement.id" class="sale-detail-view__line">
          <div class="sale-detail-view__line-info">
            <div class="font-weight-medium">{{ line.productName }}</div>
            <div class="text-secondary">{{ line.detail }}</div>
          </div>
          <div class="font-weight-medium">
            {{ (line.movement.amount ?? 0).toLocaleString('fr-FR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) }} €
          </div>
        </div>
        <div class="sale-detail-view__total-row">
          <div class="font-weight-medium">Total</div>
          <div class="sale-detail-view__total">
            {{ sale.total.toLocaleString('fr-FR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) }} €
          </div>
        </div>
      </AppCard>

      <AppButton v-if="!sale.paid" block height="56" color="success" :disabled="togglingPayment" @click="markPaid">
        <v-icon start size="18">phosphor:check-circle</v-icon>
        Marquer comme payée
      </AppButton>
    </div>
  </v-container>
</template>

<style scoped>
.sale-detail-view {
  padding-bottom: 40px;
}

.sale-detail-view__date {
  font-size: 16px;
  font-weight: 500;
  padding: 0 4px 16px;
  text-transform: capitalize;
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

.sale-detail-view__line {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  min-height: 52px;
  border-top: 1px solid rgb(var(--v-theme-status-neutral-container));
  padding: 8px 0;
}

.sale-detail-view__line:first-of-type {
  border-top: none;
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
