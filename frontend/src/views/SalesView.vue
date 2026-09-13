<script setup lang="ts">
import { computed, defineAsyncComponent, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useDisplay } from 'vuetify'
import AppFab from '@/components/base/AppFab.vue'
import AppBrandHeader from '@/components/base/AppBrandHeader.vue'
import AppSortableTable from '@/components/base/AppSortableTable.vue'
import CustomerPicker from '@/components/domain/CustomerPicker.vue'
import { listSales } from '@/api/sales'
import { useAsyncData } from '@/composables/useAsyncData'
import {
  emptySalesFilters,
  filterSales,
  hasActiveFilters,
  salesTotal,
  sortSales,
  type SalesSort,
  type SalesSortKey,
} from '@/composables/useSalesFilters'
import type { TableColumn } from '@/composables/useTableSort'
import type { SaleDto } from '@/api/types'
import { useAddDialog } from '@/composables/useAddDialog'

const SaleAddView = defineAsyncComponent(() => import('@/views/SaleAddView.vue'))

const { data: allSales, loading, error, reload } = useAsyncData(listSales, [] as SaleDto[])
const { open: addOpen } = useAddDialog()

// Sur écran large, la liste devient un tableau filtrable et triable ; sur téléphone, rien ne change.
const { mdAndUp } = useDisplay()
const router = useRouter()

const filters = reactive(emptySalesFilters())
const sort = ref<SalesSort>({ key: 'date', direction: 'desc' })

const tableSales = computed(() => sortSales(filterSales(allSales.value, filters), sort.value))
const tableTotal = computed(() => salesTotal(tableSales.value))
const filtering = computed(() => hasActiveFilters(filters))

// Date et montant se lisent d'abord du plus grand ; client et statut, dans l'ordre naturel.
const columns: TableColumn<SalesSortKey>[] = [
  { id: 'number', label: 'Numéro' },
  { id: 'date', label: 'Date', sortKey: 'date', firstDirection: 'desc' },
  { id: 'customer', label: 'Client', sortKey: 'customer' },
  { id: 'status', label: 'Paiement', sortKey: 'status' },
  { id: 'total', label: 'Montant', sortKey: 'total', firstDirection: 'desc', numeric: true },
]

function resetFilters() {
  Object.assign(filters, emptySalesFilters())
}

function formatEuros(value: number): string {
  return `${value.toLocaleString('fr-FR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} €`
}

const query = ref('')
const filtered = computed(() => {
  const q = query.value.trim().toLowerCase()
  if (!q) return allSales.value
  return allSales.value.filter(
    (s) => s.customerName.toLowerCase().includes(q) || s.saleNumber.toLowerCase().includes(q),
  )
})

const currentYear = new Date().getFullYear()
const yearSales = computed(() => allSales.value.filter((s) => new Date(s.date).getFullYear() === currentYear))
const yearRevenue = computed(() => yearSales.value.reduce((sum, s) => sum + s.total, 0))

interface MonthGroup {
  label: string
  subtotal: number
  sales: SaleDto[]
}

const groups = computed<MonthGroup[]>(() => {
  const byMonth = new Map<string, SaleDto[]>()
  for (const s of [...filtered.value].sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime())) {
    const key = s.date.slice(0, 7)
    const list = byMonth.get(key) ?? []
    list.push(s)
    byMonth.set(key, list)
  }
  return [...byMonth.entries()]
    .sort((a, b) => b[0].localeCompare(a[0]))
    .map(([key, sales]) => ({
      label: new Date(`${key}-15`).toLocaleDateString('fr-FR', { month: 'long', year: 'numeric' }),
      subtotal: sales.reduce((sum, s) => sum + s.total, 0),
      sales,
    }))
})
</script>

<template>
  <v-container class="sales-view app-page-container">
    <AppBrandHeader />

    <header class="sales-view__header">
      <h1 class="text-h4 font-weight-bold">Ventes</h1>

      <div class="sales-view__stats">
        <div class="sales-view__stat">
          <div class="sales-view__stat-value sales-view__stat-value--accent">
            {{ Math.round(yearRevenue).toLocaleString('fr-FR') }} €
          </div>
          <div class="sales-view__stat-label text-secondary">CA {{ currentYear }}</div>
        </div>
        <div class="sales-view__divider" />
        <div class="sales-view__stat">
          <div class="sales-view__stat-value">{{ yearSales.length }}</div>
          <div class="sales-view__stat-label text-secondary">ventes en {{ currentYear }}</div>
        </div>
      </div>

      <div v-if="!mdAndUp" class="sales-view__search">
        <v-icon size="20">phosphor:magnifying-glass</v-icon>
        <input v-model="query" type="text" placeholder="Client ou n° de vente" class="sales-view__search-input" />
      </div>
    </header>

    <p v-if="loading" class="text-secondary">Chargement...</p>
    <p v-else-if="error" class="text-error">{{ error }}</p>

    <!-- Écran large : filtres et tableau (FR-017, FR-018) -->
    <section v-else-if="mdAndUp" class="sales-view__desk">
      <div class="sales-view__filters">
        <div class="sales-view__filter sales-view__filter--customer">
          <span class="sales-view__filter-label">Client</span>
          <CustomerPicker v-model="filters.customerId" />
        </div>
        <label class="sales-view__filter">
          <span class="sales-view__filter-label">Paiement</span>
          <select v-model="filters.payment" class="sales-view__field">
            <option value="all">Toutes les ventes</option>
            <option value="pending">À payer</option>
            <option value="paid">Payées</option>
          </select>
        </label>
        <label class="sales-view__filter">
          <span class="sales-view__filter-label">Du</span>
          <input v-model="filters.from" type="date" class="sales-view__field" :max="filters.to || undefined" />
        </label>
        <label class="sales-view__filter">
          <span class="sales-view__filter-label">Au</span>
          <input v-model="filters.to" type="date" class="sales-view__field" :min="filters.from || undefined" />
        </label>
        <v-btn v-if="filtering" variant="text" color="secondary" class="sales-view__reset" @click="resetFilters">
          Effacer les filtres
        </v-btn>
      </div>

      <div class="sales-view__summary">
        <span>{{ tableSales.length }} vente{{ tableSales.length > 1 ? 's' : '' }}</span>
        <span class="sales-view__summary-total">{{ formatEuros(tableTotal) }}</span>
      </div>

      <p v-if="tableSales.length === 0" class="text-secondary sales-view__empty">
        {{ filtering ? 'Aucune vente ne correspond à ces filtres.' : 'Aucune vente enregistrée.' }}
      </p>

      <AppSortableTable
        v-else
        v-model:sort="sort"
        :columns="columns"
        :rows="tableSales"
        :row-key="(s) => s.id"
        @row-click="(s) => router.push(`/sales/${s.id}`)"
      >
        <template #row="{ row: s }">
          <td class="app-table__cell--muted">{{ s.saleNumber }}</td>
          <td>{{ new Date(s.date).toLocaleDateString('fr-FR', { day: 'numeric', month: 'long', year: 'numeric' }) }}</td>
          <td class="app-table__cell--strong">{{ s.customerName }}</td>
          <td>
            <span :class="s.paid ? 'sales-view__paid' : 'sales-view__pending'">
              {{ s.paid ? 'Payée' : 'À payer' }}
            </span>
          </td>
          <td class="app-table__cell--numeric app-table__cell--amount">{{ formatEuros(s.total) }}</td>
        </template>
      </AppSortableTable>
    </section>

    <p v-else-if="groups.length === 0" class="text-secondary sales-view__empty">Aucune vente trouvée.</p>

    <div v-else class="sales-view__groups">
      <section v-for="group in groups" :key="group.label" class="sales-view__group">
        <div class="sales-view__group-header">
          <div class="sales-view__group-label">{{ group.label }}</div>
          <div class="text-secondary font-weight-medium">
            {{ group.subtotal.toLocaleString('fr-FR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) }} €
          </div>
        </div>
        <div class="sales-view__list">
          <RouterLink v-for="s in group.sales" :key="s.id" :to="`/sales/${s.id}`" class="sales-view__row">
            <div class="sales-view__row-info">
              <div class="sales-view__row-client">{{ s.customerName }}</div>
              <div class="text-secondary">
                {{ s.saleNumber }} ·
                {{ new Date(s.date).toLocaleDateString('fr-FR', { day: 'numeric', month: 'long' }) }}
              </div>
            </div>
            <span v-if="!s.paid" class="sales-view__pending">À payer</span>
            <div class="sales-view__row-amount">
              {{ s.total.toLocaleString('fr-FR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) }} €
            </div>
          </RouterLink>
        </div>
      </section>
    </div>

    <AppFab icon="plus" ariaLabel="Nouvelle vente" :to="mdAndUp ? undefined : '/sales/add'" @click="addOpen = true" />

    <v-dialog v-model="addOpen">
      <SaleAddView v-if="addOpen" dialog @saved="addOpen = false; reload()" @cancel="addOpen = false" />
    </v-dialog>
  </v-container>
</template>

<style scoped>
.sales-view {
  padding-bottom: 96px;
}

.sales-view__header {
  display: flex;
  flex-direction: column;
  gap: 14px;
  padding: 0 4px 12px;
}

.sales-view__stats {
  display: flex;
  gap: 10px;
  background: rgb(var(--v-theme-surface));
  border-radius: 14px;
  padding: 16px 18px;
}

.sales-view__stat {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 3px;
  text-align: center;
}

.sales-view__stat-value {
  font-family: var(--font-heading);
  font-weight: 700;
  font-size: 26px;
  line-height: 1;
}

.sales-view__stat-value--accent {
  color: rgb(var(--v-theme-success));
}

.sales-view__stat-label {
  font-size: 13px;
  font-weight: 500;
}

.sales-view__divider {
  width: 1px;
  background: rgb(var(--v-theme-status-neutral-container));
}

.sales-view__search {
  display: flex;
  align-items: center;
  gap: 8px;
  background: rgb(var(--v-theme-surface));
  border-radius: 12px;
  padding: 0 14px;
  height: 48px;
  color: rgb(var(--v-theme-secondary));
}

.sales-view__search-input {
  flex: 1;
  min-width: 0;
  border: none;
  outline: none;
  background: none;
  font-family: var(--font-body);
  font-size: 17px;
  color: rgb(var(--v-theme-on-surface));
  height: 100%;
}

.sales-view__empty {
  text-align: center;
  padding: 40px 20px;
}

.sales-view__groups {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.sales-view__group-header {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  gap: 10px;
  padding: 0 4px;
  margin-bottom: 8px;
}

.sales-view__group-label {
  font-family: var(--font-heading);
  font-weight: 700;
  font-size: 19px;
  color: rgb(var(--v-theme-secondary));
  text-transform: capitalize;
}

.sales-view__list {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.sales-view__row {
  text-decoration: none;
  color: rgb(var(--v-theme-on-surface));
  background: rgb(var(--v-theme-surface));
  border-radius: 14px;
  padding: 14px 16px;
  display: flex;
  align-items: center;
  gap: 12px;
  box-shadow: 0 1px 2px rgba(43, 36, 30, 0.06);
  min-height: 48px;
}

.sales-view__row:hover {
  background: rgb(var(--v-theme-status-neutral-container));
}

.sales-view__row-info {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.sales-view__row-client {
  font-family: var(--font-heading);
  font-weight: 600;
  font-size: 19px;
  line-height: 1.2;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.sales-view__pending {
  background: rgb(var(--v-theme-warning-container));
  color: rgb(var(--v-theme-warning));
  font-size: 13px;
  font-weight: 600;
  padding: 3px 10px;
  border-radius: 999px;
  flex-shrink: 0;
}

.sales-view__row-amount {
  font-size: 18px;
  font-weight: 600;
  color: rgb(var(--v-theme-success));
  flex-shrink: 0;
}

/* --- Écran large : filtres et tableau ------------------------------------------------------- */

.sales-view__desk {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.sales-view__filters {
  display: flex;
  flex-wrap: wrap;
  align-items: flex-end;
  gap: 12px;
  background: rgb(var(--v-theme-surface));
  border-radius: 14px;
  padding: 14px 16px;
}

.sales-view__filter {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.sales-view__filter--customer {
  flex: 1;
  min-width: 260px;
}

/* Le sélecteur de client réserve une marge basse pour sa liste de résultats ; inutile dans ce bandeau. */
.sales-view__filter--customer :deep(.customer-picker__search) {
  margin-bottom: 0;
}

.sales-view__filter-label {
  font-size: 13px;
  font-weight: 600;
  color: rgb(var(--v-theme-secondary));
}

.sales-view__field {
  height: 52px;
  border: 1.5px solid rgb(var(--v-theme-field-border));
  border-radius: 10px;
  background: rgb(var(--v-theme-field-surface));
  padding: 0 12px;
  font-family: var(--font-body);
  font-size: 16px;
  color: rgb(var(--v-theme-on-surface));
}

.sales-view__reset {
  align-self: center;
}

.sales-view__summary {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  padding: 0 4px;
  font-size: 15px;
  font-weight: 600;
  color: rgb(var(--v-theme-secondary));
}

.sales-view__summary-total {
  font-family: var(--font-heading);
  font-size: 22px;
  font-weight: 700;
  color: rgb(var(--v-theme-success));
}

.sales-view__paid {
  background: rgb(var(--v-theme-success-container));
  color: rgb(var(--v-theme-success));
  font-size: 13px;
  font-weight: 600;
  padding: 3px 10px;
  border-radius: 999px;
}
</style>
