<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue'
import AppBadge from '@/components/base/AppBadge.vue'
import AppSortableTable from '@/components/base/AppSortableTable.vue'
import { listAccounts } from '@/api/accounts'
import { searchAuditEntries } from '@/api/auditEntries'
import { useAsyncData } from '@/composables/useAsyncData'
import {
  auditActionLabels,
  auditActionTones,
  auditEntityTypeLabels,
  describeDeletedContent,
  entryAuthor,
  entryLink,
  formatOccurredAt,
} from '@/composables/useJournal'
import type { TableColumn, TableSort } from '@/composables/useTableSort'
import type { AccountDto, AuditEntityType, AuditEntryDto, AuditEntryPageDto } from '@/api/types'

/**
 * Journal « qui a fait quoi », réservé à l'administrateur (FR-023). Conçu pour le PC ; le serveur trie
 * du plus récent au plus ancien et pagine. Une ligne s'ouvre dans une fenêtre : contenu d'une
 * suppression (FR-022), ou lien vers l'objet quand il existe encore.
 */
const PAGE_SIZE = 50

const filters = reactive({ accountId: '', entityType: '' as AuditEntityType | '', from: '', to: '' })
const page = ref(1)

const { data: accounts } = useAsyncData(listAccounts, [] as AccountDto[])
const {
  data: result,
  loading,
  error,
  reload,
} = useAsyncData(
  () =>
    searchAuditEntries({
      accountId: filters.accountId || undefined,
      entityType: filters.entityType || undefined,
      from: filters.from || undefined,
      to: filters.to || undefined,
      page: page.value,
      pageSize: PAGE_SIZE,
    }),
  { items: [], total: 0, page: 1, pageSize: PAGE_SIZE } as AuditEntryPageDto,
)

watch(filters, () => {
  page.value = 1
  reload()
})
watch(page, reload)

const filtering = computed(() => Object.values(filters).some((value) => value !== ''))
const pageCount = computed(() => Math.max(1, Math.ceil(result.value.total / PAGE_SIZE)))

function resetFilters() {
  Object.assign(filters, { accountId: '', entityType: '', from: '', to: '' })
}

const entityTypes = Object.entries(auditEntityTypeLabels) as [AuditEntityType, string][]

// Le serveur impose l'ordre chronologique inverse : aucune colonne n'est triable.
const columns: TableColumn<never>[] = [
  { id: 'occurredAt', label: 'Date' },
  { id: 'author', label: 'Auteur' },
  { id: 'action', label: 'Opération' },
  { id: 'object', label: 'Objet' },
]
const sort = ref({ key: 'occurredAt', direction: 'desc' } as unknown as TableSort<never>)

const opened = ref<AuditEntryDto | null>(null)
const detailOpen = computed({
  get: () => opened.value !== null,
  set: (open: boolean) => {
    if (!open) opened.value = null
  },
})
const openedContent = computed(() => (opened.value ? describeDeletedContent(opened.value) : null))
const openedLink = computed(() => (opened.value ? entryLink(opened.value) : null))

function objectLabel(entry: AuditEntryDto): string {
  const type = entry.entityType ? auditEntityTypeLabels[entry.entityType] : null
  return [type, entry.entityLabel].filter(Boolean).join(' · ')
}
</script>

<template>
  <v-container class="journal-view app-page-container">
    <header class="journal-view__header">
      <h1 class="journal-view__title">Journal</h1>
      <p class="journal-view__subtitle text-secondary">Qui a créé, modifié ou supprimé quoi, et quand.</p>
    </header>

    <div class="journal-view__filters">
      <label class="journal-view__filter journal-view__filter--wide">
        <span class="journal-view__filter-label">Auteur</span>
        <select v-model="filters.accountId" class="journal-view__field">
          <option value="">Tous les comptes</option>
          <option v-for="account in accounts" :key="account.id" :value="account.id">{{ account.displayName }}</option>
        </select>
      </label>
      <label class="journal-view__filter journal-view__filter--wide">
        <span class="journal-view__filter-label">Objet</span>
        <select v-model="filters.entityType" class="journal-view__field">
          <option value="">Tous les objets</option>
          <option v-for="[value, label] in entityTypes" :key="value" :value="value">{{ label }}</option>
        </select>
      </label>
      <label class="journal-view__filter">
        <span class="journal-view__filter-label">Du</span>
        <input v-model="filters.from" type="date" class="journal-view__field" :max="filters.to || undefined" />
      </label>
      <label class="journal-view__filter">
        <span class="journal-view__filter-label">Au</span>
        <input v-model="filters.to" type="date" class="journal-view__field" :min="filters.from || undefined" />
      </label>
      <v-btn v-if="filtering" variant="text" color="secondary" class="journal-view__reset" @click="resetFilters">
        Effacer les filtres
      </v-btn>
    </div>

    <div class="journal-view__summary">
      {{ result.total }} opération{{ result.total > 1 ? 's' : '' }}
    </div>

    <p v-if="loading && result.items.length === 0" class="text-secondary">Chargement...</p>
    <p v-else-if="error" class="text-error">{{ error }}</p>
    <p v-else-if="result.items.length === 0" class="text-secondary journal-view__empty">
      {{ filtering ? 'Aucune opération ne correspond à ces filtres.' : 'Aucune opération enregistrée.' }}
    </p>

    <template v-else>
      <AppSortableTable
        v-model:sort="sort"
        :columns="columns"
        :rows="result.items"
        :row-key="(e) => e.id"
        @row-click="(e) => (opened = e)"
      >
        <template #row="{ row: e }">
          <td class="app-table__cell--muted">{{ formatOccurredAt(e.occurredAt) }}</td>
          <td class="app-table__cell--strong">{{ entryAuthor(e) }}</td>
          <td>
            <AppBadge :tone="auditActionTones[e.action]">{{ auditActionLabels[e.action] }}</AppBadge>
          </td>
          <td>{{ objectLabel(e) }}</td>
        </template>
      </AppSortableTable>

      <nav v-if="pageCount > 1" class="journal-view__pages" aria-label="Pages du journal">
        <v-btn variant="text" color="secondary" :disabled="page <= 1" @click="page--">
          <v-icon start size="16">phosphor:caret-left</v-icon>
          Plus récentes
        </v-btn>
        <span class="text-secondary">Page {{ page }} sur {{ pageCount }}</span>
        <v-btn variant="text" color="secondary" :disabled="page >= pageCount" @click="page++">
          Plus anciennes
          <v-icon end size="16">phosphor:caret-right</v-icon>
        </v-btn>
      </nav>
    </template>

    <v-dialog v-model="detailOpen">
      <v-card v-if="opened" class="journal-view__dialog">
        <h2 class="text-h6 font-weight-bold mb-1">{{ auditActionLabels[opened.action] }}</h2>
        <p class="text-secondary mb-4">{{ entryAuthor(opened) }} · {{ formatOccurredAt(opened.occurredAt) }}</p>

        <p class="journal-view__object">{{ objectLabel(opened) }}</p>

        <template v-if="openedContent">
          <div class="journal-view__content-title">Contenu au moment de la suppression</div>
          <dl class="journal-view__fields">
            <template v-for="field in openedContent.fields" :key="field.label">
              <dt class="text-secondary">{{ field.label }}</dt>
              <dd>{{ field.value }}</dd>
            </template>
          </dl>
          <template v-if="openedContent.items.length > 0">
            <div class="journal-view__content-title">{{ openedContent.itemsTitle }}</div>
            <ul class="journal-view__items">
              <li v-for="(item, index) in openedContent.items" :key="index">{{ item }}</li>
            </ul>
          </template>
        </template>

        <div class="journal-view__dialog-actions">
          <v-btn variant="text" color="secondary" @click="opened = null">Fermer</v-btn>
          <v-btn v-if="openedLink" color="primary" variant="flat" :to="openedLink">Ouvrir</v-btn>
        </div>
      </v-card>
    </v-dialog>
  </v-container>
</template>

<style scoped>
.journal-view {
  padding-bottom: 40px;
}

.journal-view__header {
  padding: 8px 4px 16px;
}

.journal-view__title {
  font-family: var(--font-heading);
  font-size: 32px;
  font-weight: 700;
  line-height: 1.1;
  margin: 0;
}

.journal-view__subtitle {
  font-size: 16px;
  margin: 4px 0 0;
}

.journal-view__filters {
  display: flex;
  flex-wrap: wrap;
  align-items: flex-end;
  gap: 12px;
  background: rgb(var(--v-theme-surface));
  border-radius: 14px;
  padding: 14px 16px;
  margin-bottom: 14px;
}

.journal-view__filter {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.journal-view__filter--wide {
  flex: 1;
  min-width: 180px;
}

.journal-view__filter-label {
  font-size: 13px;
  font-weight: 600;
  color: rgb(var(--v-theme-secondary));
}

.journal-view__field {
  height: 52px;
  border: 1.5px solid rgb(var(--v-theme-field-border));
  border-radius: 10px;
  background: rgb(var(--v-theme-field-surface));
  padding: 0 12px;
  font-family: var(--font-body);
  font-size: 16px;
  color: rgb(var(--v-theme-on-surface));
}

.journal-view__reset {
  align-self: center;
}

.journal-view__summary {
  padding: 0 4px 10px;
  font-size: 15px;
  font-weight: 600;
  color: rgb(var(--v-theme-secondary));
}

.journal-view__empty {
  text-align: center;
  padding: 40px 20px;
}

.journal-view__pages {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 12px;
  padding-top: 14px;
}

.journal-view__dialog {
  padding: 20px;
  border-radius: 16px;
}

.journal-view__object {
  font-weight: 600;
  margin: 0 0 12px;
  overflow-wrap: anywhere;
}

.journal-view__content-title {
  font-size: 13px;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.04em;
  color: rgb(var(--v-theme-secondary));
  margin: 12px 0 6px;
}

.journal-view__fields {
  display: grid;
  grid-template-columns: auto 1fr;
  gap: 4px 14px;
  margin: 0;
}

.journal-view__fields dd {
  margin: 0;
  overflow-wrap: anywhere;
}

.journal-view__items {
  margin: 0;
  padding-left: 18px;
}

.journal-view__dialog-actions {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
  margin-top: 20px;
}
</style>
