<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue'
import AppPageHeader from '@/components/base/AppPageHeader.vue'
import AppCard from '@/components/base/AppCard.vue'
import AppTextField from '@/components/base/AppTextField.vue'
import AppButton from '@/components/base/AppButton.vue'
import ProductStatusBadge from '@/components/domain/ProductStatusBadge.vue'
import { listProducts, updateProduct, deactivateProduct, reactivateProduct } from '@/api/products'
import { getStockDetail } from '@/composables/useStock'
import { useAsyncData } from '@/composables/useAsyncData'
import { ApiError } from '@/api/http'
import type { ProductDto } from '@/api/types'

const props = defineProps<{ code: string }>()

async function loadProduct(): Promise<ProductDto | null> {
  const all = await listProducts(true)
  return all.find((p) => p.code.toUpperCase() === props.code.toUpperCase()) ?? null
}

const { data: product, loading, error, reload } = useAsyncData(loadProduct, null)
const { data: stockSummary } = useAsyncData(async () => (await getStockDetail(props.code))?.summary ?? null, null)

watch(() => props.code, reload)

const state = reactive({ name: '', allowPartialSale: false })

watch(
  product,
  (p) => {
    if (!p) return
    state.name = p.name
    state.allowPartialSale = p.allowPartialSale
  },
  { immediate: true },
)

const dirty = computed(
  () =>
    !!product.value &&
    (state.name !== product.value.name || state.allowPartialSale !== product.value.allowPartialSale),
)

const canSave = computed(() => state.name.trim().length > 0)
const saving = ref(false)
const saveError = ref<string | null>(null)

async function save() {
  if (!product.value || !canSave.value) return
  saving.value = true
  saveError.value = null
  try {
    await updateProduct(product.value.id, {
      name: state.name.trim(),
      allowPartialSale: product.value.saleMode === 'by_weight' && state.allowPartialSale,
    })
    await reload()
  } catch (err) {
    saveError.value = err instanceof ApiError ? err.message : "Erreur lors de l'enregistrement, réessaie."
  } finally {
    saving.value = false
  }
}

const togglingStatus = ref(false)
async function toggleStatus() {
  if (!product.value) return
  togglingStatus.value = true
  try {
    if (product.value.isActive) await deactivateProduct(product.value.id)
    else await reactivateProduct(product.value.id)
    await reload()
  } finally {
    togglingStatus.value = false
  }
}
</script>

<template>
  <v-container v-if="loading" class="product-detail-view">
    <p class="text-secondary">Chargement...</p>
  </v-container>

  <v-container v-else-if="error" class="product-detail-view">
    <p class="text-error">{{ error }}</p>
  </v-container>

  <v-container v-else-if="product" class="product-detail-view">
    <AppPageHeader to="/products" back-label="Produits" :title="product.name">
      <template #badge>
        <ProductStatusBadge v-if="!product.isActive" :is-active="false" />
      </template>
    </AppPageHeader>

    <div class="product-detail-view__sections">
      <AppCard :style="{ opacity: product.isActive ? 1 : 0.6 }">
        <div class="product-detail-view__section-title text-secondary">Le produit</div>
        <AppTextField v-model="state.name" label="Nom du produit" :disabled="!product.isActive" class="mb-3" />
        <AppTextField :model-value="product.code" label="Code (numéros de lot)" disabled style="max-width: 110px" />
      </AppCard>

      <AppCard>
        <div class="product-detail-view__section-title text-secondary">Mode de vente</div>
        <!-- Écart maquette assumé : sale_mode est immuable côté backend (data-model.md §3.3,
             absent d'UpdateProductRequest) — affiché en lecture seule, pas de sélecteur. -->
        <div class="product-detail-view__mode">
          <v-icon size="22">phosphor:{{ product.saleMode === 'by_weight' ? 'scales' : 'hand-coins' }}</v-icon>
          {{ product.saleMode === 'by_weight' ? 'Au poids' : 'À la pièce' }}
        </div>

        <v-checkbox
          v-if="product.saleMode === 'by_weight'"
          v-model="state.allowPartialSale"
          label="Peut être vendu à la tranche (ex. jambon entier)"
          color="primary"
          density="comfortable"
          :disabled="!product.isActive"
          hide-details
          class="mt-2"
        />
      </AppCard>

      <AppCard>
        <div class="product-detail-view__stock-header">
          <div class="product-detail-view__section-title text-secondary mb-0">En stock actuellement</div>
          <RouterLink :to="`/stock/${product.code}`" class="text-decoration-none font-weight-medium">Voir le stock</RouterLink>
        </div>
        <div class="product-detail-view__stock-line">{{ stockSummary ?? '—' }}</div>
      </AppCard>

      <AppButton
        block
        height="56"
        :color="product.isActive ? 'error' : 'success'"
        variant="outlined"
        :disabled="togglingStatus"
        @click="toggleStatus"
      >
        <v-icon start size="18">phosphor:{{ product.isActive ? 'trash' : 'plus' }}</v-icon>
        {{ product.isActive ? 'Désactiver ce produit' : 'Réactiver ce produit' }}
      </AppButton>
      <p class="product-detail-view__hint text-secondary">
        Un produit désactivé n'apparaît plus dans l'ajout au stock ni dans les ventes. Son historique est conservé.
      </p>
    </div>

    <div v-if="dirty" class="product-detail-view__footer">
      <p v-if="saveError" class="product-detail-view__save-error text-error">{{ saveError }}</p>
      <AppButton block height="60" color="primary" :disabled="!canSave || saving" @click="save">
        {{ saving ? 'Enregistrement...' : 'Enregistrer les modifications' }}
      </AppButton>
    </div>
  </v-container>

  <v-container v-else>
    <p class="text-secondary">Produit introuvable.</p>
  </v-container>
</template>

<style scoped>
.product-detail-view {
  padding-bottom: 130px;
}

.product-detail-view__sections {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.product-detail-view__section-title {
  font-size: 16px;
  font-weight: 600;
  margin-bottom: 12px;
}

.product-detail-view__mode {
  display: flex;
  align-items: center;
  gap: 10px;
  font-size: 17px;
  font-weight: 500;
}

.product-detail-view__stock-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
}

.product-detail-view__stock-line {
  font-family: var(--font-heading);
  font-weight: 700;
  font-size: 28px;
  line-height: 1;
  margin-top: 6px;
}

.product-detail-view__hint {
  font-size: 14px;
  text-align: center;
  padding: 0 12px;
}

.product-detail-view__save-error {
  font-size: 14px;
  font-weight: 500;
  text-align: center;
  margin: 0 0 10px;
}

.product-detail-view__footer {
  position: fixed;
  left: 0;
  right: 0;
  bottom: 0;
  padding: 14px 16px 34px;
  background: linear-gradient(to top, rgb(var(--v-theme-background)) 70%, transparent);
}
</style>
