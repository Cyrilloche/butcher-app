<script setup lang="ts">
import { computed, defineAsyncComponent, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import ActionFab from '@/components/domain/ActionFab.vue'
import AppBrandHeader from '@/components/base/AppBrandHeader.vue'
import AppSortableTable from '@/components/base/AppSortableTable.vue'
import ProductRow from '@/components/domain/ProductRow.vue'
import ProductStatusBadge from '@/components/domain/ProductStatusBadge.vue'
import { listProducts } from '@/api/products'
import { useAsyncData } from '@/composables/useAsyncData'
import { useAddDialog } from '@/composables/useAddDialog'
import { compareText, sortRows, type TableColumn, type TableSort } from '@/composables/useTableSort'
import type { ProductDto } from '@/api/types'

const ProductAddView = defineAsyncComponent(() => import('@/views/ProductAddView.vue'))
const { mdAndUp, open: addOpen } = useAddDialog()

const includeInactive = ref(false)
const { data: products, loading, error, reload } = useAsyncData(() => listProducts(includeInactive.value), [])

watch(includeInactive, reload)

// --- Écran large : tableau trié (FR-018) ---------------------------------------------------------

const router = useRouter()

type ProductSortKey = 'name' | 'code' | 'stock' | 'status'

const columns: TableColumn<ProductSortKey>[] = [
  { id: 'name', label: 'Produit', sortKey: 'name' },
  { id: 'code', label: 'Code', sortKey: 'code' },
  { id: 'mode', label: 'Vente' },
  { id: 'partial', label: 'À la tranche' },
  { id: 'stock', label: 'En stock', sortKey: 'stock', firstDirection: 'desc', numeric: true },
  { id: 'status', label: 'État', sortKey: 'status' },
]

const sort = ref<TableSort<ProductSortKey>>({ key: 'name', direction: 'asc' })

function compareProducts(key: ProductSortKey, a: ProductDto, b: ProductDto): number {
  switch (key) {
    case 'name':
      return compareText(a.name, b.name)
    case 'code':
      return compareText(a.code, b.code)
    case 'stock':
      return a.remainingStockUnitCount - b.remainingStockUnitCount
    case 'status':
      // Ordre croissant : les produits actifs d'abord.
      return Number(b.isActive) - Number(a.isActive)
  }
}

const tableProducts = computed(() =>
  sortRows(products.value, sort.value, compareProducts, (a, b) => compareText(a.name, b.name)),
)
</script>

<template>
  <v-container class="products-view app-page-container">
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

    <AppSortableTable
      v-else-if="mdAndUp"
      v-model:sort="sort"
      :columns="columns"
      :rows="tableProducts"
      :row-key="(p) => p.id"
      :row-muted="(p) => !p.isActive"
      @row-click="(p) => router.push(`/products/${p.code}`)"
    >
      <template #row="{ row: p }">
        <td class="app-table__cell--strong">{{ p.name }}</td>
        <td class="app-table__cell--muted">{{ p.code }}</td>
        <td>{{ p.saleMode === 'by_weight' ? 'Au poids' : 'À la pièce' }}</td>
        <td>{{ p.saleMode === 'by_weight' && p.allowPartialSale ? 'Oui' : '—' }}</td>
        <td class="app-table__cell--numeric">
          <span v-if="p.remainingStockUnitCount > 0">{{ p.remainingStockUnitCount }}</span>
          <span v-else class="text-secondary">—</span>
        </td>
        <td><ProductStatusBadge :is-active="p.isActive" /></td>
      </template>
    </AppSortableTable>

    <div v-else class="products-view__list">
      <ProductRow v-for="product in products" :key="product.id" :product="product" />
    </div>

    <ActionFab label="Créer un produit" :to="mdAndUp ? undefined : '/products/add'" @click="addOpen = true" />

    <v-dialog v-model="addOpen">
      <ProductAddView v-if="addOpen" dialog @saved="addOpen = false; reload()" @cancel="addOpen = false" />
    </v-dialog>
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
