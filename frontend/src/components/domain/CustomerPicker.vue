<!--
  Recherche et choix d'un client, partagé par la saisie d'une vente et sa correction.

  Extrait de SaleAddView : la correction d'une vente doit offrir exactement le même geste que la
  saisie (FR-003), et deux copies de cette recherche divergeraient au premier ajustement.

  Le composant charge lui-même la liste des clients : ses deux appelants n'en ont aucun autre
  usage, et la remonter ne ferait que déplacer la même requête.
-->
<script setup lang="ts">
import { computed, ref } from 'vue'
import { listCustomers } from '@/api/customers'
import { customerFullName } from '@/composables/useCustomers'
import { useAsyncData } from '@/composables/useAsyncData'
import type { CustomerDto } from '@/api/types'

const props = defineProps<{ modelValue: number | null }>()
const emit = defineEmits<{ 'update:modelValue': [value: number | null] }>()

const { data: customers } = useAsyncData(listCustomers, [])

const query = ref('')

const selected = computed<CustomerDto | null>(
  () => customers.value.find((c) => c.id === props.modelValue) ?? null,
)

// Deux caractères avant d'afficher quoi que ce soit, cinq résultats au plus : sur un téléphone,
// une liste plus longue pousse le reste de l'écran hors de vue.
const results = computed(() => {
  const q = query.value.trim().toLowerCase()
  if (q.length < 2) return []
  return customers.value.filter((c) => customerFullName(c).toLowerCase().includes(q)).slice(0, 5)
})

function pick(id: number) {
  emit('update:modelValue', id)
  query.value = ''
}
</script>

<template>
  <template v-if="!selected">
    <div class="customer-picker__search">
      <v-icon size="20">phosphor:magnifying-glass</v-icon>
      <input
        v-model="query"
        type="text"
        placeholder="Nom ou téléphone"
        aria-label="Rechercher un client"
        class="customer-picker__search-input"
      />
    </div>
    <div v-if="results.length > 0" class="customer-picker__results">
      <button
        v-for="c in results"
        :key="c.id"
        type="button"
        class="customer-picker__result"
        @click="pick(c.id)"
      >
        <span class="customer-picker__result-name">{{ customerFullName(c) }}</span>
        <span class="text-secondary">{{ c.phone }}</span>
      </button>
    </div>
  </template>

  <div v-else class="customer-picker__selected">
    <div class="customer-picker__selected-info">
      <div class="customer-picker__selected-name">{{ customerFullName(selected) }}</div>
      <div class="text-secondary">{{ selected.phone }}</div>
    </div>
    <button type="button" class="customer-picker__change" @click="emit('update:modelValue', null)">
      Changer
    </button>
  </div>
</template>

<style scoped>
.customer-picker__search {
  display: flex;
  align-items: center;
  gap: 8px;
  background: rgb(var(--v-theme-field-surface));
  border: 1.5px solid rgb(var(--v-theme-field-border));
  border-radius: 10px;
  padding: 0 14px;
  height: 52px;
  color: rgb(var(--v-theme-secondary));
  margin-bottom: 10px;
}

.customer-picker__search-input {
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

.customer-picker__results {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.customer-picker__result {
  display: flex;
  align-items: center;
  gap: 10px;
  border: none;
  background: rgb(var(--v-theme-status-neutral-container));
  border-radius: 10px;
  padding: 10px 14px;
  cursor: pointer;
  font-family: var(--font-body);
  text-align: left;
  min-height: 48px;
}

.customer-picker__result-name {
  flex: 1;
  min-width: 0;
  font-size: 16px;
  font-weight: 500;
  color: rgb(var(--v-theme-on-surface));
}

.customer-picker__selected {
  display: flex;
  align-items: center;
  gap: 12px;
  background: rgb(var(--v-theme-status-neutral-container));
  border-radius: 10px;
  padding: 12px 14px;
}

.customer-picker__selected-info {
  flex: 1;
  min-width: 0;
}

.customer-picker__selected-name {
  font-family: var(--font-heading);
  font-weight: 600;
  font-size: 19px;
}

.customer-picker__change {
  border: none;
  background: none;
  color: rgb(var(--v-theme-primary));
  font-size: 15px;
  font-weight: 600;
  cursor: pointer;
  font-family: var(--font-body);
  min-height: 44px;
}
</style>
