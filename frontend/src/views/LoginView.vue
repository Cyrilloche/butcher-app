<!-- src/views/LoginView.vue -->
<script setup lang="ts">
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import AppCard from '@/components/base/AppCard.vue'
import AppTextField from '@/components/base/AppTextField.vue'
import AppButton from '@/components/base/AppButton.vue'
import { useAuthStore } from '@/stores/auth'
import { ApiError } from '@/api/http'

const auth = useAuthStore()
const router = useRouter()
const route = useRoute()

const email = ref('')
const password = ref('')
const errorMessage = ref<string | null>(null)
const submitting = ref(false)

async function submit() {
  errorMessage.value = null
  submitting.value = true
  try {
    await auth.login(email.value, password.value)
    const redirect = typeof route.query.redirect === 'string' ? route.query.redirect : '/'
    await router.push(redirect)
  } catch (err) {
    errorMessage.value =
      err instanceof ApiError && err.status === 401
        ? 'Identifiants incorrects.'
        : "Impossible de se connecter. Vérifie que l'API est bien lancée."
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <v-container class="login-view">
    <AppCard class="login-view__card">
      <h1 class="text-h4 font-weight-bold mb-4">Connexion</h1>

      <form class="login-view__form" @submit.prevent="submit">
        <AppTextField v-model="email" type="email" label="Email" autocomplete="username" />
        <AppTextField
          v-model="password"
          type="password"
          label="Mot de passe"
          autocomplete="current-password"
        />

        <div v-if="errorMessage" class="login-view__error text-error">{{ errorMessage }}</div>

        <AppButton type="submit" block height="56" color="primary" :disabled="submitting">
          {{ submitting ? 'Connexion...' : 'Se connecter' }}
        </AppButton>
      </form>
    </AppCard>
  </v-container>
</template>

<style scoped>
.login-view {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
}

.login-view__card {
  width: 100%;
  max-width: 380px;
}

.login-view__form {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.login-view__error {
  font-size: 14px;
  font-weight: 500;
}
</style>
