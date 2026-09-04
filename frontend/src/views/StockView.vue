<!-- src/views/StockView.vue -->
<script setup lang="ts">
import AppFab from '@/components/base/AppFab.vue'
import AppBrandHeader from '@/components/base/AppBrandHeader.vue'
import StockProductRow from '@/components/domain/StockProductRow.vue'
import { getStockDashboard } from '@/composables/useStock'
import { useAsyncData } from '@/composables/useAsyncData'

const {
  data: dashboard,
  loading,
  error,
} = useAsyncData(getStockDashboard, { products: [], totalAvailableUnits: 0 })
</script>

<template>
  <v-container class="stock-view">
    <header class="stock-view__header">
      <AppBrandHeader />
      <div class="stock-view__title-row">
        <h1 class="text-h4 font-weight-bold">Stock</h1>
        <span class="stock-view__total text-secondary font-weight-medium">
          {{ dashboard.totalAvailableUnits }} unités disponibles
        </span>
      </div>
    </header>

    <p v-if="loading" class="text-secondary">Chargement...</p>
    <p v-else-if="error" class="text-error">{{ error }}</p>

    <div v-else class="stock-view__list">
      <StockProductRow v-for="product in dashboard.products" :key="product.code" :product="product" />
    </div>

    <AppFab icon="plus" ariaLabel="Ajouter des produits au stock" to="/stock/add" />
  </v-container>
</template>

<style scoped>
.stock-view {
  padding-bottom: 96px;
}

.stock-view__header {
  display: flex;
  flex-direction: column;
  gap: 14px;
  padding: 8px 4px 12px;
}

.stock-view__title-row {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  gap: 12px;
}

.stock-view__total {
  font-size: 15px;
}

.stock-view__list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}
</style>
