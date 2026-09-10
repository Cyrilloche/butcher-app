<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { listStockUnits } from '@/api/stockUnits'
import { writeOffProductStock } from '@/api/products'
import { ApiError } from '@/api/http'
import { formatWeight } from '@/composables/useStock'
import type { StockUnitDto } from '@/api/types'

/**
 * Solde des unités restantes d'un produit, pour pouvoir le désactiver.
 *
 * L'utilisatrice choisit les unités à solder ; le type de sortie est toujours « perte » sur cette
 * action groupée. Une unité réellement consommée en perso se sort une par une depuis le détail
 * stock. Le poids de chaque sortie est calculé par le serveur, jamais envoyé d'ici.
 */
const props = defineProps<{ modelValue: boolean; productId: number; productName: string }>()
const emit = defineEmits<{ 'update:modelValue': [value: boolean]; done: [] }>()

const units = ref<StockUnitDto[]>([])
const selected = ref<number[]>([])
const loading = ref(false)
const submitting = ref(false)
const error = ref<string | null>(null)

const allSelected = computed(() => units.value.length > 0 && selected.value.length === units.value.length)

function toggleAll() {
  selected.value = allSelected.value ? [] : units.value.map((u) => u.id)
}

function unitLabel(unit: StockUnitDto): string {
  const weight = unit.weight != null ? ` · ${formatWeight(Math.round(unit.weight * 1000))}` : ''
  const opened = unit.status === 'opened' ? ' · entamée' : ''
  return `${unit.batchNumber}${weight}${opened}`
}

watch(
  () => props.modelValue,
  async (open) => {
    if (!open) return
    loading.value = true
    error.value = null
    selected.value = []
    try {
      const all = await listStockUnits({ productId: props.productId })
      units.value = all.filter((u) => u.status === 'available' || u.status === 'opened')
      selected.value = units.value.map((u) => u.id)
    } catch (err) {
      error.value = err instanceof ApiError ? err.message : 'Impossible de charger les unités.'
    } finally {
      loading.value = false
    }
  },
)

async function confirm() {
  submitting.value = true
  error.value = null
  try {
    await writeOffProductStock(props.productId, selected.value)
    emit('update:modelValue', false)
    emit('done')
  } catch (err) {
    error.value = err instanceof ApiError ? err.message : 'Erreur, réessaie.'
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <v-dialog
    :model-value="modelValue"
    max-width="420"
    @update:model-value="(value) => emit('update:modelValue', value)"
  >
    <v-card class="write-off__dialog">
      <h2 class="text-h6 font-weight-bold mb-2">Solder le stock restant</h2>
      <p class="text-secondary write-off__intro">
        Les unités cochées seront déclarées perdues, pour pouvoir désactiver
        « {{ productName }} ». C'est définitif.
      </p>

      <p v-if="loading" class="text-secondary">Chargement des unités...</p>
      <p v-else-if="units.length === 0" class="text-secondary">
        Il ne reste aucune unité à solder pour ce produit.
      </p>

      <template v-else>
        <button type="button" class="write-off__select-all" @click="toggleAll">
          {{ allSelected ? 'Tout décocher' : 'Tout cocher' }}
        </button>
        <div class="write-off__units">
          <v-checkbox
            v-for="unit in units"
            :key="unit.id"
            v-model="selected"
            :value="unit.id"
            :label="unitLabel(unit)"
            color="primary"
            density="compact"
            hide-details
          />
        </div>
      </template>

      <p v-if="error" class="text-error write-off__error">{{ error }}</p>

      <div class="write-off__actions">
        <v-btn variant="text" color="secondary" @click="emit('update:modelValue', false)">Annuler</v-btn>
        <v-btn
          color="error"
          variant="flat"
          :loading="submitting"
          :disabled="selected.length === 0"
          @click="confirm"
        >
          Déclarer {{ selected.length === 1 ? '1 perte' : `${selected.length} pertes` }}
        </v-btn>
      </div>
    </v-card>
  </v-dialog>
</template>

<style scoped>
.write-off__dialog {
  padding: 20px;
  border-radius: 16px;
}

.write-off__intro {
  font-size: 14px;
  line-height: 1.45;
  margin-bottom: 12px;
}

.write-off__select-all {
  background: none;
  border: none;
  padding: 0;
  color: rgb(var(--v-theme-primary));
  font-weight: 600;
  font-size: 14px;
  cursor: pointer;
}

.write-off__units {
  max-height: 240px;
  overflow-y: auto;
  margin: 6px 0 12px;
}

.write-off__error {
  font-size: 14px;
  font-weight: 500;
  margin-bottom: 12px;
}

.write-off__actions {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
}
</style>
