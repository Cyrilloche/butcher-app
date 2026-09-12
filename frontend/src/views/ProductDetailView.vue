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
import SaleModeToggle from '@/components/domain/SaleModeToggle.vue'
import ProductWriteOffDialog from '@/components/domain/ProductWriteOffDialog.vue'
import { useAuthStore } from '@/stores/auth'
import type { ProductDto, SaleMode } from '@/api/types'

const props = defineProps<{ code: string }>()

// Désactiver, réactiver et solder un produit engagent tout le catalogue : gestes réservés à
// l'administrateur (FR-011). L'interface les masque ; le serveur les refuse de toute façon (403).
const auth = useAuthStore()

async function loadProduct(): Promise<ProductDto | null> {
  const all = await listProducts(true)
  return all.find((p) => p.code.toUpperCase() === props.code.toUpperCase()) ?? null
}

const { data: product, loading, error, reload } = useAsyncData(loadProduct, null)
const { data: stockSummary } = useAsyncData(async () => (await getStockDetail(props.code))?.summary ?? null, null)

watch(() => props.code, reload)

const state = reactive({ name: '', allowPartialSale: false, code: '', saleMode: 'by_weight' as SaleMode })

watch(
  product,
  (p) => {
    if (!p) return
    state.name = p.name
    state.allowPartialSale = p.allowPartialSale
    state.code = p.code
    state.saleMode = p.saleMode
  },
  { immediate: true },
)

/**
 * Le code et le mode de vente ne sont modifiables que tant qu'aucun lot n'est rattaché : le code
 * apparaît dans des numéros de lot recopiés à la main sur les étiquettes, et le mode de vente
 * détermine la lecture des ventes passées. Le serveur applique la même règle, l'interface ne fait
 * que la refléter.
 */
const identityLocked = computed(() => product.value?.isUsed ?? false)

const dirty = computed(
  () =>
    !!product.value &&
    (state.name !== product.value.name ||
      state.allowPartialSale !== product.value.allowPartialSale ||
      state.code.toUpperCase() !== product.value.code ||
      state.saleMode !== product.value.saleMode),
)

const canSave = computed(() => state.name.trim().length > 0 && state.code.trim().length > 0)
const saving = ref(false)
const saveError = ref<string | null>(null)

async function save() {
  if (!product.value || !canSave.value) return
  saving.value = true
  saveError.value = null
  try {
    await updateProduct(product.value.id, {
      name: state.name.trim(),
      allowPartialSale: state.saleMode === 'by_weight' && state.allowPartialSale,
      code: state.code.trim().toUpperCase(),
      saleMode: state.saleMode,
    })
    await reload()
  } catch (err) {
    saveError.value = err instanceof ApiError ? err.message : "Erreur lors de l'enregistrement, réessaie."
  } finally {
    saving.value = false
  }
}

const togglingStatus = ref(false)
const statusError = ref<string | null>(null)
const confirmingStatus = ref(false)
const writingOff = ref(false)

/** Il reste du stock : la désactivation sera refusée par le serveur, le solde est la porte de sortie. */
const hasRemainingStock = computed(() => (product.value?.remainingStockUnitCount ?? 0) > 0)

async function toggleStatus() {
  if (!product.value) return
  confirmingStatus.value = false
  togglingStatus.value = true
  statusError.value = null
  try {
    if (product.value.isActive) await deactivateProduct(product.value.id)
    else await reactivateProduct(product.value.id)
    await reload()
  } catch (err) {
    // Le refus vient du serveur, qui nomme le nombre d'unités restantes : on l'affiche tel quel.
    statusError.value = err instanceof ApiError ? err.message : 'Erreur, réessaie.'
  } finally {
    togglingStatus.value = false
  }
}

async function onWriteOffDone() {
  statusError.value = null
  await reload()
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
        <AppTextField
          v-if="!identityLocked"
          v-model="state.code"
          label="Code (numéros de lot)"
          :disabled="!product.isActive"
          style="max-width: 110px"
        />
        <template v-else>
          <AppTextField :model-value="product.code" label="Code (numéros de lot)" disabled style="max-width: 110px" />
          <p class="product-detail-view__locked text-secondary">
            Ce produit a déjà servi à fabriquer au moins un lot : son code est figé, car il est
            recopié sur les étiquettes des lots existants. Pour en changer, désactivez ce produit
            et créez-en un nouveau.
          </p>
        </template>
      </AppCard>

      <AppCard>
        <div class="product-detail-view__section-title text-secondary">Mode de vente</div>
        <SaleModeToggle v-if="!identityLocked && product.isActive" v-model="state.saleMode" />
        <template v-else>
          <div class="product-detail-view__mode">
            <v-icon size="22">phosphor:{{ product.saleMode === 'by_weight' ? 'scales' : 'hand-coins' }}</v-icon>
            {{ product.saleMode === 'by_weight' ? 'Au poids' : 'À la pièce' }}
          </div>
          <p v-if="identityLocked" class="product-detail-view__locked text-secondary">
            Le mode de vente est figé lui aussi : il détermine la façon dont les ventes déjà
            enregistrées se lisent. Même marche à suivre pour en changer.
          </p>
        </template>

        <v-checkbox
          v-if="state.saleMode === 'by_weight'"
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
        v-if="auth.isAdmin"
        block
        height="56"
        :color="product.isActive ? 'error' : 'success'"
        variant="outlined"
        :disabled="togglingStatus"
        @click="confirmingStatus = true"
      >
        <v-icon start size="18">phosphor:{{ product.isActive ? 'trash' : 'plus' }}</v-icon>
        {{ product.isActive ? 'Désactiver ce produit' : 'Réactiver ce produit' }}
      </AppButton>

      <template v-if="statusError && auth.isAdmin">
        <p class="product-detail-view__status-error text-error">{{ statusError }}</p>
        <AppButton
          v-if="hasRemainingStock"
          block
          height="48"
          color="error"
          variant="flat"
          @click="writingOff = true"
        >
          Solder les {{ product.remainingStockUnitCount }} unités restantes
        </AppButton>
      </template>

      <p v-if="auth.isAdmin" class="product-detail-view__hint text-secondary">
        Un produit désactivé n'apparaît plus dans l'ajout au stock ni dans les ventes. Son historique est conservé.
      </p>
      <p v-else class="product-detail-view__hint text-secondary">
        Retirer un produit du catalogue est réservé à l'administrateur.
      </p>

      <v-dialog v-model="confirmingStatus" max-width="380">
        <v-card class="product-detail-view__dialog">
          <h2 class="text-h6 font-weight-bold mb-2">
            {{ product.isActive ? 'Désactiver ce produit ?' : 'Réactiver ce produit ?' }}
          </h2>
          <p class="text-secondary product-detail-view__dialog-body">
            <template v-if="product.isActive">
              « {{ product.name }} » disparaîtra de l'ajout au stock et de la saisie des ventes.
              Son historique reste consultable, et vous pourrez le réactiver.
            </template>
            <template v-else>
              « {{ product.name }} » réapparaîtra dans l'ajout au stock et dans la saisie des ventes.
            </template>
          </p>
          <div class="product-detail-view__dialog-actions">
            <v-btn variant="text" color="secondary" @click="confirmingStatus = false">Annuler</v-btn>
            <v-btn
              :color="product.isActive ? 'error' : 'success'"
              variant="flat"
              :loading="togglingStatus"
              @click="toggleStatus"
            >
              {{ product.isActive ? 'Désactiver' : 'Réactiver' }}
            </v-btn>
          </div>
        </v-card>
      </v-dialog>

      <ProductWriteOffDialog
        v-model="writingOff"
        :product-id="product.id"
        :product-name="product.name"
        @done="onWriteOffDone"
      />
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

.product-detail-view__locked {
  font-size: 14px;
  line-height: 1.45;
  margin: 10px 0 0;
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

.product-detail-view__status-error {
  font-size: 14px;
  font-weight: 500;
  text-align: center;
  margin: 0;
}

.product-detail-view__dialog {
  padding: 20px;
  border-radius: 16px;
}

.product-detail-view__dialog-body {
  font-size: 14px;
  line-height: 1.45;
  margin-bottom: 16px;
}

.product-detail-view__dialog-actions {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
}

.product-detail-view__save-error {
  font-size: 14px;
  font-weight: 500;
  text-align: center;
  margin: 0 0 10px;
}

.product-detail-view__footer {
  position: fixed;
  /* Commence au bord de la barre latérale sur écran large, contenu centré comme le formulaire. */
  left: var(--v-layout-left, 0px);
  right: var(--v-layout-right, 0px);
  bottom: 0;
  padding: 14px max(16px, calc((100% - 720px) / 2)) 34px;
  background: linear-gradient(to top, rgb(var(--v-theme-background)) 70%, transparent);
}
</style>
