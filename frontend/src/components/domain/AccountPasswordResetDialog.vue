<script setup lang="ts">
import { ref, watch } from 'vue'
import AppTextField from '@/components/base/AppTextField.vue'
import { resetAccountPassword } from '@/api/accounts'
import { apiErrorMessage } from '@/composables/useApiError'
import { passwordRuleText } from '@/composables/useAccounts'
import type { AccountDto } from '@/api/types'

/**
 * Réinitialisation du mot de passe d'un compte par l'administrateur (FR-003) : oubli, ou compte
 * verrouillé. Le serveur lève le verrouillage et ferme les sessions ouvertes du compte. Le nouveau mot
 * de passe se transmet de vive voix : l'outil n'envoie aucun message.
 */
const props = defineProps<{ modelValue: boolean; account: AccountDto | null }>()
const emit = defineEmits<{ 'update:modelValue': [value: boolean]; done: [] }>()

const password = ref('')
const submitting = ref(false)
const error = ref<string | null>(null)

watch(
  () => props.modelValue,
  (open) => {
    if (!open) return
    password.value = ''
    error.value = null
  },
)

function close() {
  emit('update:modelValue', false)
}

async function save() {
  if (!props.account || password.value.length === 0) return
  submitting.value = true
  error.value = null
  try {
    await resetAccountPassword(props.account.id, { newPassword: password.value })
    close()
    emit('done')
  } catch (err) {
    error.value = apiErrorMessage(err, 'Réinitialisation impossible, réessaie.')
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <v-dialog
    :model-value="modelValue"
    max-width="440"
    @update:model-value="(value) => emit('update:modelValue', value)"
  >
    <v-card v-if="account" class="account-reset__dialog">
      <h2 class="text-h6 font-weight-bold mb-2">Nouveau mot de passe pour {{ account.displayName }}</h2>
      <p class="text-secondary account-reset__intro">
        {{ account.displayName }} sera déconnecté de ses appareils et devra utiliser ce mot de passe.
      </p>

      <AppTextField v-model="password" type="password" autocomplete="new-password" label="Nouveau mot de passe" />
      <p class="text-secondary account-reset__hint">{{ passwordRuleText(account.role) }}</p>

      <p v-if="error" class="account-reset__error text-error">{{ error }}</p>

      <div class="account-reset__actions">
        <v-btn variant="text" color="secondary" @click="close">Annuler</v-btn>
        <v-btn
          color="primary"
          variant="flat"
          :disabled="password.length === 0"
          :loading="submitting"
          @click="save"
        >
          Enregistrer
        </v-btn>
      </div>
    </v-card>
  </v-dialog>
</template>

<style scoped>
.account-reset__dialog {
  padding: 20px;
  border-radius: 16px;
}

.account-reset__intro {
  font-size: 15px;
  font-weight: 500;
  margin: 0 0 14px;
}

.account-reset__hint {
  font-size: 14px;
  font-weight: 500;
  margin: 8px 0 0;
  overflow-wrap: anywhere;
}

.account-reset__error {
  font-size: 14px;
  font-weight: 500;
  margin: 12px 0 0;
}

.account-reset__actions {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
  margin-top: 20px;
}
</style>
