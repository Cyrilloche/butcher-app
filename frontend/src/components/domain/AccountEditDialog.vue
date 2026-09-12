<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue'
import AppTextField from '@/components/base/AppTextField.vue'
import { createAccount, updateAccount } from '@/api/accounts'
import { apiErrorMessage } from '@/composables/useApiError'
import { accountRoleLabels, passwordRuleText } from '@/composables/useAccounts'
import type { AccountDto, AccountRole } from '@/api/types'

/**
 * Création d'un compte, ou modification de son nom et de son rôle (FR-003).
 *
 * L'email ne change pas après création : c'est l'identifiant de connexion. Promouvoir un utilisateur
 * demande, dans le même geste, un nouveau mot de passe conforme à la règle administrateur (FR-035).
 * Les règles sont appliquées par le serveur ; ses refus sont affichés tels quels.
 */
const props = defineProps<{ modelValue: boolean; account: AccountDto | null }>()
const emit = defineEmits<{ 'update:modelValue': [value: boolean]; saved: [] }>()

const roles: AccountRole[] = ['user', 'admin']

const draft = reactive({ email: '', displayName: '', role: 'user' as AccountRole, password: '' })
const submitting = ref(false)
const error = ref<string | null>(null)

const isCreation = computed(() => props.account === null)
const isPromotion = computed(() => props.account?.role === 'user' && draft.role === 'admin')
const needsPassword = computed(() => isCreation.value || isPromotion.value)

const canSave = computed(
  () =>
    draft.displayName.trim().length > 0 &&
    (!isCreation.value || draft.email.trim().length > 0) &&
    (!needsPassword.value || draft.password.length > 0),
)

watch(
  () => props.modelValue,
  (open) => {
    if (!open) return
    draft.email = props.account?.email ?? ''
    draft.displayName = props.account?.displayName ?? ''
    draft.role = props.account?.role ?? 'user'
    draft.password = ''
    error.value = null
  },
)

function close() {
  emit('update:modelValue', false)
}

async function save() {
  if (!canSave.value) return
  submitting.value = true
  error.value = null
  try {
    if (props.account === null) {
      await createAccount({
        email: draft.email.trim(),
        displayName: draft.displayName.trim(),
        role: draft.role,
        password: draft.password,
      })
    } else {
      await updateAccount(props.account.id, {
        displayName: draft.displayName.trim(),
        role: draft.role,
        newPassword: isPromotion.value ? draft.password : undefined,
      })
    }
    close()
    emit('saved')
  } catch (err) {
    error.value = apiErrorMessage(err, 'Enregistrement impossible, réessaie.')
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
    <v-card class="account-edit__dialog">
      <h2 class="text-h6 font-weight-bold mb-4">
        {{ isCreation ? 'Nouveau compte' : `Modifier ${account?.displayName}` }}
      </h2>

      <AppTextField v-model="draft.displayName" label="Nom affiché" class="mb-3" />

      <AppTextField
        v-if="isCreation"
        v-model="draft.email"
        type="email"
        inputmode="email"
        autocomplete="off"
        label="Adresse email (sert à se connecter)"
        class="mb-3"
      />
      <p v-else class="text-secondary account-edit__email">{{ account?.email }}</p>

      <div class="account-edit__label">Rôle</div>
      <div class="account-edit__roles">
        <button
          v-for="role in roles"
          :key="role"
          type="button"
          class="account-edit__role"
          :class="{ 'account-edit__role--selected': draft.role === role }"
          @click="draft.role = role"
        >
          {{ accountRoleLabels[role] }}
        </button>
      </div>

      <template v-if="needsPassword">
        <AppTextField
          v-model="draft.password"
          type="password"
          autocomplete="new-password"
          :label="isCreation ? 'Mot de passe' : 'Nouveau mot de passe administrateur'"
          class="mt-3"
        />
        <p class="text-secondary account-edit__hint">
          <template v-if="isPromotion">Devenir administrateur exige un mot de passe plus long. </template>
          {{ passwordRuleText(draft.role) }}
        </p>
      </template>

      <p v-if="error" class="account-edit__error text-error">{{ error }}</p>

      <div class="account-edit__actions">
        <v-btn variant="text" color="secondary" @click="close">Annuler</v-btn>
        <v-btn color="primary" variant="flat" :disabled="!canSave" :loading="submitting" @click="save">
          {{ isCreation ? 'Créer le compte' : 'Enregistrer' }}
        </v-btn>
      </div>
    </v-card>
  </v-dialog>
</template>

<style scoped>
.account-edit__dialog {
  padding: 20px;
  border-radius: 16px;
}

.account-edit__email {
  font-size: 15px;
  font-weight: 500;
  margin: 0 0 12px;
}

.account-edit__label {
  font-size: 15px;
  font-weight: 500;
  margin-bottom: 6px;
}

.account-edit__roles {
  display: flex;
  gap: 10px;
}

.account-edit__role {
  flex: 1;
  min-height: 48px;
  border: 2px solid rgb(var(--v-theme-field-border));
  background: rgb(var(--v-theme-field-surface));
  color: rgb(var(--v-theme-on-surface));
  font-family: var(--font-body);
  font-size: 16px;
  font-weight: 500;
  border-radius: 12px;
  cursor: pointer;
}

.account-edit__role--selected {
  border-color: rgb(var(--v-theme-primary));
  background: rgb(var(--v-theme-primary));
  color: rgb(var(--v-theme-surface));
}

.account-edit__hint {
  font-size: 14px;
  font-weight: 500;
  margin: 8px 0 0;
  overflow-wrap: anywhere;
}

.account-edit__error {
  font-size: 14px;
  font-weight: 500;
  margin: 12px 0 0;
}

.account-edit__actions {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
  margin-top: 20px;
}
</style>
