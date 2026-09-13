<script setup lang="ts">
import { computed, ref } from 'vue'
import AppPageHeader from '@/components/base/AppPageHeader.vue'
import AppButton from '@/components/base/AppButton.vue'
import AccountEditDialog from '@/components/domain/AccountEditDialog.vue'
import AccountPasswordResetDialog from '@/components/domain/AccountPasswordResetDialog.vue'
import { deactivateAccount, listAccounts, reactivateAccount } from '@/api/accounts'
import { useAsyncData } from '@/composables/useAsyncData'
import { apiErrorMessage } from '@/composables/useApiError'
import { accountRoleLabels, formatLastLogin } from '@/composables/useAccounts'
import { useAuthStore } from '@/stores/auth'
import type { AccountDto } from '@/api/types'

/**
 * Gestion des comptes, réservée à l'administrateur (ADR-011, FR-003). Un compte ne se supprime pas :
 * il se désactive, et reste l'auteur de ce qu'il a saisi (FR-009). Les refus du serveur — dernier
 * administrateur, son propre compte, email déjà pris — sont affichés tels quels.
 */
const auth = useAuthStore()
const { data: accounts, loading, error, reload } = useAsyncData(listAccounts, [] as AccountDto[])

const actionError = ref<string | null>(null)

// --- Création et modification ------------------------------------------------------------------

const editOpen = ref(false)
const editedAccount = ref<AccountDto | null>(null)

function openCreate() {
  editedAccount.value = null
  editOpen.value = true
}

function openEdit(account: AccountDto) {
  editedAccount.value = account
  editOpen.value = true
}

async function onSaved() {
  actionError.value = null
  await reload()
  // Son propre nom a pu changer : le bandeau et les salutations le relisent.
  if (editedAccount.value?.id === auth.account?.id) await auth.loadAccount()
}

// --- Mot de passe --------------------------------------------------------------------------------

const resetOpen = ref(false)
const resetAccount = ref<AccountDto | null>(null)

function openReset(account: AccountDto) {
  resetAccount.value = account
  resetOpen.value = true
}

// --- Désactivation et réactivation ---------------------------------------------------------------

const deactivating = ref<AccountDto | null>(null)
const confirmDeactivation = computed({
  get: () => deactivating.value !== null,
  set: (open: boolean) => {
    if (!open) deactivating.value = null
  },
})
const submitting = ref(false)

async function deactivate() {
  if (!deactivating.value) return
  submitting.value = true
  actionError.value = null
  try {
    await deactivateAccount(deactivating.value.id)
    await reload()
  } catch (err) {
    actionError.value = apiErrorMessage(err, 'Désactivation impossible, réessaie.')
  } finally {
    submitting.value = false
    deactivating.value = null
  }
}

async function reactivate(account: AccountDto) {
  actionError.value = null
  try {
    await reactivateAccount(account.id)
    await reload()
  } catch (err) {
    actionError.value = apiErrorMessage(err, 'Réactivation impossible, réessaie.')
  }
}

function isSelf(account: AccountDto) {
  return account.id === auth.account?.id
}
</script>

<template>
  <v-container class="accounts-view app-page-container">
    <AppPageHeader to="/" back-label="Stock" title="Comptes" subtitle="Qui peut se connecter à Saloir" />

    <AppButton block height="52" color="primary" class="mb-4" @click="openCreate">
      <v-icon start size="18">phosphor:plus</v-icon>
      Nouveau compte
    </AppButton>

    <p v-if="actionError" class="accounts-view__error text-error">{{ actionError }}</p>
    <p v-if="loading" class="text-secondary">Chargement...</p>
    <p v-else-if="error" class="text-error">{{ error }}</p>

    <div v-else class="accounts-view__list">
      <article
        v-for="account in accounts"
        :key="account.id"
        class="accounts-view__card"
        :class="{ 'accounts-view__card--inactive': !account.isActive }"
      >
        <div class="accounts-view__identity">
          <div class="accounts-view__name">
            {{ account.displayName }}
            <span v-if="isSelf(account)" class="text-secondary accounts-view__self">(vous)</span>
          </div>
          <div class="text-secondary accounts-view__email">{{ account.email }}</div>
          <div class="accounts-view__badges">
            <span class="accounts-view__badge" :class="`accounts-view__badge--${account.role}`">
              {{ accountRoleLabels[account.role] }}
            </span>
            <span v-if="!account.isActive" class="accounts-view__badge accounts-view__badge--inactive">
              Désactivé
            </span>
          </div>
          <div class="text-secondary accounts-view__last-login">{{ formatLastLogin(account.lastLoginAt) }}</div>
        </div>

        <v-menu location="bottom end">
          <template #activator="{ props: menuProps }">
            <button
              type="button"
              class="accounts-view__menu"
              :aria-label="`Actions pour ${account.displayName}`"
              v-bind="menuProps"
            >
              <v-icon size="20">phosphor:dots-three-vertical</v-icon>
            </button>
          </template>
          <v-list density="comfortable">
            <v-list-item title="Modifier" @click="openEdit(account)" />
            <v-list-item title="Nouveau mot de passe" @click="openReset(account)" />
            <v-list-item
              v-if="account.isActive && !isSelf(account)"
              title="Désactiver"
              base-color="error"
              @click="deactivating = account"
            />
            <v-list-item v-if="!account.isActive" title="Réactiver" @click="reactivate(account)" />
          </v-list>
        </v-menu>
      </article>
    </div>

    <AccountEditDialog v-model="editOpen" :account="editedAccount" @saved="onSaved" />
    <AccountPasswordResetDialog v-model="resetOpen" :account="resetAccount" @done="reload" />

    <v-dialog v-model="confirmDeactivation" max-width="380">
      <v-card v-if="deactivating" class="accounts-view__dialog">
        <h2 class="text-h6 font-weight-bold mb-2">Désactiver {{ deactivating.displayName }} ?</h2>
        <p class="text-secondary mb-5">
          {{ deactivating.displayName }} ne pourra plus se connecter et sera déconnecté de ses appareils.
          Ses saisies restent à son nom. Le compte peut être réactivé plus tard.
        </p>
        <div class="accounts-view__dialog-actions">
          <v-btn variant="text" color="secondary" @click="deactivating = null">Annuler</v-btn>
          <v-btn color="error" variant="flat" :loading="submitting" @click="deactivate">Désactiver</v-btn>
        </div>
      </v-card>
    </v-dialog>
  </v-container>
</template>

<style scoped>
.accounts-view {
  padding-bottom: 40px;
}

.accounts-view__error {
  font-size: 14px;
  font-weight: 500;
  padding: 0 4px 12px;
}

.accounts-view__list {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.accounts-view__card {
  display: flex;
  align-items: flex-start;
  gap: 12px;
  background: rgb(var(--v-theme-surface));
  border-radius: 14px;
  padding: 14px 16px;
  box-shadow: 0 1px 2px rgba(43, 36, 30, 0.06);
}

.accounts-view__card--inactive {
  opacity: 0.7;
}

.accounts-view__identity {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 3px;
}

.accounts-view__name {
  font-family: var(--font-heading);
  font-weight: 600;
  font-size: 19px;
  line-height: 1.2;
}

.accounts-view__self {
  font-family: var(--font-body);
  font-size: 14px;
  font-weight: 500;
}

.accounts-view__email {
  font-size: 14px;
  overflow-wrap: anywhere;
}

.accounts-view__badges {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  margin-top: 2px;
}

.accounts-view__badge {
  font-size: 13px;
  font-weight: 600;
  padding: 2px 10px;
  border-radius: 999px;
}

.accounts-view__badge--admin {
  background: rgb(var(--v-theme-primary));
  color: rgb(var(--v-theme-surface));
}

.accounts-view__badge--user {
  background: rgb(var(--v-theme-status-neutral-container));
  color: rgb(var(--v-theme-secondary));
}

.accounts-view__badge--inactive {
  background: rgb(var(--v-theme-error-container));
  color: rgb(var(--v-theme-error));
}

.accounts-view__last-login {
  font-size: 13px;
}

.accounts-view__menu {
  width: 40px;
  height: 40px;
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  border: none;
  background: none;
  color: rgb(var(--v-theme-secondary));
  border-radius: 10px;
  cursor: pointer;
}

.accounts-view__menu:hover {
  background: rgb(var(--v-theme-status-neutral-container));
}

.accounts-view__dialog {
  padding: 20px;
  border-radius: 16px;
}

.accounts-view__dialog-actions {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
}
</style>
