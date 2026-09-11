<script setup lang="ts">
import { computed, ref } from 'vue'
import AppButton from '@/components/base/AppButton.vue'
import { deleteSale } from '@/api/sales'
import { apiErrorMessage } from '@/composables/useApiError'
import type { SaleDto } from '@/api/types'

/**
 * Suppression d'une vente entière (RG-14), depuis le bas de son écran de détail.
 *
 * C'est le recours ultime : celui qui garantit qu'aucune saisie n'est définitive pour des
 * utilisateurs qui n'ont aucun accès à la base. Le serveur supprime les lignes avec la vente et
 * rend « disponible » toute unité qui ne porte plus aucun mouvement.
 *
 * L'annonce du retour en stock reste volontairement générale : une unité qui porte une autre
 * sortie (perso, perte) ne reviendra pas, et l'écran ne peut pas le savoir sans interroger les
 * mouvements de chaque unité. Mieux vaut une phrase juste qu'un décompte faux.
 */
const props = defineProps<{ sale: SaleDto }>()
const emit = defineEmits<{ deleted: [] }>()

const confirming = ref(false)
const submitting = ref(false)
const error = ref<string | null>(null)

const lineCount = computed(() => props.sale.lines.length)

async function confirm() {
  submitting.value = true
  error.value = null
  try {
    await deleteSale(props.sale.id)
    confirming.value = false
    emit('deleted')
  } catch (err) {
    error.value = apiErrorMessage(err, 'Suppression impossible, réessaie.')
    confirming.value = false
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <AppButton block height="56" color="error" variant="outlined" @click="confirming = true">
    <v-icon start size="18">phosphor:trash</v-icon>
    Supprimer cette vente
  </AppButton>

  <p v-if="error" class="sale-delete__error text-error">{{ error }}</p>

  <v-dialog v-model="confirming" max-width="380">
    <v-card class="sale-delete__dialog">
      <h2 class="text-h6 font-weight-bold mb-2">Supprimer cette vente ?</h2>
      <p class="text-secondary mb-2">
        La vente {{ sale.saleNumber }} et
        {{ lineCount === 1 ? 'sa ligne' : `ses ${lineCount} lignes` }} disparaîtront, y compris de
        l'historique d'achats de {{ sale.customerName }}.
      </p>
      <p class="text-secondary sale-delete__hint">
        Les unités qui n'ont pas d'autre sortie reviennent en stock. C'est définitif.
      </p>

      <div class="sale-delete__actions">
        <v-btn variant="text" color="secondary" @click="confirming = false">Annuler</v-btn>
        <v-btn color="error" variant="flat" :loading="submitting" @click="confirm">Supprimer</v-btn>
      </div>
    </v-card>
  </v-dialog>
</template>

<style scoped>
.sale-delete__dialog {
  padding: 20px;
  border-radius: 16px;
}

.sale-delete__hint {
  font-size: 14px;
  font-weight: 500;
  margin-bottom: 16px;
}

.sale-delete__actions {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
}

.sale-delete__error {
  font-size: 14px;
  font-weight: 500;
  text-align: center;
  margin: 10px 0 0;
}
</style>
