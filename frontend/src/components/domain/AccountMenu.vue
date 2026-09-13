<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

/**
 * Menu du compte connecté (ADR-011) : « Mon compte », « Comptes » pour l'administrateur,
 * « Se déconnecter ». Partagé par le bandeau mobile et la barre latérale PC, qui fournissent chacun
 * leur déclencheur via le slot `activator`.
 */
defineProps<{ location?: 'bottom end' | 'top end' | 'end bottom' }>()
defineSlots<{ activator(props: { props: Record<string, unknown> }): unknown }>()

const auth = useAuthStore()
const router = useRouter()

// Confirmation explicite : la déconnexion est irréversible côté session (le
// refresh token est révoqué) et les utilisateurs visés sont peu à l'aise avec
// le numérique — un appui accidentel ne doit pas les éjecter de l'app.
const confirmOpen = ref(false)
const loggingOut = ref(false)

async function confirmLogout() {
  loggingOut.value = true
  try {
    await auth.logout()
    await router.push({ name: 'login' })
  } finally {
    loggingOut.value = false
    confirmOpen.value = false
  }
}
</script>

<template>
  <v-menu :location="location ?? 'bottom end'">
    <template #activator="{ props: menuProps }">
      <slot name="activator" :props="menuProps" />
    </template>
    <v-list density="comfortable">
      <v-list-item
        v-if="auth.account"
        :title="auth.account.displayName"
        :subtitle="auth.account.email"
        class="account-menu__identity"
      />
      <v-divider />
      <v-list-item title="Mon compte" prepend-icon="phosphor:user" :to="{ name: 'my-account' }" />
      <v-list-item v-if="auth.isAdmin" title="Comptes" prepend-icon="phosphor:users" :to="{ name: 'accounts' }" />
      <v-list-item title="Se déconnecter" prepend-icon="phosphor:sign-out" @click="confirmOpen = true" />
    </v-list>
  </v-menu>

  <v-dialog v-model="confirmOpen">
    <v-card class="account-menu__dialog">
      <h2 class="text-h6 font-weight-bold mb-2">Se déconnecter ?</h2>
      <p v-if="auth.account" class="mb-2">Connecté en tant que {{ auth.account.displayName }}.</p>
      <p class="text-secondary mb-5">Il faudra saisir à nouveau l'email et le mot de passe.</p>
      <div class="account-menu__dialog-actions">
        <v-btn variant="text" color="secondary" @click="confirmOpen = false">Annuler</v-btn>
        <v-btn color="primary" variant="flat" :loading="loggingOut" @click="confirmLogout">
          Se déconnecter
        </v-btn>
      </div>
    </v-card>
  </v-dialog>
</template>

<style scoped>
.account-menu__identity {
  pointer-events: none;
}

.account-menu__dialog {
  padding: 20px;
  border-radius: 16px;
}

.account-menu__dialog-actions {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
}
</style>
