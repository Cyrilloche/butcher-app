<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import AppPageHeader from '@/components/base/AppPageHeader.vue'
import AppCard from '@/components/base/AppCard.vue'
import AppStepper from '@/components/base/AppStepper.vue'
import AppTextField from '@/components/base/AppTextField.vue'
import AppButton from '@/components/base/AppButton.vue'
import { listActiveProducts, formatWeight } from '@/composables/useStock'
import { useAsyncData } from '@/composables/useAsyncData'
import { createProductionBatch } from '@/api/productionBatches'
import { addStockUnits } from '@/api/stockUnits'
import { ApiError } from '@/api/http'

const router = useRouter()

const { data: productCatalog, loading: loadingCatalog, error: catalogError } = useAsyncData(
  listActiveProducts,
  [],
)

const state = reactive({
  productId: null as number | null,
  date: new Date().toISOString().slice(0, 10),
  price: '',
  weights: [] as number[],
  newWeight: '',
  qty: 1,
})

const product = computed(() => productCatalog.value.find((p) => p.id === state.productId) ?? null)
const isWeight = computed(() => product.value?.saleMode === 'by_weight')
const isPiece = computed(() => product.value?.saleMode === 'by_piece')

function pickProduct(id: number) {
  state.productId = id
  state.weights = []
  state.newWeight = ''
  state.qty = 1
}

// Aperçu local du numéro de lot (RG-07) — le numéro définitif est généré côté API à l'enregistrement.
const batchDateCode = computed(() => (state.date ? state.date.slice(2).replace(/-/g, '') : '——'))
function numberFor(index: number) {
  return product.value ? `${product.value.code}-${batchDateCode.value}-${index}` : '—'
}
const nextBatchPreview = computed(() => numberFor(state.weights.length + 1))

function addWeight() {
  const grams = Math.round(Number(state.newWeight))
  if (grams > 0) {
    state.weights.push(grams)
    state.newWeight = ''
  }
}

function removeWeight(index: number) {
  state.weights.splice(index, 1)
}

const weightRows = computed(() =>
  state.weights.map((grams, i) => ({ label: numberFor(i + 1), weightLabel: formatWeight(grams) })),
)

const weightSummary = computed(() => {
  if (state.weights.length === 0) return null
  const totalKg = state.weights.reduce((a, b) => a + b, 0) / 1000
  return `${state.weights.length} sachet${state.weights.length > 1 ? 's' : ''} · ${totalKg.toLocaleString('fr-FR', { maximumFractionDigits: 2 })} kg au total`
})

const unitCount = computed(() => (isWeight.value ? state.weights.length : state.qty))
const canSave = computed(() => !!product.value && Number(state.price) > 0 && unitCount.value > 0)
const saving = ref(false)
const saveError = ref<string | null>(null)
const saveLabel = computed(() => {
  if (saving.value) return 'Enregistrement...'
  return canSave.value
    ? `Enregistrer — ${unitCount.value} unité${unitCount.value > 1 ? 's' : ''} en stock`
    : 'Enregistrer'
})

async function save() {
  if (!product.value || !canSave.value) return
  saving.value = true
  saveError.value = null
  try {
    // Deux appels distincts côté API (création du lot, puis des unités physiques) —
    // le frontend les enchaîne pour donner l'impression d'un flux unique (PRD RF-10).
    const batch = await createProductionBatch({
      productId: product.value.id,
      productionDate: state.date,
      salePrice: Number(state.price),
    })
    await addStockUnits(
      batch.id,
      isWeight.value
        ? { weights: state.weights.map((grams) => grams / 1000) }
        : { quantity: state.qty },
    )
    await router.push('/')
  } catch (err) {
    saveError.value =
      err instanceof ApiError ? err.message : "Erreur lors de l'enregistrement, réessaie."
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <v-container class="stock-add-view">
    <AppPageHeader to="/" back-label="Stock" title="Ajouter au stock" />

    <div class="stock-add-view__sections">
      <AppCard>
        <div class="stock-add-view__section-title text-secondary">1. Quel produit ?</div>
        <p v-if="loadingCatalog" class="text-secondary">Chargement des produits...</p>
        <p v-else-if="catalogError" class="text-error">{{ catalogError }}</p>
        <div v-else class="stock-add-view__chips">
          <button
            v-for="p in productCatalog"
            :key="p.id"
            type="button"
            class="stock-add-view__chip"
            :class="{ 'stock-add-view__chip--selected': p.id === state.productId }"
            @click="pickProduct(p.id)"
          >
            {{ p.name }}
          </button>
        </div>
      </AppCard>

      <template v-if="product">
        <AppCard>
          <div class="stock-add-view__section-title text-secondary">2. Le lot de production</div>

          <AppTextField v-model="state.date" type="date" label="Date de fabrication" class="mb-3" />

          <AppTextField
            v-model="state.price"
            type="number"
            inputmode="decimal"
            min="0"
            step="0.5"
            label="Prix de vente"
            :suffix="`€ / ${product.priceUnit}`"
            style="max-width: 220px"
          />

          <div class="stock-add-view__batch-preview text-secondary">
            <v-icon size="20">phosphor:tag</v-icon>
            Prochain numéro : <strong>{{ nextBatchPreview }}</strong>
          </div>
        </AppCard>

        <AppCard>
          <div class="stock-add-view__section-title text-secondary">
            {{ isPiece ? '3. Combien de pièces ?' : '3. La pesée des sachets' }}
          </div>

          <div v-if="isWeight" class="stock-add-view__weights">
            <div v-for="(row, i) in weightRows" :key="row.label" class="stock-add-view__weight-row">
              <span class="stock-add-view__weight-label">{{ row.label }}</span>
              <span class="stock-add-view__weight-value">{{ row.weightLabel }}</span>
              <v-btn
                icon
                variant="text"
                color="error"
                size="44"
                aria-label="Retirer"
                @click="removeWeight(i)"
              >
                <v-icon size="20">phosphor:trash</v-icon>
              </v-btn>
            </div>

            <div class="stock-add-view__weight-add">
              <AppTextField
                v-model="state.newWeight"
                type="number"
                inputmode="numeric"
                min="0"
                suffix="g"
                hide-details
                @keyup.enter="addWeight"
              />
              <AppButton @click="addWeight">
                <v-icon start size="18">phosphor:plus</v-icon>
                Peser
              </AppButton>
            </div>

            <div v-if="weightSummary" class="stock-add-view__weight-summary">{{ weightSummary }}</div>
          </div>

          <AppStepper v-else v-model="state.qty" :min="1" />
        </AppCard>
      </template>
    </div>

    <div class="stock-add-view__footer">
      <p v-if="saveError" class="stock-add-view__save-error text-error">{{ saveError }}</p>
      <AppButton
        block
        height="60"
        :color="canSave ? 'primary' : undefined"
        :disabled="!canSave || saving"
        @click="save"
      >
        {{ saveLabel }}
      </AppButton>
    </div>
  </v-container>
</template>

<style scoped>
.stock-add-view {
  padding-bottom: 110px;
}

.stock-add-view__sections {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.stock-add-view__section-title {
  font-size: 16px;
  font-weight: 600;
  margin-bottom: 12px;
}

.stock-add-view__chips {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
}

.stock-add-view__chip {
  border: 2px solid rgb(var(--v-theme-status-neutral-container));
  background: rgb(var(--v-theme-surface));
  color: rgb(var(--v-theme-on-surface));
  font-family: var(--font-body);
  font-size: 17px;
  font-weight: 500;
  padding: 12px 16px;
  border-radius: 12px;
  cursor: pointer;
  min-height: 48px;
}

.stock-add-view__chip--selected {
  border-color: rgb(var(--v-theme-primary));
  background: rgb(var(--v-theme-primary));
  color: rgb(var(--v-theme-surface));
}

.stock-add-view__batch-preview {
  display: flex;
  align-items: center;
  gap: 8px;
  background: rgb(var(--v-theme-status-neutral-container));
  border-radius: 10px;
  padding: 10px 14px;
  font-size: 15px;
  margin-top: 12px;
}

.stock-add-view__weights {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.stock-add-view__weight-row {
  display: flex;
  align-items: center;
  gap: 12px;
  background: rgb(var(--v-theme-surface));
  border: 1.5px solid rgb(var(--v-theme-status-neutral-container));
  border-radius: 10px;
  padding: 10px 8px 10px 16px;
}

.stock-add-view__weight-label {
  flex: 1;
  font-size: 17px;
}

.stock-add-view__weight-value {
  font-size: 18px;
  font-weight: 600;
}

.stock-add-view__weight-add {
  display: flex;
  align-items: center;
  gap: 10px;
}

.stock-add-view__weight-summary {
  font-size: 16px;
  color: rgb(var(--v-theme-success));
  font-weight: 600;
}

.stock-add-view__save-error {
  font-size: 14px;
  font-weight: 500;
  text-align: center;
  margin: 0 0 10px;
}

.stock-add-view__footer {
  position: fixed;
  left: 0;
  right: 0;
  bottom: 56px;
  padding: 14px 16px;
  background: linear-gradient(to top, rgb(var(--v-theme-background)) 70%, transparent);
}
</style>
