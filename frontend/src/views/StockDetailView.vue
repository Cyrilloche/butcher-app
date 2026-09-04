<script setup lang="ts">
import { watch } from 'vue'
import AppPageHeader from '@/components/base/AppPageHeader.vue'
import AppCard from '@/components/base/AppCard.vue'
import StockUnitRow from '@/components/domain/StockUnitRow.vue'
import { getStockDetail } from '@/composables/useStock'
import { useAsyncData } from '@/composables/useAsyncData'

const props = defineProps<{ code: string }>()

const {
  data: detail,
  loading,
  error,
  reload,
} = useAsyncData(() => getStockDetail(props.code), null)

watch(() => props.code, reload)
</script>

<template>
  <v-container v-if="loading" class="stock-detail-view">
    <p class="text-secondary">Chargement...</p>
  </v-container>

  <v-container v-else-if="error" class="stock-detail-view">
    <p class="text-error">{{ error }}</p>
  </v-container>

  <v-container v-else-if="detail" class="stock-detail-view">
    <AppPageHeader to="/" back-label="Stock" :title="detail.name" :subtitle="detail.summary" />

    <div class="stock-detail-view__batches">
      <AppCard v-for="batch in detail.batches" :key="batch.batchNumber" class="stock-detail-view__batch">
        <div class="stock-detail-view__batch-header">
          <span class="font-weight-medium">Fabriqué le {{ batch.dateLabel }}</span>
          <span class="text-secondary font-weight-medium">{{ batch.priceLabel }}</span>
        </div>
        <div>
          <StockUnitRow v-for="unit in batch.units" :key="unit.number" :unit="unit" />
        </div>
      </AppCard>
    </div>
  </v-container>

  <v-container v-else>
    <p class="text-secondary">Produit introuvable.</p>
  </v-container>
</template>

<style scoped>
.stock-detail-view {
  padding-bottom: 40px;
}

.stock-detail-view__batches {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.stock-detail-view__batch-header {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  gap: 10px;
  font-size: 16px;
  margin-bottom: 10px;
}
</style>
