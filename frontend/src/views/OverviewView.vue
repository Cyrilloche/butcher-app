<script setup lang="ts">
import { computed, defineAsyncComponent, ref, watch } from 'vue'
import { listSales } from '@/api/sales'
import { getReceivables, getSalesSummary } from '@/api/reports'
import { useAddDialog } from '@/composables/useAddDialog'
import { useAsyncData } from '@/composables/useAsyncData'
import {
  availableYears,
  bestMonthIndex,
  comparisonPeriod,
  monthComparison,
  monthlyRevenue,
  receivables,
  recentSales,
  yearPeriod,
  yearSummary,
} from '@/composables/useOverview'
import { formatWeight } from '@/composables/useStock'
import { useAuthStore } from '@/stores/auth'
import type { ReceivablesDto, SaleDto, SalesSummaryDto, StockMovementDto } from '@/api/types'

/**
 * Vue d'ensemble du backoffice PC, d'après `design/backoffice/Backoffice Overview.dc.html`.
 *
 * Réservée à l'administrateur : ses chiffres relèvent des rapports (FR-027), et en viennent — les mêmes
 * que l'écran Rapports. La liste des ventes ne sert qu'aux années et aux ventes récentes. Le « stock bas »
 * de la maquette relève des alertes, prévues en V2 : ses deux emplacements l'annoncent.
 */
const auth = useAuthStore()
const today = new Date()
const selectedYear = ref(today.getFullYear())

const emptySummary: SalesSummaryDto = { saleCount: 0, total: 0, paidTotal: 0, pendingTotal: 0, months: [] }

const { data: sales, loading: salesLoading, error: salesError, reload: reloadSales } = useAsyncData(listSales, [] as SaleDto[])
const { data: yearReport, error: yearError, reload: reloadYear } = useAsyncData(
  () => getSalesSummary(yearPeriod(selectedYear.value).from, yearPeriod(selectedYear.value).to),
  emptySummary,
)
const {
  data: comparisonReport,
  loading: comparisonLoading,
  error: comparisonError,
  reload: reloadComparison,
} = useAsyncData(() => getSalesSummary(comparisonPeriod(today).from, comparisonPeriod(today).to), emptySummary)
const {
  data: receivablesReport,
  loading: receivablesLoading,
  error: receivablesError,
  reload: reloadReceivables,
} = useAsyncData(getReceivables, { total: 0, customers: [] } as ReceivablesDto)

// Changer d'année ne recharge que son rapport : la page reste affichée pendant ce temps.
watch(selectedYear, reloadYear)

const loading = computed(() => salesLoading.value || comparisonLoading.value || receivablesLoading.value)
const error = computed(() => salesError.value ?? yearError.value ?? comparisonError.value ?? receivablesError.value)

function onSaleSaved() {
  saleOpen.value = false
  reloadSales()
  reloadYear()
  reloadComparison()
  reloadReceivables()
}

const SaleAddView = defineAsyncComponent(() => import('@/views/SaleAddView.vue'))
const { mdAndUp, open: saleOpen } = useAddDialog()

const years = computed(() => availableYears(sales.value, today))

// Si l'année choisie disparaît de la liste (aucune donnée), on revient à la plus récente.
watch(years, (list) => {
  if (!list.includes(selectedYear.value)) selectedYear.value = list[0] ?? today.getFullYear()
})

const summary = computed(() => yearSummary(yearReport.value))
const monthly = computed(() => monthlyRevenue(yearReport.value, selectedYear.value))
const bestMonth = computed(() => bestMonthIndex(monthly.value))
const comparison = computed(() => monthComparison(comparisonReport.value, today))
const pending = computed(() => receivables(receivablesReport.value))
const recent = computed(() => recentSales(sales.value, 6))
const lastSale = computed(() => recent.value[0] ?? null)

const greeting = computed(() => {
  const salutation = today.getHours() < 18 ? 'Bonjour' : 'Bonsoir'
  const name = auth.account?.displayName
  return `${salutation}${name ? ` ${name}` : ''}, voici l'activité de Saloir.`
})

// --- Formats -------------------------------------------------------------------------------------

function euros(value: number, decimals: 0 | 2 = 2): string {
  return `${value.toLocaleString('fr-FR', { minimumFractionDigits: decimals, maximumFractionDigits: decimals })} €`
}

function monthName(monthIndex: number, style: 'long' | 'short' = 'long'): string {
  return new Date(2000, monthIndex, 1).toLocaleDateString('fr-FR', { month: style }).replace('.', '')
}

function dayLabel(sale: SaleDto): string {
  return new Date(sale.date).toLocaleDateString('fr-FR', { day: 'numeric', month: 'long' })
}

function lineQuantity(line: StockMovementDto): string {
  return line.soldWeight != null ? formatWeight(Math.round(line.soldWeight * 1000)) : 'À la pièce'
}

const trend = computed(() => {
  const change = comparison.value.changePercent
  if (change === null) return null
  return {
    label: `${change >= 0 ? '+' : ''}${change} % par rapport à ${monthName(comparison.value.previousMonth.getMonth())}`,
    rising: change >= 0,
  }
})

/** Barres du graphique : hauteur relative au meilleur mois, mois en cours mis en évidence. */
const bars = computed(() => {
  const max = Math.max(...monthly.value, 1)
  const isCurrentYear = selectedYear.value === today.getFullYear()
  return monthly.value.map((total, index) => ({
    index,
    total,
    height: `${((total / max) * 100).toFixed(1)}%`,
    isCurrent: isCurrentYear && index === today.getMonth(),
    shortLabel: monthName(index, 'short'),
    description: `${monthName(index)} ${selectedYear.value} : ${euros(total)}`,
  }))
})
</script>

<template>
  <v-container class="overview-view app-page-container">
    <header class="overview-view__header">
      <div>
        <h1 class="overview-view__title">Vue d'ensemble</h1>
        <p class="overview-view__greeting text-secondary">{{ greeting }}</p>
      </div>
      <v-btn
        color="primary"
        rounded="pill"
        size="large"
        :to="mdAndUp ? undefined : '/sales/add'"
        prepend-icon="phosphor:plus"
        @click="saleOpen = mdAndUp"
      >
        Nouvelle vente
      </v-btn>
    </header>

    <v-dialog v-model="saleOpen">
      <SaleAddView v-if="saleOpen" dialog @saved="onSaleSaved" @cancel="saleOpen = false" />
    </v-dialog>

    <p v-if="loading" class="text-secondary">Chargement...</p>
    <p v-else-if="error" class="text-error">{{ error }}</p>

    <template v-else>
      <!-- Chiffres clés -->
      <section class="overview-view__kpis" aria-label="Chiffres clés">
        <article class="overview-view__card">
          <div class="overview-view__kpi-head">
            <h2 class="overview-view__kpi-label">Chiffre d'affaires</h2>
            <select v-model.number="selectedYear" class="overview-view__year" aria-label="Année">
              <option v-for="year in years" :key="year" :value="year">{{ year }}</option>
            </select>
          </div>
          <div class="overview-view__kpi-value overview-view__kpi-value--success">
            {{ euros(summary.revenue, 0) }}
          </div>
          <div class="overview-view__kpi-note text-secondary">
            {{ summary.count }} vente{{ summary.count > 1 ? 's' : '' }}
            <template v-if="summary.averageBasket !== null"> · panier moyen {{ euros(summary.averageBasket) }}</template>
          </div>
        </article>

        <article class="overview-view__card">
          <h2 class="overview-view__kpi-label">Ce mois</h2>
          <div class="overview-view__kpi-value">{{ euros(comparison.current, 0) }}</div>
          <div
            v-if="trend"
            class="overview-view__kpi-note overview-view__trend"
            :class="trend.rising ? 'overview-view__trend--up' : 'overview-view__trend--down'"
          >
            <v-icon size="15">phosphor:{{ trend.rising ? 'trend-up' : 'trend-down' }}</v-icon>
            {{ trend.label }}
          </div>
          <div v-else class="overview-view__kpi-note text-secondary">Aucune vente le mois précédent</div>
        </article>

        <article class="overview-view__card">
          <h2 class="overview-view__kpi-label">À encaisser</h2>
          <div class="overview-view__kpi-value overview-view__kpi-value--warning">{{ euros(pending.total) }}</div>
          <div class="overview-view__kpi-note text-secondary">
            {{ pending.count }} vente{{ pending.count > 1 ? 's' : '' }} en attente de paiement
          </div>
        </article>

        <article class="overview-view__card overview-view__card--upcoming">
          <h2 class="overview-view__kpi-label">Stock bas</h2>
          <div class="overview-view__upcoming">Arrivera en V2</div>
          <div class="overview-view__kpi-note text-secondary">Alerte sur les produits sous un seuil</div>
        </article>
      </section>

      <!-- Chiffre d'affaires mensuel et dernière vente -->
      <section class="overview-view__row overview-view__row--wide-left">
        <article class="overview-view__card">
          <div class="overview-view__card-head">
            <h2 class="overview-view__card-title">CA mensuel {{ selectedYear }}</h2>
            <span v-if="bestMonth !== null" class="overview-view__card-hint text-secondary">
              meilleur mois : {{ monthName(bestMonth) }}
            </span>
          </div>
          <div class="overview-view__chart" role="img" :aria-label="`Chiffre d'affaires mensuel ${selectedYear}`">
            <div v-for="bar in bars" :key="bar.index" class="overview-view__bar-slot" :title="bar.description">
              <span class="overview-view__bar-value text-secondary">{{ bar.total > 0 ? euros(bar.total, 0) : '' }}</span>
              <span
                class="overview-view__bar"
                :class="{
                  'overview-view__bar--current': bar.isCurrent,
                  'overview-view__bar--empty': bar.total === 0,
                }"
                :style="{ height: bar.height }"
              />
            </div>
          </div>
          <div class="overview-view__chart-labels" aria-hidden="true">
            <span
              v-for="bar in bars"
              :key="bar.index"
              :class="{ 'overview-view__chart-label--current': bar.isCurrent }"
            >
              {{ bar.shortLabel }}
            </span>
          </div>
        </article>

        <article class="overview-view__card">
          <div class="overview-view__card-head">
            <h2 class="overview-view__card-title">Dernière vente</h2>
            <RouterLink v-if="lastSale" :to="`/sales/${lastSale.id}`" class="overview-view__link">Voir</RouterLink>
          </div>
          <template v-if="lastSale">
            <div>
              <div class="overview-view__last-client">{{ lastSale.customerName }}</div>
              <div class="text-secondary">{{ lastSale.saleNumber }} · {{ dayLabel(lastSale) }}</div>
            </div>
            <ul class="overview-view__last-lines">
              <li v-for="line in lastSale.lines" :key="line.id">
                <span>{{ line.productName }}</span>
                <span class="text-secondary">{{ lineQuantity(line) }}</span>
              </li>
            </ul>
            <div class="overview-view__last-footer">
              <span class="overview-view__badge" :class="lastSale.paid ? 'overview-view__badge--paid' : 'overview-view__badge--pending'">
                {{ lastSale.paid ? 'Payée' : 'À payer' }}
              </span>
              <span class="overview-view__last-total">{{ euros(lastSale.total) }}</span>
            </div>
          </template>
          <p v-else class="text-secondary">Aucune vente enregistrée.</p>
        </article>
      </section>

      <!-- Stock bas (V2) et ventes récentes -->
      <section class="overview-view__row overview-view__row--wide-right">
        <article class="overview-view__card overview-view__card--upcoming">
          <div class="overview-view__card-head">
            <h2 class="overview-view__card-title">Stock bas</h2>
            <RouterLink to="/" class="overview-view__link">Tout le stock</RouterLink>
          </div>
          <p class="overview-view__upcoming">Arrivera en V2</p>
          <p class="text-secondary overview-view__upcoming-text">
            Les produits qui passent sous un seuil d'unités s'afficheront ici, pour penser à relancer une
            fabrication.
          </p>
        </article>

        <article class="overview-view__card">
          <div class="overview-view__card-head">
            <h2 class="overview-view__card-title">Ventes récentes</h2>
            <RouterLink to="/sales" class="overview-view__link">Toutes les ventes</RouterLink>
          </div>
          <p v-if="recent.length === 0" class="text-secondary">Aucune vente enregistrée.</p>
          <RouterLink
            v-for="sale in recent"
            :key="sale.id"
            :to="`/sales/${sale.id}`"
            class="overview-view__recent"
          >
            <span class="overview-view__recent-info">
              <span class="overview-view__recent-client">{{ sale.customerName }}</span>
              <span class="text-secondary">{{ sale.saleNumber }} · {{ dayLabel(sale) }}</span>
            </span>
            <span class="overview-view__badge" :class="sale.paid ? 'overview-view__badge--paid' : 'overview-view__badge--pending'">
              {{ sale.paid ? 'Payée' : 'À payer' }}
            </span>
            <span class="overview-view__recent-total">{{ euros(sale.total) }}</span>
          </RouterLink>
        </article>
      </section>
    </template>
  </v-container>
</template>

<style scoped>
.overview-view {
  padding-block: 36px 48px;
  display: flex;
  flex-direction: column;
  gap: 28px;
}

.overview-view__header {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: 16px;
  flex-wrap: wrap;
}

.overview-view__title {
  font-family: var(--font-heading);
  font-weight: 700;
  font-size: 36px;
  line-height: 1.1;
  margin: 0;
}

.overview-view__greeting {
  font-size: 15px;
  margin: 4px 0 0;
}

.overview-view__kpis {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(210px, 1fr));
  gap: 16px;
}

.overview-view__card {
  background: rgb(var(--v-theme-surface));
  border-radius: 16px;
  padding: 20px 22px;
  display: flex;
  flex-direction: column;
  gap: 12px;
  box-shadow: 0 1px 2px rgba(43, 36, 30, 0.06);
  min-width: 0;
}

.overview-view__kpi-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
}

.overview-view__kpi-label {
  font-family: var(--font-body);
  font-size: 13px;
  font-weight: 600;
  color: rgb(var(--v-theme-secondary));
  text-transform: uppercase;
  letter-spacing: 0.04em;
  margin: 0;
}

.overview-view__year {
  border: 1px solid rgb(var(--v-theme-field-border));
  background: rgb(var(--v-theme-background));
  color: rgb(var(--v-theme-on-surface));
  font-family: var(--font-body);
  font-size: 13px;
  font-weight: 600;
  padding: 4px 10px;
  border-radius: 999px;
  cursor: pointer;
}

.overview-view__kpi-value {
  font-family: var(--font-heading);
  font-weight: 700;
  font-size: 38px;
  line-height: 1;
}

.overview-view__kpi-value--success {
  color: rgb(var(--v-theme-success));
}

.overview-view__kpi-value--warning {
  color: rgb(var(--v-theme-warning));
}

.overview-view__kpi-note {
  font-size: 13px;
}

.overview-view__trend {
  display: flex;
  align-items: center;
  gap: 6px;
  font-weight: 600;
}

.overview-view__trend--up {
  color: rgb(var(--v-theme-success));
}

.overview-view__trend--down {
  color: rgb(var(--v-theme-primary));
}

.overview-view__card--upcoming {
  background: rgb(var(--v-theme-status-neutral-container));
  box-shadow: none;
}

.overview-view__upcoming {
  font-family: var(--font-heading);
  font-weight: 700;
  font-size: 24px;
  line-height: 1.1;
  color: rgb(var(--v-theme-secondary));
  margin: 0;
}

.overview-view__upcoming-text {
  font-size: 14px;
  margin: 0;
}

.overview-view__row {
  display: grid;
  gap: 20px;
}

.overview-view__row--wide-left {
  grid-template-columns: minmax(0, 3fr) minmax(0, 2fr);
}

.overview-view__row--wide-right {
  grid-template-columns: minmax(0, 2fr) minmax(0, 3fr);
}

/* Sous la largeur de travail de la maquette, les paires de cartes s'empilent. */
@media (max-width: 1144px) {
  .overview-view {
    padding-block: 24px 40px;
  }

  .overview-view__row--wide-left,
  .overview-view__row--wide-right {
    grid-template-columns: minmax(0, 1fr);
  }
}

.overview-view__card-head {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  gap: 12px;
}

.overview-view__card-title {
  font-family: var(--font-heading);
  font-weight: 700;
  font-size: 22px;
  margin: 0;
}

.overview-view__card-hint {
  font-size: 13px;
}

.overview-view__link {
  font-size: 13px;
  font-weight: 600;
  text-decoration: none;
  color: rgb(var(--v-theme-primary));
}

.overview-view__chart {
  display: grid;
  grid-template-columns: repeat(12, minmax(0, 1fr));
  gap: 8px;
  align-items: end;
  height: 180px;
}

.overview-view__bar-slot {
  display: flex;
  flex-direction: column;
  justify-content: flex-end;
  gap: 6px;
  height: 100%;
}

.overview-view__bar-value {
  font-size: 11px;
  font-weight: 600;
  text-align: center;
  min-height: 14px;
  white-space: nowrap;
}

.overview-view__bar {
  display: block;
  min-height: 3px;
  border-radius: 6px 6px 3px 3px;
  background: rgb(var(--v-theme-success));
}

.overview-view__bar--empty {
  background: rgb(var(--v-theme-status-neutral-container));
}

.overview-view__bar--current {
  background: rgb(var(--v-theme-primary));
}

.overview-view__chart-labels {
  display: grid;
  grid-template-columns: repeat(12, minmax(0, 1fr));
  gap: 8px;
  font-size: 12px;
  font-weight: 500;
  text-align: center;
  text-transform: capitalize;
  color: rgb(var(--v-theme-secondary));
}

.overview-view__chart-label--current {
  color: rgb(var(--v-theme-primary));
}

.overview-view__last-client {
  font-family: var(--font-heading);
  font-weight: 600;
  font-size: 24px;
  line-height: 1.2;
}

.overview-view__last-lines {
  list-style: none;
  margin: 0;
  padding: 14px 0 0;
  border-top: 1px solid rgb(var(--v-theme-status-neutral-container));
  display: flex;
  flex-direction: column;
  gap: 8px;
  font-size: 14px;
}

.overview-view__last-lines li {
  display: flex;
  justify-content: space-between;
  gap: 12px;
}

.overview-view__last-footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding-top: 14px;
  border-top: 1px solid rgb(var(--v-theme-status-neutral-container));
}

.overview-view__last-total {
  font-family: var(--font-heading);
  font-weight: 700;
  font-size: 28px;
  color: rgb(var(--v-theme-success));
}

.overview-view__badge {
  font-size: 12px;
  font-weight: 600;
  padding: 3px 10px;
  border-radius: 999px;
  white-space: nowrap;
}

.overview-view__badge--paid {
  background: rgb(var(--v-theme-success-container));
  color: rgb(var(--v-theme-success));
}

.overview-view__badge--pending {
  background: rgb(var(--v-theme-warning-container));
  color: rgb(var(--v-theme-warning));
}

.overview-view__recent {
  display: grid;
  grid-template-columns: minmax(0, 1fr) auto auto;
  align-items: center;
  gap: 16px;
  padding: 12px 8px;
  border-bottom: 1px solid rgb(var(--v-theme-status-neutral-container));
  border-radius: 8px;
  text-decoration: none;
  color: rgb(var(--v-theme-on-surface));
}

.overview-view__recent:hover {
  background: rgb(var(--v-theme-status-neutral-container));
}

.overview-view__recent-info {
  display: flex;
  flex-direction: column;
  gap: 2px;
  min-width: 0;
  font-size: 13px;
}

.overview-view__recent-client {
  font-size: 15px;
  font-weight: 600;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.overview-view__recent-total {
  font-size: 16px;
  font-weight: 600;
  color: rgb(var(--v-theme-success));
  min-width: 80px;
  text-align: right;
}
</style>
