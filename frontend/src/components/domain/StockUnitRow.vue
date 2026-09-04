<script setup lang="ts">
import StockStatusBadge from '@/components/domain/StockStatusBadge.vue'
import type { StockDetailUnit } from '@/composables/useStock'

defineProps<{ unit: StockDetailUnit }>()
</script>

<template>
  <div class="stock-unit-row">
    <span class="stock-unit-row__num">{{ unit.number }}</span>
    <span v-if="unit.weightLabel" class="stock-unit-row__weight text-secondary">{{ unit.weightLabel }}</span>
    <StockStatusBadge :status="unit.status" />
  </div>
</template>

<style scoped>
.stock-unit-row {
  display: flex;
  align-items: center;
  gap: 12px;
  min-height: 52px;
  border-top: 1px solid rgb(var(--v-theme-status-neutral-container));
  padding: 6px 0;
}

.stock-unit-row:first-child {
  border-top: none;
}

.stock-unit-row__num {
  flex: 1;
  font-size: 17px;
  font-weight: 500;
}

.stock-unit-row__weight {
  font-size: 17px;
}
</style>
