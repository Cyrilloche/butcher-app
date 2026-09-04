<!--
  Actions de sortie d'une unité physique, depuis Détail Stock (RF-21, RG-12) :
  usage personnel, perte, et clôture d'une unité entamée (RF-20).

  Une sortie change le statut de l'unité et la fait quitter le stock — geste
  irréversible depuis l'interface (la suppression du mouvement existe côté API,
  RG-11, mais n'est pas exposée). D'où la confirmation systématique, qui rappelle
  l'unité concernée et ce qui va se passer.
-->
<script setup lang="ts">
import { computed, ref } from 'vue'
import { createStockMovement } from '@/api/stockMovements'
import { closeStockUnit } from '@/api/stockUnits'
import { getRemainingWeightKg, formatWeight, type StockDetailUnit } from '@/composables/useStock'
import { ApiError } from '@/api/http'

const props = defineProps<{ unit: StockDetailUnit }>()
const emit = defineEmits<{ done: []; failed: [message: string] }>()

type Outcome = 'personal' | 'loss' | 'close'

const pending = ref<Outcome | null>(null)
const submitting = ref(false)
const preparing = ref(false)
/** Poids qui sera enregistré sur le mouvement — restant estimé, pas poids d'origine. */
const outcomeWeightKg = ref<number | null>(null)

const dialog = computed(() => {
  switch (pending.value) {
    case 'personal':
      return {
        title: 'Sortie pour usage perso ?',
        body: `L'unité ${props.unit.number} quittera le stock et sera marquée « Perso ».`,
        cta: 'Marquer en perso',
        color: 'secondary',
      }
    case 'loss':
      return {
        title: 'Déclarer une perte ?',
        body: `L'unité ${props.unit.number} quittera le stock et sera marquée « Perdue ».`,
        cta: 'Déclarer la perte',
        color: 'error',
      }
    case 'close':
      return {
        title: 'Clôturer cette unité ?',
        body: `L'unité ${props.unit.number} est entamée. La clôturer la marque « Vendue » : elle ne pourra plus recevoir de vente.`,
        cta: 'Clôturer',
        color: 'primary',
      }
    default:
      return null
  }
})

/**
 * Une unité entamée a déjà été vendue en partie : enregistrer son poids d'origine
 * compterait deux fois la part vendue. On repart donc du restant estimé (RG-05).
 * À zéro, il n'y a plus rien à sortir — l'unité doit être clôturée.
 */
const nothingLeft = computed(
  () => pending.value !== 'close' && outcomeWeightKg.value != null && outcomeWeightKg.value <= 0,
)

const weightHint = computed(() =>
  outcomeWeightKg.value != null && outcomeWeightKg.value > 0
    ? `Poids enregistré : ${formatWeight(Math.round(outcomeWeightKg.value * 1000))}`
    : null,
)

async function open(outcome: Outcome) {
  pending.value = outcome
  outcomeWeightKg.value = null
  if (outcome === 'close' || props.unit.weightKg == null) return

  preparing.value = true
  try {
    outcomeWeightKg.value = await getRemainingWeightKg(props.unit.id, props.unit.weightKg)
  } catch {
    // Le restant n'a pas pu être calculé : on retombe sur le poids pesé, que le
    // backend revalidera de toute façon.
    outcomeWeightKg.value = props.unit.weightKg
  } finally {
    preparing.value = false
  }
}

async function confirm() {
  if (pending.value == null) return
  submitting.value = true
  try {
    if (pending.value === 'close') {
      await closeStockUnit(props.unit.id)
    } else {
      await createStockMovement(props.unit.id, {
        type: pending.value,
        // Requis pour un produit au poids, interdit pour un produit à la pièce.
        ...(outcomeWeightKg.value != null ? { soldWeight: outcomeWeightKg.value } : {}),
      })
    }
    pending.value = null
    emit('done')
  } catch (err) {
    emit('failed', err instanceof ApiError ? err.message : 'Erreur, réessaie.')
    pending.value = null
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <v-menu location="bottom end">
    <template #activator="{ props: menuProps }">
      <button
        v-bind="menuProps"
        type="button"
        class="stock-unit-outcome__trigger"
        :aria-label="`Actions sur l'unité ${unit.number}`"
      >
        <v-icon size="20">phosphor:dots-three-vertical</v-icon>
      </button>
    </template>

    <v-list density="comfortable" class="stock-unit-outcome__list">
      <v-list-item v-if="unit.status === 'opened'" @click="open('close')">
        <template #prepend><v-icon size="20">phosphor:check-circle</v-icon></template>
        <v-list-item-title>Clôturer (vendue)</v-list-item-title>
      </v-list-item>
      <v-list-item @click="open('personal')">
        <template #prepend><v-icon size="20">phosphor:house</v-icon></template>
        <v-list-item-title>Usage perso</v-list-item-title>
      </v-list-item>
      <v-list-item @click="open('loss')">
        <template #prepend><v-icon size="20">phosphor:trash</v-icon></template>
        <v-list-item-title>Déclarer une perte</v-list-item-title>
      </v-list-item>
    </v-list>
  </v-menu>

  <v-dialog :model-value="pending !== null" max-width="380" @update:model-value="pending = null">
    <v-card v-if="dialog" class="stock-unit-outcome__dialog">
      <h2 class="text-h6 font-weight-bold mb-2">{{ dialog.title }}</h2>
      <p class="text-secondary mb-2">{{ dialog.body }}</p>

      <p v-if="preparing" class="text-secondary stock-unit-outcome__hint">Calcul du poids restant...</p>
      <p v-else-if="nothingLeft" class="text-error stock-unit-outcome__hint">
        Cette unité a déjà été vendue en totalité : il n'y a plus rien à sortir. Clôture-la plutôt.
      </p>
      <p v-else-if="weightHint" class="text-secondary stock-unit-outcome__hint">{{ weightHint }}</p>

      <div class="stock-unit-outcome__actions">
        <v-btn variant="text" color="secondary" @click="pending = null">Annuler</v-btn>
        <v-btn
          :color="dialog.color"
          variant="flat"
          :loading="submitting"
          :disabled="preparing || nothingLeft"
          @click="confirm"
        >
          {{ dialog.cta }}
        </v-btn>
      </div>
    </v-card>
  </v-dialog>
</template>

<style scoped>
.stock-unit-outcome__trigger {
  width: 36px;
  height: 40px;
  display: flex;
  align-items: center;
  justify-content: center;
  border: none;
  background: none;
  color: rgb(var(--v-theme-secondary));
  border-radius: 10px;
  cursor: pointer;
  flex-shrink: 0;
}

.stock-unit-outcome__trigger:hover {
  background: rgb(var(--v-theme-status-neutral-container));
}

.stock-unit-outcome__list {
  border-radius: 12px;
}

.stock-unit-outcome__dialog {
  padding: 20px;
  border-radius: 16px;
}

.stock-unit-outcome__hint {
  font-size: 14px;
  font-weight: 500;
  margin-bottom: 16px;
}

.stock-unit-outcome__actions {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
}
</style>
