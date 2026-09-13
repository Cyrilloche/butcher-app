<script setup lang="ts" generic="T, K extends string">
import { nextSort, type TableColumn, type TableSort } from '@/composables/useTableSort'

/**
 * Tableau des écrans de consultation sur écran large (FR-018) : en-têtes triables, ligne cliquable
 * vers le détail. Les cellules viennent de l'écran, par le slot `row`, en `<td>` ; les classes
 * `app-table__cell--*` en règlent l'allure commune.
 */
defineProps<{
  columns: TableColumn<K>[]
  rows: T[]
  rowKey: (row: T) => string | number
  /** Ligne atténuée (produit désactivé, par exemple). */
  rowMuted?: (row: T) => boolean
}>()
const sort = defineModel<TableSort<K>>('sort', { required: true })
const emit = defineEmits<{ rowClick: [row: T] }>()

function ariaSort(column: TableColumn<K>): 'ascending' | 'descending' | 'none' | undefined {
  if (!column.sortKey) return undefined
  if (sort.value.key !== column.sortKey) return 'none'
  return sort.value.direction === 'asc' ? 'ascending' : 'descending'
}
</script>

<template>
  <div class="app-table-wrap">
    <table class="app-table">
      <thead>
        <tr>
          <th
            v-for="column in columns"
            :key="column.id"
            scope="col"
            :aria-sort="ariaSort(column)"
            :class="{ 'app-table__head--numeric': column.numeric }"
          >
            <button
              v-if="column.sortKey"
              type="button"
              class="app-table__sort"
              :class="{ 'app-table__sort--active': sort.key === column.sortKey }"
              @click="sort = nextSort(sort, column.sortKey, column.firstDirection)"
            >
              {{ column.label }}
              <v-icon v-if="sort.key === column.sortKey" size="14">
                phosphor:{{ sort.direction === 'asc' ? 'caret-up' : 'caret-down' }}
              </v-icon>
            </button>
            <template v-else>{{ column.label }}</template>
          </th>
        </tr>
      </thead>
      <tbody>
        <tr
          v-for="row in rows"
          :key="rowKey(row)"
          class="app-table__row"
          :class="{ 'app-table__row--muted': rowMuted?.(row) }"
          tabindex="0"
          @click="emit('rowClick', row)"
          @keydown.enter="emit('rowClick', row)"
        >
          <slot name="row" :row="row" />
        </tr>
      </tbody>
    </table>
  </div>
</template>

<style scoped>
.app-table-wrap {
  background: rgb(var(--v-theme-surface));
  border-radius: 14px;
  box-shadow: 0 1px 2px rgba(43, 36, 30, 0.06);
  overflow-x: auto;
}

.app-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 15px;
}

.app-table th {
  text-align: left;
  font-size: 13px;
  font-weight: 600;
  color: rgb(var(--v-theme-secondary));
  text-transform: uppercase;
  letter-spacing: 0.04em;
  padding: 14px 16px;
  border-bottom: 1px solid rgb(var(--v-theme-status-neutral-container));
  white-space: nowrap;
}

.app-table th.app-table__head--numeric {
  text-align: right;
}

.app-table :slotted(td) {
  padding: 12px 16px;
  border-bottom: 1px solid rgb(var(--v-theme-status-neutral-container));
}

.app-table tbody tr:last-child :slotted(td) {
  border-bottom: none;
}

.app-table__sort {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  border: none;
  background: none;
  padding: 0;
  font: inherit;
  color: inherit;
  text-transform: inherit;
  letter-spacing: inherit;
  cursor: pointer;
}

.app-table__sort--active {
  color: rgb(var(--v-theme-primary));
}

.app-table__row {
  cursor: pointer;
}

.app-table__row:hover,
.app-table__row:focus-visible {
  background: rgb(var(--v-theme-status-neutral-container));
  outline: none;
}

.app-table__row--muted {
  opacity: 0.65;
}

/* Allure commune des cellules fournies par l'écran. */
.app-table :slotted(.app-table__cell--numeric) {
  text-align: right;
  white-space: nowrap;
}

.app-table :slotted(.app-table__cell--strong) {
  font-weight: 600;
}

.app-table :slotted(.app-table__cell--muted) {
  color: rgb(var(--v-theme-secondary));
  white-space: nowrap;
}

.app-table :slotted(.app-table__cell--amount) {
  font-weight: 600;
  color: rgb(var(--v-theme-success));
}
</style>
