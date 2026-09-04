<script setup lang="ts">
import { ref, watch } from 'vue'
import AppFab from '@/components/base/AppFab.vue'
import AppBrandHeader from '@/components/base/AppBrandHeader.vue'
import ProductRow from '@/components/domain/ProductRow.vue'
import { listProducts } from '@/api/products'
import { useAsyncData } from '@/composables/useAsyncData'

const includeInactive = ref(false)
const { data: products, loading, error, reload } = useAsyncData(() => listProducts(includeInactive.value), [])

watch(includeInactive, reload)
</script>

<template>
  <v-container class="products-view">
    <AppBrandHeader />

    <header class="products-view__header">
      <div class="products-view__title-row">
        <h1 class="text-h4 font-weight-bold">Produits</h1>
        <span class="products-view__total text-secondary font-weight-medium">
          {{ products.filter((p) => p.isActive).length }} produits actifs
        </span>
      </div>
      <button type="button" class="products-view__toggle" @click="includeInactive = !includeInactive">
        <span>Afficher les produits désactivés</span>
        <span class="products-view__switch" :class="{ 'products-view__switch--on': includeInactive }">
          <span class="products-view__knob" />
        </span>
      </button>
    </header>

    <p v-if="loading" class="text-secondary">Chargement...</p>
    <p v-else-if="error" class="text-error">{{ error }}</p>

    <div v-else class="products-view__list">
      <ProductRow v-for="product in products" :key="product.id" :product="product" />
    </div>

    <AppFab icon="plus" ariaLabel="Créer un produit" to="/products/add" />
  </v-container>
</template>

<style scoped>
.products-view {
  padding-bottom: 96px;
}

.products-view__header {
  display: flex;
  flex-direction: column;
  gap: 14px;
  padding: 0 4px 12px;
}

.products-view__title-row {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  gap: 12px;
}

.products-view__total {
  font-size: 15px;
}

.products-view__toggle {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  background: rgb(var(--v-theme-surface));
  border: none;
  border-radius: 12px;
  padding: 12px 16px;
  cursor: pointer;
  font-family: var(--font-body);
  font-size: 16px;
  font-weight: 500;
  color: rgb(var(--v-theme-on-surface));
  min-height: 48px;
}

.products-view__switch {
  width: 52px;
  height: 32px;
  border-radius: 999px;
  background: rgb(var(--v-theme-field-border));
  position: relative;
  flex-shrink: 0;
  transition: background 0.2s;
}

.products-view__switch--on {
  background: rgb(var(--v-theme-primary));
}

.products-view__knob {
  position: absolute;
  top: 3px;
  left: 3px;
  width: 26px;
  height: 26px;
  border-radius: 50%;
  background: rgb(var(--v-theme-surface));
  box-shadow: 0 1px 3px rgba(43, 36, 30, 0.3);
  transition: left 0.2s;
}

.products-view__switch--on .products-view__knob {
  left: 23px;
}

.products-view__list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}
</style>
