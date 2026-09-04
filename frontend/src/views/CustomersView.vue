<script setup lang="ts">
import { computed, ref } from 'vue'
import AppFab from '@/components/base/AppFab.vue'
import AppBrandHeader from '@/components/base/AppBrandHeader.vue'
import CustomerRow from '@/components/domain/CustomerRow.vue'
import { listCustomers } from '@/api/customers'
import { useAsyncData } from '@/composables/useAsyncData'
import { customerFullName, groupCustomersByLetter } from '@/composables/useCustomers'

const { data: customers, loading, error } = useAsyncData(listCustomers, [])

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
</script>

<template>
  <v-container class="customers-view">
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

    <nav v-if="groups.length > 0" class="customers-view__index" aria-label="Index alphabétique">
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

    <AppFab icon="plus" ariaLabel="Créer un client" to="/customers/add" />
  </v-container>
</template>

<style scoped>
.customers-view {
  padding-bottom: 96px;
  /* Gouttière réservée au rail alphabétique, pour qu'il ne recouvre pas
     la fin des noms de clients. */
  padding-right: 30px;
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
