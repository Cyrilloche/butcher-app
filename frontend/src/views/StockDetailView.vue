<script setup lang="ts">
import { ref, watch } from 'vue'
import AppPageHeader from '@/components/base/AppPageHeader.vue'
import AppCard from '@/components/base/AppCard.vue'
import StockUnitRow from '@/components/domain/StockUnitRow.vue'
import StockUnitOutcomeMenu from '@/components/domain/StockUnitOutcomeMenu.vue'
import BatchPriceEditAction from '@/components/domain/BatchPriceEditAction.vue'
import BatchDeleteAction from '@/components/domain/BatchDeleteAction.vue'
import StockUnitDeleteAction from '@/components/domain/StockUnitDeleteAction.vue'
import ProductStatusBadge from '@/components/domain/ProductStatusBadge.vue'
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
    <AppPageHeader to="/" back-label="Stock" :title="detail.name" :subtitle="detail.summary">
      <template #badge>
        <ProductStatusBadge v-if="!detail.isActive" :is-active="false" />
      </template>
    </AppPageHeader>
    <p v-if="outcomeError" class="text-error stock-detail-view__outcome-error">{{ outcomeError }}</p>

    <!-- La date sort de la carte et la surmonte : elle sépare les fournées comme un titre de
         section, et la carte n'a plus à porter qu'une liste d'unités. -->
    <div class="stock-detail-view__batches">
      <section v-for="batch in detail.batches" :key="batch.id">
        <div class="stock-detail-view__day">
          <div class="stock-detail-view__day-label">
            <h3 class="stock-detail-view__day-title">{{ batch.dateLabel }}</h3>
            <!-- Sous la date, jamais à côté : accolé, le rang poussait la ligne à se couper en
                 plein milieu du titre sur un téléphone. -->
            <span v-if="batch.dayRankLabel" class="stock-detail-view__day-rank text-secondary">
              {{ batch.dayRankLabel }}
            </span>
          </div>
          <div class="stock-detail-view__day-meta">
            <span class="text-secondary font-weight-medium">{{ batch.priceLabel }}</span>
            <BatchPriceEditAction :batch="batch" @done="onOutcomeDone" />
            <BatchDeleteAction
              :batch="batch"
              @done="onOutcomeDone"
              @failed="(message) => (outcomeError = message)"
            />
          </div>
        </div>

        <AppCard>
          <StockUnitRow v-for="unit in batch.units" :key="unit.number" :unit="unit">
            <template #action>
              <StockUnitDeleteAction
                :unit="unit"
                @done="onOutcomeDone"
                @failed="(message) => (outcomeError = message)"
              />
              <StockUnitOutcomeMenu
                :unit="unit"
                @done="onOutcomeDone"
                @failed="(message) => (outcomeError = message)"
              />
            </template>
          </StockUnitRow>
        </AppCard>
      </section>
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
  gap: 26px;
}

.stock-detail-view__outcome-error {
  font-size: 14px;
  font-weight: 500;
  padding: 0 4px 12px;
}

.stock-detail-view__day {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  gap: 10px;
  padding: 0 2px 8px;
  margin-bottom: 10px;
  border-bottom: 2px solid rgb(var(--v-theme-secondary));
}

.stock-detail-view__day-label {
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.stock-detail-view__day-title {
  font-family: var(--font-heading);
  font-size: 21px;
  font-weight: 600;
  line-height: 1.15;
  margin: 0;
}

.stock-detail-view__day-rank {
  font-family: var(--font-body);
  font-size: 13px;
  font-weight: 500;
  line-height: 1.2;
}

.stock-detail-view__day-meta {
  display: flex;
  align-items: center;
  gap: 4px;
  flex-shrink: 0;
}
</style>
