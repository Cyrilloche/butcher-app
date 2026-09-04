<!--
  Squelette : route sur l'id technique du stock_movement (pas de numéro de
  vente — cf. docs/data-model.md §9, QM-04). Une "vente" ici = un seul
  mouvement, pas plusieurs lots regroupés comme dans la maquette.
-->
<script setup lang="ts">
import { computed } from 'vue'
import AppPageHeader from '@/components/base/AppPageHeader.vue'
import AppCard from '@/components/base/AppCard.vue'
import { getStockMovement } from '@/api/stockMovements'
import { getStockUnit } from '@/api/stockUnits'
import { listProducts } from '@/api/products'
import { useAsyncData } from '@/composables/useAsyncData'
import { formatWeight } from '@/composables/useStock'

const props = defineProps<{ id: string }>()
const movementId = computed(() => Number(props.id))

const { data: sale, loading, error } = useAsyncData(async () => {
  const movement = await getStockMovement(movementId.value)
  const unit = await getStockUnit(movement.stockUnitId)
  const productCode = unit.batchNumber.split('-')[0]
  const products = await listProducts(true)
  const product = products.find((p) => p.code === productCode)
  return { movement, unit, productName: product?.name ?? productCode }
}, null)
</script>

<template>
  <v-container v-if="loading" class="sale-detail-view">
    <p class="text-secondary">Chargement...</p>
  </v-container>

  <v-container v-else-if="error" class="sale-detail-view">
    <p class="text-error">{{ error }}</p>
  </v-container>

  <v-container v-else-if="sale" class="sale-detail-view">
    <AppPageHeader
      to="/sales"
      back-label="Ventes"
      :title="new Date(sale.movement.date).toLocaleDateString('fr-FR', { day: 'numeric', month: 'long', year: 'numeric' })"
    />

    <div class="sale-detail-view__sections">
      <RouterLink
        v-if="sale.movement.customerId"
        :to="`/customers/${sale.movement.customerId}`"
        class="sale-detail-view__customer"
      >
        <div class="sale-detail-view__customer-info">
          <div class="sale-detail-view__customer-label text-secondary">Client</div>
          <div class="sale-detail-view__customer-name">{{ sale.movement.customerName }}</div>
        </div>
        <v-icon size="16">phosphor:caret-right</v-icon>
      </RouterLink>

      <AppCard>
        <div class="sale-detail-view__lot-row">
          <div class="sale-detail-view__lot-info">
            <div class="sale-detail-view__lot-product">{{ sale.productName }}</div>
            <div class="text-secondary">
              {{ sale.unit.batchNumber }} ·
              {{ sale.unit.weight != null ? formatWeight(Math.round(sale.unit.weight * 1000)) : 'À la pièce' }}
            </div>
          </div>
        </div>
        <div class="sale-detail-view__total-row">
          <div class="font-weight-medium">Total</div>
          <div class="sale-detail-view__total">
            {{ (sale.movement.amount ?? 0).toLocaleString('fr-FR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) }} €
          </div>
        </div>
      </AppCard>
    </div>
  </v-container>
</template>

<style scoped>
.sale-detail-view {
  padding-bottom: 40px;
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

.sale-detail-view__lot-row {
  padding-bottom: 12px;
  border-bottom: 1px solid rgb(var(--v-theme-status-neutral-container));
  margin-bottom: 12px;
}

.sale-detail-view__lot-product {
  font-size: 16px;
  font-weight: 600;
}

.sale-detail-view__total-row {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
}

.sale-detail-view__total {
  font-family: var(--font-heading);
  font-weight: 700;
  font-size: 28px;
  color: rgb(var(--v-theme-success));
}
</style>
