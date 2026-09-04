<script setup lang="ts">
import ProductStatusBadge from '@/components/domain/ProductStatusBadge.vue'
import type { ProductDto } from '@/api/types'

defineProps<{ product: ProductDto }>()
</script>

<template>
  <RouterLink :to="`/products/${product.code}`" class="product-row" :style="{ opacity: product.isActive ? 1 : 0.65 }">
    <div class="product-row__info">
      <div class="product-row__name">{{ product.name }}</div>
      <div class="product-row__meta text-secondary">
        {{ product.saleMode === 'by_weight' ? 'Au poids' : 'À la pièce' }} · {{ product.code }}
      </div>
    </div>
    <ProductStatusBadge :is-active="product.isActive" />
  </RouterLink>
</template>

<style scoped>
.product-row {
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

.product-row:hover {
  background: rgb(var(--v-theme-status-neutral-container));
}

.product-row__info {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 5px;
}

.product-row__name {
  font-family: var(--font-heading);
  font-weight: 600;
  font-size: 21px;
  line-height: 1.2;
}

.product-row__meta {
  font-size: 15px;
}
</style>
