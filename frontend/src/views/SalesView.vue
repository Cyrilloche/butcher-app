<script setup lang="ts">
import { computed, ref } from 'vue'
import AppFab from '@/components/base/AppFab.vue'
import AppBrandHeader from '@/components/base/AppBrandHeader.vue'
import { listSales } from '@/api/sales'
import { useAsyncData } from '@/composables/useAsyncData'
import type { SaleDto } from '@/api/types'

const { data: allSales, loading, error } = useAsyncData(listSales, [] as SaleDto[])

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
  <v-container class="sales-view">
    <header class="sales-view__header">
      <AppBrandHeader />
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

      <div class="sales-view__search">
        <v-icon size="20">phosphor:magnifying-glass</v-icon>
        <input v-model="query" type="text" placeholder="Client ou n° de vente" class="sales-view__search-input" />
      </div>
    </header>

    <p v-if="loading" class="text-secondary">Chargement...</p>
    <p v-else-if="error" class="text-error">{{ error }}</p>
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

    <AppFab icon="plus" ariaLabel="Nouvelle vente" to="/sales/add" />
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
  padding: 8px 4px 12px;
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
</style>
