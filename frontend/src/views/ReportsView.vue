<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import AppBadge from '@/components/base/AppBadge.vue'
import AppSortableTable from '@/components/base/AppSortableTable.vue'
import AssistantUsageReport from '@/components/domain/AssistantUsageReport.vue'
import { getReceivables, getSalesByCustomer, getSalesByProduct, getSalesSummary } from '@/api/reports'
import { useAsyncData } from '@/composables/useAsyncData'
import {
  daysSince,
  describePeriod,
  formatAge,
  matchingPreset,
  monthLabel,
  periodPresets,
  presetPeriod,
  type PeriodPreset,
} from '@/composables/useReportPeriod'
import { compareText, sortRows, type TableColumn, type TableSort } from '@/composables/useTableSort'
import type { CustomerSalesDto, MonthlySalesDto, ProductSalesDto, ReceivablesDto, SalesSummaryDto } from '@/api/types'

/**
 * Rapports de ventes, réservés à l'administrateur (FR-027 à FR-029). Conçus pour le PC.
 *
 * Tous les montants viennent du serveur, qui additionne les montants saisis sur les lignes (FR-030) ;
 * aucune notion de coût ni de marge (FR-031). La période s'ouvre sur l'année en cours (clarification du
 * 2026-09-13). « À encaisser » ne dépend pas de la période : une dette ne s'efface pas en changeant de dates.
 */
const router = useRouter()
const today = new Date()

const period = reactive(presetPeriod('this_year', today))
const activePreset = computed(() => matchingPreset(period, today))
const validPeriod = computed(() => period.from !== '' && period.to !== '' && period.from <= period.to)

function applyPreset(preset: PeriodPreset) {
  Object.assign(period, presetPeriod(preset, today))
}

const emptySummary: SalesSummaryDto = { saleCount: 0, total: 0, paidTotal: 0, pendingTotal: 0, months: [] }

const {
  data: summary,
  loading: summaryLoading,
  error: summaryError,
  reload: reloadSummary,
} = useAsyncData(() => (validPeriod.value ? getSalesSummary(period.from, period.to) : Promise.resolve(emptySummary)), emptySummary)
const {
  data: customers,
  error: customersError,
  reload: reloadCustomers,
} = useAsyncData(
  () => (validPeriod.value ? getSalesByCustomer(period.from, period.to) : Promise.resolve([] as CustomerSalesDto[])),
  [] as CustomerSalesDto[],
)
const {
  data: products,
  error: productsError,
  reload: reloadProducts,
} = useAsyncData(
  () => (validPeriod.value ? getSalesByProduct(period.from, period.to) : Promise.resolve([] as ProductSalesDto[])),
  [] as ProductSalesDto[],
)
const { data: receivables, error: receivablesError } = useAsyncData(getReceivables, { total: 0, customers: [] } as ReceivablesDto)

watch(period, () => {
  if (!validPeriod.value) return
  reloadSummary()
  reloadCustomers()
  reloadProducts()
})

const periodError = computed(() => summaryError.value ?? customersError.value ?? productsError.value)

// --- Formats -------------------------------------------------------------------------------------

function euros(value: number): string {
  return `${value.toLocaleString('fr-FR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} €`
}

function kilograms(value: number): string {
  return `${value.toLocaleString('fr-FR', { maximumFractionDigits: 3 })} kg`
}

function dayLabel(iso: string, withYear = true): string {
  return new Date(iso).toLocaleDateString('fr-FR', { day: 'numeric', month: 'long', ...(withYear ? { year: 'numeric' } : {}) })
}

// --- Tableaux ------------------------------------------------------------------------------------

const monthColumns: TableColumn<never>[] = [
  { id: 'month', label: 'Mois' },
  { id: 'count', label: 'Ventes', numeric: true },
  { id: 'total', label: 'Total', numeric: true },
  { id: 'paid', label: 'Encaissé', numeric: true },
  { id: 'pending', label: 'À encaisser', numeric: true },
]

type CustomerSortKey = 'name' | 'count' | 'total' | 'pending'

const customerColumns: TableColumn<CustomerSortKey>[] = [
  { id: 'name', label: 'Client', sortKey: 'name' },
  { id: 'count', label: 'Ventes', sortKey: 'count', firstDirection: 'desc', numeric: true },
  { id: 'total', label: 'Total', sortKey: 'total', firstDirection: 'desc', numeric: true },
  { id: 'pending', label: 'À encaisser', sortKey: 'pending', firstDirection: 'desc', numeric: true },
]
const customerSort = ref<TableSort<CustomerSortKey>>({ key: 'total', direction: 'desc' })

function compareCustomers(key: CustomerSortKey, a: CustomerSalesDto, b: CustomerSalesDto): number {
  switch (key) {
    case 'name':
      return compareText(a.customerName, b.customerName)
    case 'count':
      return a.saleCount - b.saleCount
    case 'total':
      return a.total - b.total
    case 'pending':
      return a.pendingTotal - b.pendingTotal
  }
}

const sortedCustomers = computed(() =>
  sortRows(customers.value, customerSort.value, compareCustomers, (a, b) => compareText(a.customerName, b.customerName)),
)

type ProductSortKey = 'name' | 'units' | 'lines' | 'weight' | 'total'

const productColumns: TableColumn<ProductSortKey>[] = [
  { id: 'name', label: 'Produit', sortKey: 'name' },
  { id: 'code', label: 'Code' },
  { id: 'units', label: 'Unités', sortKey: 'units', firstDirection: 'desc', numeric: true },
  { id: 'lines', label: 'Lignes de vente', sortKey: 'lines', firstDirection: 'desc', numeric: true },
  { id: 'weight', label: 'Poids vendu', sortKey: 'weight', firstDirection: 'desc', numeric: true },
  { id: 'total', label: 'Total', sortKey: 'total', firstDirection: 'desc', numeric: true },
]
const productSort = ref<TableSort<ProductSortKey>>({ key: 'total', direction: 'desc' })

function compareProducts(key: ProductSortKey, a: ProductSalesDto, b: ProductSalesDto): number {
  switch (key) {
    case 'name':
      return compareText(a.productName, b.productName)
    case 'units':
      return a.unitCount - b.unitCount
    case 'lines':
      return a.lineCount - b.lineCount
    case 'weight':
      // Un produit à la pièce n'a pas de poids : il se range après le plus léger.
      return (a.soldWeight ?? -1) - (b.soldWeight ?? -1)
    case 'total':
      return a.total - b.total
  }
}

const sortedProducts = computed(() =>
  sortRows(products.value, productSort.value, compareProducts, (a, b) => compareText(a.productName, b.productName)),
)

const pendingSaleCount = computed(() => receivables.value.customers.reduce((count, c) => count + c.sales.length, 0))
</script>

<template>
  <v-container class="reports-view app-page-container">
    <header class="reports-view__header">
      <h1 class="reports-view__title">Rapports</h1>
      <p class="reports-view__subtitle text-secondary">Ce qui a été vendu, et ce qui reste à encaisser.</p>
    </header>

    <!-- Période -->
    <section class="reports-view__period" aria-label="Période">
      <div class="reports-view__presets">
        <button
          v-for="preset in periodPresets"
          :key="preset.value"
          type="button"
          class="reports-view__preset"
          :class="{ 'reports-view__preset--active': activePreset === preset.value }"
          :aria-pressed="activePreset === preset.value"
          @click="applyPreset(preset.value)"
        >
          {{ preset.label }}
        </button>
      </div>
      <div class="reports-view__dates">
        <label class="reports-view__filter">
          <span class="reports-view__filter-label">Du</span>
          <input v-model="period.from" type="date" class="reports-view__field" :max="period.to || undefined" />
        </label>
        <label class="reports-view__filter">
          <span class="reports-view__filter-label">Au</span>
          <input v-model="period.to" type="date" class="reports-view__field" :min="period.from || undefined" />
        </label>
      </div>
      <p v-if="!validPeriod" class="text-error reports-view__period-note">
        Choisissez une date de début et une date de fin, la première avant la seconde.
      </p>
      <p v-else class="text-secondary reports-view__period-note">{{ describePeriod(period) }}</p>
    </section>

    <p v-if="periodError" class="text-error">{{ periodError }}</p>

    <!-- Synthèse -->
    <section class="reports-view__kpis" aria-label="Synthèse de la période" :aria-busy="summaryLoading">
      <article class="reports-view__card">
        <h2 class="reports-view__kpi-label">Ventes</h2>
        <div class="reports-view__kpi-value">{{ summary.saleCount }}</div>
      </article>
      <article class="reports-view__card">
        <h2 class="reports-view__kpi-label">Montant total</h2>
        <div class="reports-view__kpi-value">{{ euros(summary.total) }}</div>
      </article>
      <article class="reports-view__card">
        <h2 class="reports-view__kpi-label">Encaissé</h2>
        <div class="reports-view__kpi-value reports-view__kpi-value--success">{{ euros(summary.paidTotal) }}</div>
      </article>
      <article class="reports-view__card">
        <h2 class="reports-view__kpi-label">À encaisser sur la période</h2>
        <div class="reports-view__kpi-value reports-view__kpi-value--warning">{{ euros(summary.pendingTotal) }}</div>
      </article>
    </section>

    <!-- Par mois -->
    <section class="reports-view__section">
      <h2 class="reports-view__section-title">Par mois</h2>
      <p v-if="summary.months.length === 0" class="text-secondary reports-view__empty">Aucune vente sur cette période.</p>
      <AppSortableTable v-else :columns="monthColumns" :rows="summary.months" :row-key="(m: MonthlySalesDto) => m.month">
        <template #row="{ row: m }">
          <td class="app-table__cell--strong reports-view__month">{{ monthLabel(m.month) }}</td>
          <td class="app-table__cell--numeric">{{ m.saleCount }}</td>
          <td class="app-table__cell--numeric app-table__cell--amount">{{ euros(m.total) }}</td>
          <td class="app-table__cell--numeric">{{ euros(m.paidTotal) }}</td>
          <td class="app-table__cell--numeric">
            <AppBadge v-if="m.pendingTotal > 0" tone="warning">{{ euros(m.pendingTotal) }}</AppBadge>
            <span v-else class="text-secondary">—</span>
          </td>
        </template>
      </AppSortableTable>
    </section>

    <!-- Par client -->
    <section class="reports-view__section">
      <h2 class="reports-view__section-title">Par client</h2>
      <p v-if="customers.length === 0" class="text-secondary reports-view__empty">Aucune vente sur cette période.</p>
      <AppSortableTable
        v-else
        v-model:sort="customerSort"
        :columns="customerColumns"
        :rows="sortedCustomers"
        :row-key="(c) => c.customerId"
        @row-click="(c) => router.push(`/customers/${c.customerId}`)"
      >
        <template #row="{ row: c }">
          <td class="app-table__cell--strong">{{ c.customerName }}</td>
          <td class="app-table__cell--numeric">{{ c.saleCount }}</td>
          <td class="app-table__cell--numeric app-table__cell--amount">{{ euros(c.total) }}</td>
          <td class="app-table__cell--numeric">
            <AppBadge v-if="c.pendingTotal > 0" tone="warning">{{ euros(c.pendingTotal) }}</AppBadge>
            <span v-else class="text-secondary">—</span>
          </td>
        </template>
      </AppSortableTable>
    </section>

    <!-- Par produit -->
    <section class="reports-view__section">
      <h2 class="reports-view__section-title">Par produit</h2>
      <p class="text-secondary reports-view__hint">
        Un jambon vendu en plusieurs tranches compte pour une unité, et pour autant de lignes de vente que de tranches.
      </p>
      <p v-if="products.length === 0" class="text-secondary reports-view__empty">Aucune vente sur cette période.</p>
      <AppSortableTable
        v-else
        v-model:sort="productSort"
        :columns="productColumns"
        :rows="sortedProducts"
        :row-key="(p) => p.productId"
        @row-click="(p) => router.push(`/products/${p.productCode}`)"
      >
        <template #row="{ row: p }">
          <td class="app-table__cell--strong">{{ p.productName }}</td>
          <td class="app-table__cell--muted">{{ p.productCode }}</td>
          <td class="app-table__cell--numeric">{{ p.unitCount }}</td>
          <td class="app-table__cell--numeric">{{ p.lineCount }}</td>
          <td class="app-table__cell--numeric">
            <span v-if="p.soldWeight !== null">{{ kilograms(p.soldWeight) }}</span>
            <span v-else class="text-secondary">À la pièce</span>
          </td>
          <td class="app-table__cell--numeric app-table__cell--amount">{{ euros(p.total) }}</td>
        </template>
      </AppSortableTable>
    </section>

    <!-- Assistant vocal (RF-36) -->
    <AssistantUsageReport v-if="validPeriod" :from="period.from" :to="period.to" class="reports-view__section" />

    <!-- À encaisser -->
    <section class="reports-view__section">
      <div class="reports-view__section-head">
        <h2 class="reports-view__section-title">À encaisser</h2>
        <span class="text-secondary">Toutes périodes confondues</span>
      </div>
      <p v-if="receivablesError" class="text-error">{{ receivablesError }}</p>
      <p v-else-if="receivables.customers.length === 0" class="text-secondary reports-view__empty">
        Aucune vente en attente de paiement.
      </p>
      <template v-else>
        <p class="reports-view__receivables-total">
          <span class="reports-view__kpi-value reports-view__kpi-value--warning">{{ euros(receivables.total) }}</span>
          <span class="text-secondary">
            {{ pendingSaleCount }} vente{{ pendingSaleCount > 1 ? 's' : '' }} ·
            {{ receivables.customers.length }} client{{ receivables.customers.length > 1 ? 's' : '' }}
          </span>
        </p>
        <div class="reports-view__debtors">
          <article v-for="debtor in receivables.customers" :key="debtor.customerId" class="reports-view__card reports-view__debtor">
            <div class="reports-view__debtor-head">
              <RouterLink :to="`/customers/${debtor.customerId}`" class="reports-view__debtor-name">
                {{ debtor.customerName }}
              </RouterLink>
              <span class="reports-view__debtor-amount">{{ euros(debtor.pendingTotal) }}</span>
            </div>
            <div class="text-secondary reports-view__debtor-age">
              Plus ancienne vente impayée le {{ dayLabel(debtor.oldestUnpaidDate) }},
              {{ formatAge(daysSince(debtor.oldestUnpaidDate, today)) }}
            </div>
            <div class="reports-view__debtor-sales">
              <RouterLink
                v-for="sale in debtor.sales"
                :key="sale.id"
                :to="`/sales/${sale.id}`"
                class="reports-view__debtor-sale"
              >
                {{ sale.saleNumber }} · {{ dayLabel(sale.date, false) }} · {{ euros(sale.total) }}
              </RouterLink>
            </div>
          </article>
        </div>
      </template>
    </section>
  </v-container>
</template>

<style scoped>
.reports-view {
  padding-block: 24px 48px;
  display: flex;
  flex-direction: column;
  gap: 24px;
}

.reports-view__title {
  font-family: var(--font-heading);
  font-size: 36px;
  font-weight: 700;
  line-height: 1.1;
  margin: 0;
}

.reports-view__subtitle {
  font-size: 15px;
  margin: 4px 0 0;
}

.reports-view__period {
  display: flex;
  flex-wrap: wrap;
  align-items: flex-end;
  gap: 12px 20px;
  background: rgb(var(--v-theme-surface));
  border-radius: 14px;
  padding: 14px 16px;
}

.reports-view__presets {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.reports-view__preset {
  min-height: 44px;
  padding: 0 16px;
  border: 1.5px solid rgb(var(--v-theme-field-border));
  border-radius: 999px;
  background: rgb(var(--v-theme-field-surface));
  color: rgb(var(--v-theme-on-surface));
  font-family: var(--font-body);
  font-size: 15px;
  font-weight: 500;
  cursor: pointer;
}

.reports-view__preset--active {
  border-color: rgb(var(--v-theme-primary));
  background: rgb(var(--v-theme-primary));
  color: rgb(var(--v-theme-surface));
}

.reports-view__dates {
  display: flex;
  gap: 12px;
  flex-wrap: wrap;
}

.reports-view__filter {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.reports-view__filter-label {
  font-size: 13px;
  font-weight: 600;
  color: rgb(var(--v-theme-secondary));
}

.reports-view__field {
  height: 44px;
  border: 1.5px solid rgb(var(--v-theme-field-border));
  border-radius: 10px;
  background: rgb(var(--v-theme-field-surface));
  padding: 0 12px;
  font-family: var(--font-body);
  font-size: 16px;
  color: rgb(var(--v-theme-on-surface));
}

.reports-view__period-note {
  flex-basis: 100%;
  font-size: 14px;
  margin: 0;
}

.reports-view__kpis {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 16px;
}

.reports-view__card {
  background: rgb(var(--v-theme-surface));
  border-radius: 16px;
  padding: 18px 20px;
  display: flex;
  flex-direction: column;
  gap: 10px;
  box-shadow: 0 1px 2px rgba(43, 36, 30, 0.06);
  min-width: 0;
}

.reports-view__kpi-label {
  font-family: var(--font-body);
  font-size: 13px;
  font-weight: 600;
  color: rgb(var(--v-theme-secondary));
  text-transform: uppercase;
  letter-spacing: 0.04em;
  margin: 0;
}

.reports-view__kpi-value {
  font-family: var(--font-heading);
  font-weight: 700;
  font-size: 32px;
  line-height: 1;
}

.reports-view__kpi-value--success {
  color: rgb(var(--v-theme-success));
}

.reports-view__kpi-value--warning {
  color: rgb(var(--v-theme-warning));
}

.reports-view__section {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.reports-view__section-head {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  gap: 12px;
  flex-wrap: wrap;
}

.reports-view__section-title {
  font-family: var(--font-heading);
  font-size: 24px;
  font-weight: 700;
  margin: 0;
  padding: 0 4px;
}

.reports-view__hint {
  font-size: 14px;
  margin: 0;
  padding: 0 4px;
}

.reports-view__empty {
  background: rgb(var(--v-theme-surface));
  border-radius: 14px;
  padding: 20px;
  text-align: center;
  margin: 0;
}

.reports-view__month {
  text-transform: capitalize;
}

.reports-view__receivables-total {
  display: flex;
  align-items: baseline;
  gap: 12px;
  flex-wrap: wrap;
  margin: 0;
  padding: 0 4px;
}

.reports-view__debtors {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
  gap: 14px;
}

.reports-view__debtor-head {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  gap: 12px;
}

.reports-view__debtor-name {
  font-family: var(--font-heading);
  font-size: 20px;
  font-weight: 600;
  color: rgb(var(--v-theme-on-surface));
  text-decoration: none;
}

.reports-view__debtor-name:hover {
  color: rgb(var(--v-theme-primary));
}

.reports-view__debtor-amount {
  font-size: 18px;
  font-weight: 700;
  color: rgb(var(--v-theme-warning));
  white-space: nowrap;
}

.reports-view__debtor-age {
  font-size: 14px;
}

.reports-view__debtor-sales {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
}

.reports-view__debtor-sale {
  font-size: 13px;
  font-weight: 600;
  padding: 4px 10px;
  border-radius: 999px;
  background: rgb(var(--v-theme-warning-container));
  color: rgb(var(--v-theme-warning));
  text-decoration: none;
  white-space: nowrap;
}

.reports-view__debtor-sale:hover {
  background: rgb(var(--v-theme-warning));
  color: rgb(var(--v-theme-surface));
}
</style>
