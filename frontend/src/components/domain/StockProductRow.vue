<script setup lang="ts">
import type { StockDashboardProduct } from '@/composables/useStock'

defineProps<{ product: StockDashboardProduct }>()
</script>

<template>
  <RouterLink :to="product.href" class="stock-product-row">
    <div class="stock-product-row__info">
      <div class="stock-product-row__name">{{ product.name }}</div>
      <div class="stock-product-row__meta text-secondary">{{ product.meta }}</div>
      <div v-if="product.openedLabel" class="stock-product-row__opened-wrap">
        <span class="stock-product-row__opened">{{ product.openedLabel }}</span>
      </div>
    </div>

    <div v-if="product.isEmpty" class="stock-product-row__empty">Épuisé</div>
    <div v-else class="stock-product-row__qty">
      <div class="stock-product-row__qty-value">{{ product.qty }}</div>
      <div class="stock-product-row__qty-label text-secondary">{{ product.qtyLabel }}</div>
    </div>
  </RouterLink>
</template>

<style scoped>
.stock-product-row {
  text-decoration: none;
  color: rgb(var(--v-theme-on-surface));
  background: rgb(var(--v-theme-surface));
  border-radius: 14px;
  padding: 18px 18px 18px 20px;
  display: flex;
  align-items: center;
  gap: 14px;
  box-shadow: 0 1px 2px rgba(43, 36, 30, 0.06);
  min-height: 56px;
}

.stock-product-row:hover {
  background: rgb(var(--v-theme-status-neutral-container));
}

.stock-product-row__info {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 5px;
}

.stock-product-row__name {
  font-family: var(--font-heading);
  font-weight: 600;
  font-size: 21px;
  line-height: 1.2;
}

.stock-product-row__meta {
  font-size: 15px;
}

.stock-product-row__opened-wrap {
  display: flex;
}

.stock-product-row__opened {
  background: rgb(var(--v-theme-warning-container));
  color: rgb(var(--v-theme-warning));
  font-size: 14px;
  font-weight: 600;
  padding: 3px 10px;
  border-radius: 999px;
}

.stock-product-row__empty {
  font-size: 16px;
  font-weight: 600;
  color: rgb(var(--v-theme-secondary));
  background: rgb(var(--v-theme-status-neutral-container));
  padding: 6px 14px;
  border-radius: 999px;
}

.stock-product-row__qty {
  text-align: right;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.stock-product-row__qty-value {
  font-family: var(--font-heading);
  font-weight: 700;
  font-size: 30px;
  line-height: 1;
  color: rgb(var(--v-theme-success));
}

.stock-product-row__qty-label {
  font-size: 14px;
}
</style>
