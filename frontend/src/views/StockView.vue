<!-- src/views/StockView.vue -->
<script setup lang="ts">
import { computed, defineAsyncComponent, ref } from 'vue'
import { useRouter } from 'vue-router'
import ActionFab from '@/components/domain/ActionFab.vue'
import AppBadge from '@/components/base/AppBadge.vue'
import AppBrandHeader from '@/components/base/AppBrandHeader.vue'
import AppSortableTable from '@/components/base/AppSortableTable.vue'
import StockProductRow from '@/components/domain/StockProductRow.vue'
import { formatWeight, getStockDashboard, type StockDashboardProduct } from '@/composables/useStock'
import { useAsyncData } from '@/composables/useAsyncData'
import { useAddDialog } from '@/composables/useAddDialog'
import { compareText, sortRows, type TableColumn, type TableSort } from '@/composables/useTableSort'

const StockAddView = defineAsyncComponent(() => import('@/views/StockAddView.vue'))

const {
  data: dashboard,
  loading,
  error,
  reload,
} = useAsyncData(getStockDashboard, { products: [], totalUnitsInStock: 0 })
const { mdAndUp, open: addOpen } = useAddDialog()
const router = useRouter()

// Sur écran large, un tableau trié (FR-018) ; sur téléphone, les cartes de la maquette mobile.
type StockSortKey = 'name' | 'code' | 'qty' | 'opened' | 'remaining'

const columns: TableColumn<StockSortKey>[] = [
  { id: 'name', label: 'Produit', sortKey: 'name' },
  { id: 'code', label: 'Code', sortKey: 'code' },
  { id: 'mode', label: 'Vente' },
  { id: 'qty', label: 'En stock', sortKey: 'qty', firstDirection: 'desc', numeric: true },
  { id: 'opened', label: 'Entamés', sortKey: 'opened', firstDirection: 'desc', numeric: true },
  { id: 'remaining', label: 'Poids à vendre', sortKey: 'remaining', firstDirection: 'desc', numeric: true },
]

const sort = ref<TableSort<StockSortKey>>({ key: 'name', direction: 'asc' })

function compareStock(key: StockSortKey, a: StockDashboardProduct, b: StockDashboardProduct): number {
  switch (key) {
    case 'name':
      return compareText(a.name, b.name)
    case 'code':
      return compareText(a.code, b.code)
    case 'qty':
      return a.qty - b.qty
    case 'opened':
      return a.openedCount - b.openedCount
    case 'remaining':
      // Un produit à la pièce n'a pas de poids : il se range après le plus léger.
      return (a.remainingGrams ?? -1) - (b.remainingGrams ?? -1)
  }
}

const tableProducts = computed(() =>
  sortRows(dashboard.value.products, sort.value, compareStock, (a, b) => compareText(a.name, b.name)),
)
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

    <AppSortableTable
      v-else-if="mdAndUp"
      v-model:sort="sort"
      :columns="columns"
      :rows="tableProducts"
      :row-key="(p) => p.code"
      @row-click="(p) => router.push(p.href)"
    >
      <template #row="{ row: p }">
        <td class="app-table__cell--strong">{{ p.name }}</td>
        <td class="app-table__cell--muted">{{ p.code }}</td>
        <td>{{ p.saleModeLabel }}</td>
        <td class="app-table__cell--numeric">
          <AppBadge v-if="p.isEmpty">Épuisé</AppBadge>
          <span v-else class="stock-view__qty">{{ p.qty }} {{ p.qtyLabel }}</span>
        </td>
        <td class="app-table__cell--numeric">
          <AppBadge v-if="p.openedCount > 0" tone="warning">{{ p.openedCount }}</AppBadge>
          <span v-else class="text-secondary">—</span>
        </td>
        <td class="app-table__cell--numeric">
          <span v-if="p.remainingGrams">{{ formatWeight(p.remainingGrams) }}</span>
          <span v-else class="text-secondary">—</span>
        </td>
      </template>
    </AppSortableTable>

    <div v-else class="stock-view__list">
      <StockProductRow v-for="product in dashboard.products" :key="product.code" :product="product" />
    </div>

    <ActionFab label="Ajouter des produits au stock" :to="mdAndUp ? undefined : '/stock/add'" @click="addOpen = true" />

    <v-dialog v-model="addOpen">
      <StockAddView v-if="addOpen" dialog @saved="addOpen = false; reload()" @cancel="addOpen = false" />
    </v-dialog>
  </v-container>
</template>

<style scoped>
.stock-view {
  padding-bottom: 96px;
}

.stock-view__qty {
  font-weight: 600;
  color: rgb(var(--v-theme-success));
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
