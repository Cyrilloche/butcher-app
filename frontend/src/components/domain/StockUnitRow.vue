<script setup lang="ts">
import { computed } from 'vue'
import StockStatusBadge from '@/components/domain/StockStatusBadge.vue'
import type { StockDetailUnit } from '@/composables/useStock'

/**
 * Une unité physique, sur deux lignes de hauteur fixe.
 *
 * En haut, le numéro d'étiquette — le plus gros caractère de la ligne, puisque c'est celui que
 * l'utilisateur recopie à la main — et son poids pesé. En bas, l'état, le poids encore vendable et
 * les actions. Deux lignes voulues plutôt qu'une seule qui déborde : sur un téléphone, tout tenait
 * auparavant sur une ligne qui passait à la suivante au premier libellé un peu long.
 *
 * Le poids apparaît deux fois sur une unité entamée, et c'est délibéré : en haut le poids pesé à la
 * fabrication, préfixé de « pesé » pour lever l'ambiguïté, en bas ce qu'il reste à vendre. Sur une
 * unité intacte les deux seraient identiques, donc la ligne du bas n'affiche rien.
 */
const props = defineProps<{ unit: StockDetailUnit }>()

/** Le restant ne se répète que quand il diffère du poids pesé, donc sur une unité entamée. */
const showsRemaining = computed(
  () => props.unit.status === 'opened' && props.unit.remainingLabel != null,
)
</script>

<template>
  <div class="stock-unit-row">
    <span class="stock-unit-row__num">{{ unit.number }}</span>
    <span v-if="unit.weightLabel" class="stock-unit-row__weighed text-secondary">
      <template v-if="showsRemaining">pesé </template>{{ unit.weightLabel }}
    </span>

    <div class="stock-unit-row__state">
      <StockStatusBadge :status="unit.status" />
      <span v-if="showsRemaining" class="stock-unit-row__remaining">
        {{ unit.remainingLabel }} restants
      </span>
      <!-- Purement informatif : la clôture se fait depuis le menu de la ligne. -->
      <span v-if="unit.isEmptied" class="stock-unit-row__toclose">à clôturer</span>
    </div>

    <div class="stock-unit-row__actions">
      <slot name="action" />
    </div>
  </div>
</template>

<style scoped>
.stock-unit-row {
  display: grid;
  grid-template-columns: 1fr auto;
  align-items: center;
  row-gap: 2px;
  column-gap: 10px;
  min-height: 68px;
  padding: 8px 0;
}

/* Un filet *entre* les unités, jamais au-dessus de la première : il sépare, il n'encadre pas.
   Le voisinage plutôt que `:first-child`, qui ne mordait pas — la ligne n'est pas le premier
   enfant de la carte. */
.stock-unit-row + .stock-unit-row {
  border-top: 1px solid rgb(var(--v-theme-status-neutral-container));
}

.stock-unit-row__num {
  font-size: 18px;
  font-weight: 600;
  font-variant-numeric: tabular-nums;
}

.stock-unit-row__weighed {
  justify-self: end;
  font-size: 15px;
  font-weight: 500;
  font-variant-numeric: tabular-nums;
  white-space: nowrap;
}

.stock-unit-row__state {
  display: flex;
  align-items: center;
  gap: 9px;
  flex-wrap: wrap;
  min-width: 0;
}

.stock-unit-row__remaining {
  font-size: 16px;
  font-weight: 600;
  font-variant-numeric: tabular-nums;
}

.stock-unit-row__toclose {
  font-size: 13px;
  font-weight: 500;
  color: rgb(var(--v-theme-error));
}

.stock-unit-row__actions {
  justify-self: end;
  display: flex;
  align-items: center;
  gap: 2px;
}
</style>
