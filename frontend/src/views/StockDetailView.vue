<script setup lang="ts">
import { ref, watch } from 'vue'
import AppPageHeader from '@/components/base/AppPageHeader.vue'
import AppCard from '@/components/base/AppCard.vue'
import StockUnitRow from '@/components/domain/StockUnitRow.vue'
import StockUnitOutcomeMenu from '@/components/domain/StockUnitOutcomeMenu.vue'
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

// Sorties de stock (clôture, perso, perte) : le menu porte la logique et la
// confirmation, la vue ne garde que le rafraîchissement et le message d'erreur.
const outcomeError = ref<string | null>(null)

async function onOutcomeDone() {
  outcomeError.value = null
  await reload()
}
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
    <p v-if="outcomeError" class="text-error stock-detail-view__outcome-error">{{ outcomeError }}</p>

    <div class="stock-detail-view__batches">
      <AppCard v-for="batch in detail.batches" :key="batch.batchNumber" class="stock-detail-view__batch">
        <div class="stock-detail-view__batch-header">
          <span class="font-weight-medium">Fabriqué le {{ batch.dateLabel }}</span>
          <span class="text-secondary font-weight-medium">{{ batch.priceLabel }}</span>
        </div>
        <div>
          <StockUnitRow v-for="unit in batch.units" :key="unit.number" :unit="unit">
            <template #action>
              <StockUnitOutcomeMenu
                :unit="unit"
                @done="onOutcomeDone"
                @failed="(message) => (outcomeError = message)"
              />
            </template>
          </StockUnitRow>
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

.stock-detail-view__outcome-error {
  font-size: 14px;
  font-weight: 500;
  padding: 0 4px 12px;
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
