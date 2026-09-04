<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import AppPageHeader from '@/components/base/AppPageHeader.vue'
import AppCard from '@/components/base/AppCard.vue'
import AppTextField from '@/components/base/AppTextField.vue'
import AppButton from '@/components/base/AppButton.vue'
import AppHintBox from '@/components/base/AppHintBox.vue'
import AppChip from '@/components/base/AppChip.vue'
import SaleModeToggle from '@/components/domain/SaleModeToggle.vue'
import { createProduct } from '@/api/products'
import { listUnitsOfMeasure } from '@/api/unitsOfMeasure'
import { useAsyncData } from '@/composables/useAsyncData'
import { ApiError } from '@/api/http'
import type { SaleMode } from '@/api/types'

const router = useRouter()

// Écart maquette : "Ajout Produit.dc.html" ne propose aucun choix d'unité de vente,
// alors que le backend l'exige (CreateProductRequest.SaleUnitId, requis). Ajout
// nécessaire, absent de la maquette — cf. docs/data-model.md / plan de session.
const { data: units, loading: loadingUnits, error: unitsError } = useAsyncData(
  () => listUnitsOfMeasure(false),
  [],
)

const state = reactive({
  name: '',
  code: '',
  saleMode: 'by_weight' as SaleMode,
  saleUnitId: null as number | null,
})

const batchDateCode = new Date().toISOString().slice(2, 10).replace(/-/g, '')
const batchPreview = computed(() => `${state.code.trim() ? state.code.toUpperCase() : '——'}-${batchDateCode}-1`)

const modeHint = computed(() =>
  state.saleMode === 'by_weight'
    ? 'Chaque unité sera pesée à l’ajout au stock, prix en € / kg.'
    : 'Les unités se comptent simplement, prix en € / pièce.',
)

const canSave = computed(
  () => state.name.trim().length > 0 && state.code.trim().length >= 2 && state.saleUnitId !== null,
)

const saving = ref(false)
const saveError = ref<string | null>(null)

async function save() {
  if (!canSave.value || state.saleUnitId === null) return
  saving.value = true
  saveError.value = null
  try {
    await createProduct({
      code: state.code.trim().toUpperCase(),
      name: state.name.trim(),
      saleMode: state.saleMode,
      saleUnitId: state.saleUnitId,
    })
    await router.push('/products')
  } catch (err) {
    saveError.value = err instanceof ApiError ? err.message : "Erreur lors de l'enregistrement, réessaie."
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <v-container class="product-add-view">
    <AppPageHeader to="/products" back-label="Produits" title="Nouveau produit" />

    <div class="product-add-view__sections">
      <AppCard>
        <div class="product-add-view__section-title text-secondary">1. Le produit</div>
        <AppTextField v-model="state.name" label="Nom du produit" placeholder="Ex. Saucisson à l'ail" class="mb-3" />

        <div class="product-add-view__code-row">
          <AppTextField v-model="state.code" maxlength="3" label="Code (numéros de lot)" placeholder="SA" style="max-width: 110px; text-transform: uppercase" />
          <span class="text-secondary product-add-view__code-hint">
            Ex. de numéro : <strong>{{ batchPreview }}</strong>
          </span>
        </div>

        <p v-if="loadingUnits" class="text-secondary mt-3 mb-0">Chargement des unités...</p>
        <p v-else-if="unitsError" class="text-error mt-3 mb-0">{{ unitsError }}</p>
        <div v-else class="product-add-view__units mt-3">
          <label class="product-add-view__units-label">Unité de vente</label>
          <div class="product-add-view__chips">
            <AppChip
              v-for="unit in units"
              :key="unit.id"
              :selected="unit.id === state.saleUnitId"
              @click="state.saleUnitId = unit.id"
            >
              {{ unit.label }}
            </AppChip>
          </div>
        </div>
      </AppCard>

      <AppCard>
        <div class="product-add-view__section-title text-secondary">2. Comment se vend-il ?</div>
        <SaleModeToggle v-model="state.saleMode" class="mb-3" />
        <AppHintBox icon="info">{{ modeHint }}</AppHintBox>
      </AppCard>
    </div>

    <div class="product-add-view__footer">
      <p v-if="saveError" class="product-add-view__save-error text-error">{{ saveError }}</p>
      <AppButton block height="60" :color="canSave ? 'primary' : undefined" :disabled="!canSave || saving" @click="save">
        {{ saving ? 'Création...' : 'Créer le produit' }}
      </AppButton>
    </div>
  </v-container>
</template>

<style scoped>
.product-add-view {
  padding-bottom: 110px;
}

.product-add-view__sections {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.product-add-view__section-title {
  font-size: 16px;
  font-weight: 600;
  margin-bottom: 12px;
}

.product-add-view__code-row {
  display: flex;
  align-items: center;
  gap: 10px;
}

.product-add-view__code-hint {
  font-size: 15px;
}

.product-add-view__units-label {
  display: block;
  font-size: 15px;
  font-weight: 500;
  margin-bottom: 6px;
}

.product-add-view__chips {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
}

.product-add-view__save-error {
  font-size: 14px;
  font-weight: 500;
  text-align: center;
  margin: 0 0 10px;
}

.product-add-view__footer {
  position: fixed;
  left: 0;
  right: 0;
  bottom: 56px;
  padding: 14px 16px;
  background: linear-gradient(to top, rgb(var(--v-theme-background)) 70%, transparent);
}
</style>
