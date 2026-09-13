<!-- src/views/StockView.vue -->
<script setup lang="ts">
import { defineAsyncComponent } from 'vue'
import AppFab from '@/components/base/AppFab.vue'
import AppBrandHeader from '@/components/base/AppBrandHeader.vue'
import StockProductRow from '@/components/domain/StockProductRow.vue'
import { getStockDashboard } from '@/composables/useStock'
import { useAsyncData } from '@/composables/useAsyncData'
import { useAddDialog } from '@/composables/useAddDialog'

const StockAddView = defineAsyncComponent(() => import('@/views/StockAddView.vue'))

const {
  data: dashboard,
  loading,
  error,
  reload,
} = useAsyncData(getStockDashboard, { products: [], totalUnitsInStock: 0 })
const { mdAndUp, open: addOpen } = useAddDialog()
</script>

<template>
  <v-container class="stock-view app-page-container">
    <AppBrandHeader />

    <header class="stock-view__header">
      <div class="stock-view__title-row">
        <h1 class="text-h4 font-weight-bold">Stock</h1>
        <span class="stock-view__total text-secondary font-weight-medium">
          {{ dashboard.totalUnitsInStock }} unités en stock
        </span>
      </div>
    </header>

    <p v-if="loading" class="text-secondary">Chargement...</p>
    <p v-else-if="error" class="text-error">{{ error }}</p>

    <div v-else class="stock-view__list">
      <StockProductRow v-for="product in dashboard.products" :key="product.code" :product="product" />
    </div>

    <AppFab icon="plus" ariaLabel="Ajouter des produits au stock" :to="mdAndUp ? undefined : '/stock/add'" @click="addOpen = true" />

    <v-dialog v-model="addOpen">
      <StockAddView v-if="addOpen" dialog @saved="addOpen = false; reload()" @cancel="addOpen = false" />
    </v-dialog>
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
  padding: 0 4px 12px;
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
