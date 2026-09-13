<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import AppFormShell from '@/components/base/AppFormShell.vue'
import AppCard from '@/components/base/AppCard.vue'
import AppTextField from '@/components/base/AppTextField.vue'
import AppHintBox from '@/components/base/AppHintBox.vue'
import SaleModeToggle from '@/components/domain/SaleModeToggle.vue'
import { createProduct } from '@/api/products'
import { ApiError } from '@/api/http'
import type { SaleMode } from '@/api/types'

/** `dialog` : ouvert en fenêtre depuis la liste (écran large), qui se recharge sur `saved`. */
const props = defineProps<{ dialog?: boolean }>()
const emit = defineEmits<{ saved: []; cancel: [] }>()

const router = useRouter()

const state = reactive({
  name: '',
  code: '',
  saleMode: 'by_weight' as SaleMode,
  allowPartialSale: false,
})

const batchDateCode = new Date().toISOString().slice(2, 10).replace(/-/g, '')
const batchPreview = computed(() => `${state.code.trim() ? state.code.toUpperCase() : '——'}-${batchDateCode}-1`)

const modeHint = computed(() =>
  state.saleMode === 'by_weight'
    ? 'Chaque unité sera pesée à l’ajout au stock, prix en € / kg.'
    : 'Les unités se comptent simplement, prix en € / pièce.',
)

const canSave = computed(() => state.name.trim().length > 0 && state.code.trim().length >= 2)

const saving = ref(false)
const saveError = ref<string | null>(null)

async function save() {
  if (!canSave.value) return
  saving.value = true
  saveError.value = null
  try {
    await createProduct({
      code: state.code.trim().toUpperCase(),
      name: state.name.trim(),
      saleMode: state.saleMode,
      allowPartialSale: state.saleMode === 'by_weight' && state.allowPartialSale,
    })
    if (props.dialog) emit('saved')
    else await router.push('/products')
  } catch (err) {
    saveError.value = err instanceof ApiError ? err.message : "Erreur lors de l'enregistrement, réessaie."
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <AppFormShell
    title="Nouveau produit"
    back-to="/products"
    back-label="Produits"
    :dialog="dialog"
    :save-label="saving ? 'Création...' : 'Créer le produit'"
    :can-save="canSave"
    :saving="saving"
    :error="saveError"
    @save="save"
    @cancel="emit('cancel')"
  >

    <div class="product-add-view__sections">
      <AppCard>
        <div class="product-add-view__section-title text-secondary">1. Le produit</div>
        <AppTextField v-model="state.name" label="Nom du produit" placeholder="Ex. Saucisson à l'ail" class="mb-3" />

        <!-- Libellé hors de la rangée : borné à la largeur du champ, il se coupait mot par mot. -->
        <label for="product-code" class="product-add-view__code-label">Code (numéros de lot)</label>
        <div class="product-add-view__code-row">
          <AppTextField id="product-code" v-model="state.code" maxlength="3" placeholder="SA" class="product-add-view__code-field" />
          <div class="text-secondary product-add-view__code-hint">
            <span class="product-add-view__code-hint-label">Exemple de numéro :</span>
            <strong class="product-add-view__code-hint-value">{{ batchPreview }}</strong>
          </div>
        </div>

      </AppCard>

      <AppCard>
        <div class="product-add-view__section-title text-secondary">2. Comment se vend-il ?</div>
        <SaleModeToggle v-model="state.saleMode" class="mb-3" />
        <AppHintBox icon="info">{{ modeHint }}</AppHintBox>

        <v-checkbox
          v-if="state.saleMode === 'by_weight'"
          v-model="state.allowPartialSale"
          label="Peut être vendu à la tranche (ex. jambon entier)"
          color="primary"
          density="comfortable"
          hide-details
          class="mt-2"
        />
      </AppCard>
    </div>

  </AppFormShell>
</template>

<style scoped>
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

.product-add-view__code-label {
  display: block;
  font-size: 15px;
  font-weight: 500;
  margin-bottom: 6px;
}

.product-add-view__code-row {
  display: flex;
  align-items: center;
  gap: 12px;
}

.product-add-view__code-field {
  flex: 0 0 110px;
}

.product-add-view__code-field :deep(input) {
  text-transform: uppercase;
}

/* Libellé sur une ligne, numéro recalculé à la frappe juste en dessous ; ni l'un ni l'autre ne se coupe. */
.product-add-view__code-hint {
  display: flex;
  flex-direction: column;
  gap: 2px;
  min-width: 0;
  font-size: 15px;
}

.product-add-view__code-hint-label,
.product-add-view__code-hint-value {
  white-space: nowrap;
}

.product-add-view__code-hint-value {
  font-size: 17px;
}
</style>
