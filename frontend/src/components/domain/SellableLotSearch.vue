<!--
  Recherche d'une unité à vendre, par produit puis par étiquette (RU-02).

  La recherche listait des unités à plat, limitées à 8 : le premier produit qui avait 8 sachets
  en stock occupait toute la liste, et les autres produits du même nom n'apparaissaient jamais.
  Elle propose désormais les produits qui correspondent, avec leur stock, puis toutes les unités
  du produit choisi. Un seul produit correspond : ses unités s'affichent directement.

  Un numéro d'étiquette tapé (`SC-2609`) ne correspond à aucun nom de produit : les unités dont
  le numéro le contient s'affichent alors directement.

  La saisie reste en place après un choix : vendre trois sachets du même produit ne demande pas
  de retaper son nom. Le composant doit rester monté pour la garder.
-->
<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import type { SellableLot } from '@/composables/useSales'

interface ProductGroup {
  productId: number
  productName: string
  productCode: string
  lots: SellableLot[]
  openedCount: number
}

const props = defineProps<{
  lots: SellableLot[]
  /** Unités déjà au panier, à ne plus proposer. */
  excludedIds: Set<number>
  loading: boolean
}>()
const emit = defineEmits<{ pick: [lot: SellableLot] }>()

const query = ref('')
/** Produit choisi parmi plusieurs résultats ; oublié dès que la saisie change. */
const chosenProductId = ref<number | null>(null)
watch(query, () => {
  chosenProductId.value = null
})

const normalizedQuery = computed(() => query.value.trim().toLowerCase())
const searching = computed(() => normalizedQuery.value.length >= 2)

// Les unités arrivent triées par produit puis par étiquette : les groupes gardent cet ordre.
const groups = computed<ProductGroup[]>(() => {
  const byProduct = new Map<number, ProductGroup>()
  for (const lot of props.lots) {
    if (props.excludedIds.has(lot.stockUnitId)) continue
    let group = byProduct.get(lot.productId)
    if (!group) {
      group = { productId: lot.productId, productName: lot.productName, productCode: lot.productCode, lots: [], openedCount: 0 }
      byProduct.set(lot.productId, group)
    }
    group.lots.push(lot)
    if (lot.status === 'opened') group.openedCount++
  }
  // Une unité entamée passe en tête : c'est elle qu'on finit avant d'en ouvrir une autre.
  for (const group of byProduct.values()) {
    group.lots.sort((a, b) => Number(b.status === 'opened') - Number(a.status === 'opened'))
  }
  return [...byProduct.values()]
})

const matchingProducts = computed(() => {
  if (!searching.value) return []
  const q = normalizedQuery.value
  return groups.value.filter((g) => g.productName.toLowerCase().includes(q) || g.productCode.toLowerCase() === q)
})

const shownProduct = computed<ProductGroup | null>(() => {
  if (chosenProductId.value != null) {
    return matchingProducts.value.find((g) => g.productId === chosenProductId.value) ?? null
  }
  return matchingProducts.value.length === 1 ? matchingProducts.value[0]! : null
})

const matchingUnits = computed(() => {
  if (!searching.value || matchingProducts.value.length > 0) return []
  return groups.value.flatMap((g) => g.lots).filter((l) => l.label.toLowerCase().includes(normalizedQuery.value))
})

const shownUnits = computed(() => shownProduct.value?.lots ?? matchingUnits.value)

function formatPrice(value: number) {
  return value.toLocaleString('fr-FR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}
</script>

<template>
  <div class="sellable-lot-search">
    <div class="sellable-lot-search__field">
      <v-icon size="20">phosphor:magnifying-glass</v-icon>
      <input
        v-model="query"
        type="text"
        placeholder="Produit ou n° d'étiquette"
        aria-label="Rechercher un produit"
        class="sellable-lot-search__input"
      />
    </div>

    <p v-if="loading" class="text-secondary mb-0">Chargement du stock...</p>

    <template v-else-if="shownProduct">
      <div class="sellable-lot-search__product-title">
        <span class="font-weight-medium">{{ shownProduct.productName }}</span>
        <button
          v-if="matchingProducts.length > 1"
          type="button"
          class="sellable-lot-search__back"
          @click="chosenProductId = null"
        >
          <v-icon size="16">phosphor:caret-left</v-icon>
          Autres produits
        </button>
      </div>
    </template>

    <div v-else-if="matchingProducts.length > 1" class="sellable-lot-search__results">
      <button
        v-for="group in matchingProducts"
        :key="group.productId"
        type="button"
        class="sellable-lot-search__product"
        @click="chosenProductId = group.productId"
      >
        <span class="sellable-lot-search__result-info">
          <span class="sellable-lot-search__result-name">{{ group.productName }}</span>
          <span class="sellable-lot-search__result-detail text-secondary">
            {{ group.lots.length }} en stock<template v-if="group.openedCount > 0">, dont {{ group.openedCount }} entamé{{ group.openedCount > 1 ? 's' : '' }}</template>
          </span>
        </span>
        <v-icon size="18" class="text-secondary">phosphor:caret-right</v-icon>
      </button>
    </div>

    <div v-if="!loading && shownUnits.length > 0" class="sellable-lot-search__results">
      <button
        v-for="lot in shownUnits"
        :key="lot.stockUnitId"
        type="button"
        class="sellable-lot-search__unit"
        @click="emit('pick', lot)"
      >
        <span class="sellable-lot-search__result-info">
          <span class="sellable-lot-search__result-name sellable-lot-search__result-name--label">
            {{ lot.label }}
            <span v-if="lot.status === 'opened'" class="sellable-lot-search__opened">Entamé</span>
          </span>
          <span class="sellable-lot-search__result-detail text-secondary">
            <template v-if="!shownProduct">{{ lot.productName }} · </template>{{ lot.detail }}
          </span>
        </span>
        <span v-if="lot.status === 'available'" class="sellable-lot-search__price">{{ formatPrice(lot.price) }} €</span>
      </button>
    </div>

    <p
      v-if="!loading && searching && matchingProducts.length === 0 && matchingUnits.length === 0"
      class="text-secondary sellable-lot-search__empty"
    >
      Aucun produit en stock ne correspond.
    </p>
  </div>
</template>

<style scoped>
.sellable-lot-search__field {
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

.sellable-lot-search__input {
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

.sellable-lot-search__results {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.sellable-lot-search__product,
.sellable-lot-search__unit {
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

.sellable-lot-search__product-title {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
  font-size: 16px;
  margin-bottom: 6px;
}

.sellable-lot-search__back {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  border: none;
  background: none;
  color: rgb(var(--v-theme-primary));
  font-family: var(--font-body);
  font-size: 15px;
  font-weight: 600;
  cursor: pointer;
  min-height: 44px;
}

.sellable-lot-search__result-info {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.sellable-lot-search__result-name {
  font-size: 16px;
  font-weight: 500;
  color: rgb(var(--v-theme-on-surface));
}

/* Le numéro d'étiquette ne se coupe jamais : il se recopie à la main. */
.sellable-lot-search__result-name--label {
  white-space: nowrap;
}

.sellable-lot-search__result-detail {
  font-size: 14px;
}

.sellable-lot-search__price {
  flex-shrink: 0;
  white-space: nowrap;
  font-weight: 600;
  color: rgb(var(--v-theme-success));
}

.sellable-lot-search__opened {
  background: rgb(var(--v-theme-warning-container));
  color: rgb(var(--v-theme-warning));
  font-size: 12px;
  font-weight: 600;
  padding: 2px 8px;
  border-radius: 999px;
  margin-left: 6px;
}

.sellable-lot-search__empty {
  text-align: center;
  padding: 6px 0;
  margin: 0;
}
</style>
