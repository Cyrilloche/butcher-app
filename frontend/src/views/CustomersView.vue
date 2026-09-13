<script setup lang="ts">
import { computed, defineAsyncComponent, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import AppFab from '@/components/base/AppFab.vue'
import AppBadge from '@/components/base/AppBadge.vue'
import AppBrandHeader from '@/components/base/AppBrandHeader.vue'
import AppSortableTable from '@/components/base/AppSortableTable.vue'
import CustomerRow from '@/components/domain/CustomerRow.vue'
import { listCustomers } from '@/api/customers'
import { listSales } from '@/api/sales'
import { useAsyncData } from '@/composables/useAsyncData'
import {
  customerFullName,
  customerPurchaseStats,
  customerSortKey,
  groupCustomersByLetter,
  NO_PURCHASES,
  type CustomerPurchaseStats,
} from '@/composables/useCustomers'
import { useAddDialog } from '@/composables/useAddDialog'
import { compareText, sortRows, type TableColumn, type TableSort } from '@/composables/useTableSort'
import type { CustomerDto, SaleDto } from '@/api/types'

const CustomerAddView = defineAsyncComponent(() => import('@/views/CustomerAddView.vue'))

const { mdAndUp, open: addOpen } = useAddDialog()
const router = useRouter()

const { data: customers, loading, error, reload } = useAsyncData(listCustomers, [])

// Les achats ne servent qu'au tableau de l'écran large : sur téléphone, on n'appelle pas les ventes.
const {
  data: sales,
  loading: salesLoading,
  reload: reloadSales,
} = useAsyncData(() => (mdAndUp.value ? listSales() : Promise.resolve([] as SaleDto[])), [] as SaleDto[])
watch(mdAndUp, (wide) => {
  if (wide) reloadSales()
})

function onCustomerSaved() {
  addOpen.value = false
  reload()
}

const query = ref('')
const filtered = computed(() => {
  const q = query.value.trim().toLowerCase()
  if (!q) return customers.value
  const qDigits = q.replace(/\D/g, '')
  return customers.value.filter((c) => {
    const nameHit = customerFullName(c).toLowerCase().includes(q)
    const phoneHit = qDigits.length > 0 && (c.phone ?? '').replace(/\D/g, '').includes(qDigits)
    return nameHit || phoneHit
  })
})

const groups = computed(() => groupCustomersByLetter(filtered.value))

/**
 * Index alphabétique (maquette "Clients Dashboard") : les 26 lettres sont
 * toujours affichées — celles sans client sont grisées et inertes — pour que
 * le repère visuel ne bouge pas d'une recherche à l'autre.
 */
const ALPHABET = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ'.split('')

const presentLetters = computed(() => new Set(groups.value.map((g) => g.letter)))

function letterAnchor(letter: string) {
  return `customers-letter-${letter}`
}

function jumpToLetter(letter: string) {
  if (!presentLetters.value.has(letter)) return
  document.getElementById(letterAnchor(letter))?.scrollIntoView({ behavior: 'smooth', block: 'start' })
}

// --- Écran large : tableau trié (FR-018) ---------------------------------------------------------

interface CustomerTableRow {
  customer: CustomerDto
  stats: CustomerPurchaseStats
}

type CustomerSortKey = 'name' | 'saleCount' | 'total' | 'pending' | 'lastSale'

const columns: TableColumn<CustomerSortKey>[] = [
  { id: 'name', label: 'Client', sortKey: 'name' },
  { id: 'phone', label: 'Téléphone' },
  { id: 'saleCount', label: 'Ventes', sortKey: 'saleCount', firstDirection: 'desc', numeric: true },
  { id: 'total', label: 'Total acheté', sortKey: 'total', firstDirection: 'desc', numeric: true },
  { id: 'pending', label: 'À encaisser', sortKey: 'pending', firstDirection: 'desc', numeric: true },
  { id: 'lastSale', label: 'Dernier achat', sortKey: 'lastSale', firstDirection: 'desc' },
]

const sort = ref<TableSort<CustomerSortKey>>({ key: 'name', direction: 'asc' })

function byName(a: CustomerTableRow, b: CustomerTableRow): number {
  return (
    compareText(customerSortKey(a.customer), customerSortKey(b.customer)) ||
    compareText(a.customer.firstName ?? '', b.customer.firstName ?? '')
  )
}

function compareCustomers(key: CustomerSortKey, a: CustomerTableRow, b: CustomerTableRow): number {
  switch (key) {
    case 'name':
      return byName(a, b)
    case 'saleCount':
      return a.stats.saleCount - b.stats.saleCount
    case 'total':
      return a.stats.total - b.stats.total
    case 'pending':
      return a.stats.pendingTotal - b.stats.pendingTotal
    case 'lastSale':
      // Un client sans achat se range après le plus ancien.
      return (
        (a.stats.lastSaleDate ? new Date(a.stats.lastSaleDate).getTime() : 0) -
        (b.stats.lastSaleDate ? new Date(b.stats.lastSaleDate).getTime() : 0)
      )
  }
}

const stats = computed(() => customerPurchaseStats(sales.value))
const tableRows = computed(() =>
  sortRows(
    filtered.value.map((customer) => ({ customer, stats: stats.value.get(customer.id) ?? NO_PURCHASES })),
    sort.value,
    compareCustomers,
    byName,
  ),
)

function euros(value: number): string {
  return `${value.toLocaleString('fr-FR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} €`
}
</script>

<template>
  <v-container class="customers-view app-page-container">
    <AppBrandHeader />

    <header class="customers-view__header">
      <div class="customers-view__title-row">
        <h1 class="text-h4 font-weight-bold">Clients</h1>
        <span class="text-secondary font-weight-medium customers-view__total">{{ customers.length }} clients</span>
      </div>
      <div class="customers-view__search">
        <v-icon size="20">phosphor:magnifying-glass</v-icon>
        <input v-model="query" type="text" placeholder="Nom ou téléphone" class="customers-view__search-input" />
        <button v-if="query" type="button" class="customers-view__search-clear" aria-label="Effacer" @click="query = ''">
          <v-icon size="18">phosphor:x-circle</v-icon>
        </button>
      </div>
    </header>

    <p v-if="loading" class="text-secondary">Chargement...</p>
    <p v-else-if="error" class="text-error">{{ error }}</p>
    <p v-else-if="groups.length === 0" class="text-secondary customers-view__empty">Aucun client trouvé.</p>

    <AppSortableTable
      v-else-if="mdAndUp"
      v-model:sort="sort"
      :columns="columns"
      :rows="tableRows"
      :row-key="(r) => r.customer.id"
      @row-click="(r) => router.push(`/customers/${r.customer.id}`)"
    >
      <template #row="{ row: r }">
        <td class="app-table__cell--strong">{{ customerFullName(r.customer) }}</td>
        <td class="app-table__cell--muted">{{ r.customer.phone || '—' }}</td>
        <template v-if="salesLoading">
          <td v-for="n in 4" :key="n" class="app-table__cell--numeric text-secondary">…</td>
        </template>
        <template v-else>
          <td class="app-table__cell--numeric">{{ r.stats.saleCount || '—' }}</td>
          <td class="app-table__cell--numeric app-table__cell--amount">
            {{ r.stats.saleCount ? euros(r.stats.total) : '—' }}
          </td>
          <td class="app-table__cell--numeric">
            <AppBadge v-if="r.stats.pendingTotal > 0" tone="warning">{{ euros(r.stats.pendingTotal) }}</AppBadge>
            <span v-else class="text-secondary">—</span>
          </td>
          <td class="app-table__cell--muted">
            {{
              r.stats.lastSaleDate
                ? new Date(r.stats.lastSaleDate).toLocaleDateString('fr-FR', { day: 'numeric', month: 'long', year: 'numeric' })
                : '—'
            }}
          </td>
        </template>
      </template>
    </AppSortableTable>

    <div v-else class="customers-view__groups">
      <section
        v-for="group in groups"
        :id="letterAnchor(group.letter)"
        :key="group.letter"
        class="customers-view__group"
      >
        <div class="customers-view__letter">{{ group.letter }}</div>
        <div class="customers-view__list">
          <CustomerRow v-for="customer in group.customers" :key="customer.id" :customer="customer" />
        </div>
      </section>
    </div>

    <nav v-if="groups.length > 0 && !mdAndUp" class="customers-view__index" aria-label="Index alphabétique">
      <button
        v-for="letter in ALPHABET"
        :key="letter"
        type="button"
        class="customers-view__index-letter"
        :class="{ 'customers-view__index-letter--empty': !presentLetters.has(letter) }"
        :disabled="!presentLetters.has(letter)"
        :aria-label="`Aller à la lettre ${letter}`"
        @click="jumpToLetter(letter)"
      >
        {{ letter }}
      </button>
    </nav>

    <AppFab icon="plus" ariaLabel="Créer un client" :to="mdAndUp ? undefined : '/customers/add'" @click="addOpen = true" />

    <v-dialog v-model="addOpen">
      <CustomerAddView v-if="addOpen" dialog @saved="onCustomerSaved" @cancel="addOpen = false" />
    </v-dialog>
  </v-container>
</template>

<style scoped>
.customers-view {
  padding-bottom: 96px;
}

.customers-view__header {
  display: flex;
  flex-direction: column;
  gap: 14px;
  padding: 0 4px 12px;
}

.customers-view__title-row {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  gap: 12px;
}

.customers-view__total {
  font-size: 15px;
}

.customers-view__search {
  display: flex;
  align-items: center;
  gap: 8px;
  background: rgb(var(--v-theme-surface));
  border-radius: 12px;
  padding: 0 14px;
  height: 48px;
  color: rgb(var(--v-theme-secondary));
}

.customers-view__search-input {
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

.customers-view__search-clear {
  border: none;
  background: none;
  color: rgb(var(--v-theme-secondary));
  cursor: pointer;
  width: 32px;
  height: 44px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.customers-view__empty {
  text-align: center;
  padding: 40px 20px;
}

.customers-view__groups {
  display: flex;
  flex-direction: column;
  gap: 14px;
  /* Gouttière réservée au rail alphabétique, sur la seule liste : la poser sur
     le conteneur décalait toute la page vers la gauche (padding asymétrique). */
  padding-right: 22px;
}

.customers-view__group {
  /* Compense le bandeau de marque collant lors d'un saut de lettre. */
  scroll-margin-top: 84px;
}

.customers-view__index {
  position: fixed;
  right: 2px;
  top: 140px;
  bottom: 180px;
  z-index: 2;
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
}

.customers-view__index-letter {
  flex: 0 1 auto;
  border: none;
  background: none;
  padding: 0 5px;
  font-family: var(--font-body);
  font-size: clamp(9px, 1.5vh, 12px);
  font-weight: 600;
  line-height: 1.35;
  color: rgb(var(--v-theme-primary));
  cursor: pointer;
}

.customers-view__index-letter--empty {
  color: rgb(var(--v-theme-field-border));
  cursor: default;
}

.customers-view__letter {
  font-family: var(--font-heading);
  font-weight: 700;
  font-size: 19px;
  color: rgb(var(--v-theme-secondary));
  padding-left: 4px;
  margin-bottom: 8px;
}

.customers-view__list {
  display: flex;
  flex-direction: column;
  gap: 8px;
}
</style>
