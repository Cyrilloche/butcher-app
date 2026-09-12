<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import AppPageHeader from '@/components/base/AppPageHeader.vue'
import AppCard from '@/components/base/AppCard.vue'
import AppButton from '@/components/base/AppButton.vue'
import AppTextField from '@/components/base/AppTextField.vue'
import { changePassword } from '@/api/auth'
import { apiErrorMessage } from '@/composables/useApiError'
import { accountRoleLabels, passwordRuleText } from '@/composables/useAccounts'
import { useAuthStore } from '@/stores/auth'

/**
 * Son propre compte : qui l'on est, et changer son mot de passe (FR-004). Les autres appareils sont
 * déconnectés par le serveur ; celui-ci reste connecté.
 */
const auth = useAuthStore()

const draft = reactive({ current: '', next: '', confirmation: '' })
const submitting = ref(false)
const error = ref<string | null>(null)
const done = ref(false)

const mismatch = computed(() => draft.confirmation.length > 0 && draft.next !== draft.confirmation)
const canSubmit = computed(
  () => draft.current.length > 0 && draft.next.length > 0 && draft.next === draft.confirmation,
)

async function submit() {
  if (!canSubmit.value) return
  submitting.value = true
  error.value = null
  done.value = false
  try {
    await changePassword({ currentPassword: draft.current, newPassword: draft.next })
    draft.current = ''
    draft.next = ''
    draft.confirmation = ''
    done.value = true
  } catch (err) {
    error.value = apiErrorMessage(err, 'Changement impossible, réessaie.')
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <v-container class="my-account-view">
    <AppPageHeader to="/" back-label="Stock" title="Mon compte" />

    <div v-if="auth.account" class="my-account-view__sections">
      <AppCard>
        <div class="my-account-view__name">{{ auth.account.displayName }}</div>
        <div class="text-secondary my-account-view__email">{{ auth.account.email }}</div>
        <div class="text-secondary my-account-view__role">{{ accountRoleLabels[auth.account.role] }}</div>
      </AppCard>

      <AppCard>
        <div class="my-account-view__section-title text-secondary">Changer mon mot de passe</div>

        <AppTextField
          v-model="draft.current"
          type="password"
          autocomplete="current-password"
          label="Mot de passe actuel"
          class="mb-3"
        />
        <AppTextField
          v-model="draft.next"
          type="password"
          autocomplete="new-password"
          label="Nouveau mot de passe"
          class="mb-3"
        />
        <AppTextField
          v-model="draft.confirmation"
          type="password"
          autocomplete="new-password"
          label="Nouveau mot de passe, une seconde fois"
        />
        <p class="text-secondary my-account-view__hint">{{ passwordRuleText(auth.account.role) }}</p>

        <p v-if="mismatch" class="my-account-view__error text-error">Les deux saisies ne sont pas identiques.</p>
        <p v-if="error" class="my-account-view__error text-error">{{ error }}</p>
        <p v-if="done" class="my-account-view__done">
          Mot de passe changé. Vos autres appareils ont été déconnectés.
        </p>

        <AppButton
          block
          height="52"
          color="primary"
          class="mt-4"
          :disabled="!canSubmit || submitting"
          @click="submit"
        >
          Changer le mot de passe
        </AppButton>
      </AppCard>
    </div>
  </v-container>
</template>

<style scoped>
.my-account-view {
  padding-bottom: 40px;
  max-width: 560px;
}

.my-account-view__sections {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.my-account-view__name {
  font-family: var(--font-heading);
  font-weight: 600;
  font-size: 22px;
}

.my-account-view__email {
  font-size: 15px;
  overflow-wrap: anywhere;
}

.my-account-view__role {
  font-size: 14px;
  font-weight: 600;
  margin-top: 4px;
}

.my-account-view__section-title {
  font-size: 16px;
  font-weight: 600;
  margin-bottom: 12px;
}

.my-account-view__hint {
  font-size: 14px;
  font-weight: 500;
  margin: 8px 0 0;
  overflow-wrap: anywhere;
}

.my-account-view__error {
  font-size: 14px;
  font-weight: 500;
  margin: 12px 0 0;
}

.my-account-view__done {
  font-size: 15px;
  font-weight: 600;
  color: rgb(var(--v-theme-success));
  margin: 12px 0 0;
}
</style>
