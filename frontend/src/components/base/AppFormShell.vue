<script setup lang="ts">
import { useRouter } from 'vue-router'
import { useDisplay } from 'vuetify'
import AppPageHeader from '@/components/base/AppPageHeader.vue'
import AppButton from '@/components/base/AppButton.vue'
import { ADD_DIALOG_QUERY } from '@/composables/useAddDialog'

/**
 * Cadre d'un formulaire d'ajout (stock, vente, client, produit).
 *
 * - `dialog` : contenu d'une fenêtre ouverte depuis la liste sur écran large, dans le style de la création
 *   d'un compte — titre, sections séparées d'un filet, « Annuler » et le bouton principal à droite.
 * - sinon : page de téléphone, lien de retour et grand bouton fixé en bas d'écran. Sur écran large, cette
 *   page renvoie vers sa liste, qui ouvre la fenêtre (lien direct, retour arrière).
 */
const props = defineProps<{
  title: string
  backTo: string
  backLabel: string
  dialog?: boolean
  saveLabel: string
  canSave: boolean
  saving: boolean
  error: string | null
}>()
const emit = defineEmits<{ save: []; cancel: [] }>()

const { mdAndUp } = useDisplay()
const router = useRouter()

if (!props.dialog && mdAndUp.value) {
  router.replace({ path: props.backTo, query: { [ADD_DIALOG_QUERY]: '1' } })
}
</script>

<template>
  <v-card v-if="dialog" class="app-form-shell--dialog">
    <h2 class="text-h6 font-weight-bold app-form-shell__title">{{ title }}</h2>

    <div class="app-form-shell__body">
      <slot />
    </div>

    <p v-if="error" class="app-form-shell__error app-form-shell__error--right text-error">{{ error }}</p>

    <div class="app-form-shell__actions">
      <v-btn variant="text" color="secondary" @click="emit('cancel')">Annuler</v-btn>
      <v-btn color="primary" variant="flat" :disabled="!canSave" :loading="saving" @click="emit('save')">
        {{ saveLabel }}
      </v-btn>
    </div>
  </v-card>

  <v-container v-else class="app-form-container">
    <AppPageHeader :to="backTo" :back-label="backLabel" :title="title" />

    <slot />

    <div class="app-fixed-footer">
      <p v-if="error" class="app-form-shell__error text-error">{{ error }}</p>
      <AppButton block height="60" :color="canSave ? 'primary' : undefined" :disabled="!canSave || saving" @click="emit('save')">
        {{ saveLabel }}
      </AppButton>
    </div>
  </v-container>
</template>

<style scoped>
.app-form-shell--dialog {
  padding: 20px;
  border-radius: 16px;
}

.app-form-shell__title {
  margin-bottom: 16px;
}

/* Le corps défile, le titre et les actions restent visibles. */
.app-form-shell__body {
  flex: 1 1 auto;
  min-height: 0;
  overflow-y: auto;
}

.app-form-shell__error {
  font-size: 14px;
  font-weight: 500;
  text-align: center;
  margin: 0 0 10px;
}

.app-form-shell__error--right {
  text-align: right;
  margin: 12px 0 0;
}

.app-form-shell__actions {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
  margin-top: 20px;
}
</style>
