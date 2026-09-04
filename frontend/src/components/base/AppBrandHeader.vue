<!--
  Bandeau de marque affiché en tête des vues Dashboard (Stock, Produits...),
  pas sur les sous-pages (qui utilisent AppPageHeader). Logo et nom sont
  volontairement fixes tant qu'aucune identité visuelle définitive n'existe :
  "Saloir" est le nom d'app déjà retenu (CLAUDE.md §4, manifest PWA).

  Le bandeau est en "surface" (clair) sur le fond kraft de la page : ce
  contraste sépare visuellement l'identité de l'app du contenu métier, et le
  rend lisible même quand la liste défile dessous (position sticky).
-->
<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()
const router = useRouter()

const now = new Date()
const todayWeekday = computed(() => now.toLocaleDateString('fr-FR', { weekday: 'long' }))
const todayDate = computed(() =>
  now.toLocaleDateString('fr-FR', { day: 'numeric', month: 'long', year: 'numeric' }),
)

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
  <div class="app-brand-header">
    <div class="app-brand-header__identity">
      <div class="app-brand-header__logo">
        <v-icon size="22">phosphor:storefront</v-icon>
      </div>
      <div class="app-brand-header__names">
        <div class="app-brand-header__name">Saloir</div>
        <div class="app-brand-header__tagline">Charcuterie artisanale</div>
      </div>
    </div>

    <div class="app-brand-header__end">
      <div class="app-brand-header__today">
        <div class="app-brand-header__weekday">{{ todayWeekday }}</div>
        <div class="app-brand-header__date">{{ todayDate }}</div>
      </div>
      <button
        type="button"
        class="app-brand-header__logout"
        aria-label="Se déconnecter"
        title="Se déconnecter"
        @click="confirmOpen = true"
      >
        <v-icon size="22">phosphor:sign-out</v-icon>
      </button>
    </div>
  </div>

  <v-dialog v-model="confirmOpen" max-width="360">
    <v-card class="app-brand-header__dialog">
      <h2 class="text-h6 font-weight-bold mb-2">Se déconnecter ?</h2>
      <p class="text-secondary mb-5">Il faudra saisir à nouveau l'email et le mot de passe.</p>
      <div class="app-brand-header__dialog-actions">
        <v-btn variant="text" color="secondary" @click="confirmOpen = false">Annuler</v-btn>
        <v-btn color="primary" variant="flat" :loading="loggingOut" @click="confirmLogout">
          Se déconnecter
        </v-btn>
      </div>
    </v-card>
  </v-dialog>
</template>

<style scoped>
.app-brand-header {
  /* Pleine largeur : compense le padding horizontal du v-container parent. */
  margin: -16px -16px 12px;
  padding: 14px 20px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  background: rgb(var(--v-theme-surface));
  border-bottom: 1px solid rgb(var(--v-theme-field-border));
  border-radius: 0 0 18px 18px;
  box-shadow: 0 2px 8px rgba(43, 36, 30, 0.07);
  position: sticky;
  top: 0;
  z-index: 3;
}

.app-brand-header__identity {
  display: flex;
  align-items: center;
  gap: 10px;
  min-width: 0;
}

.app-brand-header__logo {
  width: 38px;
  height: 38px;
  border-radius: 50%;
  background: rgb(var(--v-theme-primary));
  color: rgb(var(--v-theme-surface));
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.app-brand-header__names {
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.app-brand-header__name {
  font-family: var(--font-heading);
  font-weight: 700;
  font-size: 18px;
  line-height: 1.15;
  white-space: nowrap;
}

.app-brand-header__tagline {
  font-size: 12px;
  color: rgb(var(--v-theme-secondary));
  font-weight: 500;
  letter-spacing: 0.04em;
  text-transform: uppercase;
}

.app-brand-header__end {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-shrink: 0;
}

.app-brand-header__today {
  text-align: right;
  display: flex;
  flex-direction: column;
  gap: 1px;
}

.app-brand-header__weekday {
  font-size: 13px;
  color: rgb(var(--v-theme-secondary));
  font-weight: 600;
  text-transform: capitalize;
}

.app-brand-header__date {
  font-size: 14px;
  color: rgb(var(--v-theme-on-surface));
  font-weight: 600;
}

/* Bouton de sortie : pastille kraft + icône terracotta, assez contrastée
   pour être repérable sans concurrencer le logo de gauche. */
.app-brand-header__logout {
  width: 42px;
  height: 42px;
  flex-shrink: 0;
  border-radius: 50%;
  border: 1px solid rgb(var(--v-theme-field-border));
  background: rgb(var(--v-theme-background));
  color: rgb(var(--v-theme-primary));
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
}

.app-brand-header__logout:hover {
  background: rgb(var(--v-theme-status-neutral-container));
  color: rgb(var(--v-theme-primary-darken-1));
}

.app-brand-header__dialog {
  padding: 20px;
  border-radius: 16px;
}

.app-brand-header__dialog-actions {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
}
</style>
