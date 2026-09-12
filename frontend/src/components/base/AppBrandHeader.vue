<!--
  Bandeau de marque affiché en tête des vues Dashboard (Stock, Produits...),
  pas sur les sous-pages (qui utilisent AppPageHeader). Logo et nom sont
  volontairement fixes tant qu'aucune identité visuelle définitive n'existe :
  "Saloir" est le nom d'app déjà retenu (CLAUDE.md §4, manifest PWA).

  Le bandeau est en "surface" (clair) sur le fond kraft de la page : ce
  contraste sépare visuellement l'identité de l'app du contenu métier, et le
  rend lisible même quand la liste défile dessous (position sticky).

  Mobile uniquement : sur écran large, la barre latérale d'AppLayout porte déjà
  la marque, la date et le menu du compte.
-->
<script setup lang="ts">
import { computed } from 'vue'
import { useDisplay } from 'vuetify'
import AccountMenu from '@/components/domain/AccountMenu.vue'
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()
const { mdAndUp } = useDisplay()

const now = new Date()
const todayWeekday = computed(() => now.toLocaleDateString('fr-FR', { weekday: 'long' }))
const todayDate = computed(() =>
  now.toLocaleDateString('fr-FR', { day: 'numeric', month: 'long', year: 'numeric' }),
)

const initial = computed(() => (auth.account?.displayName.trim().charAt(0) ?? '?').toUpperCase())
</script>

<template>
  <div v-if="!mdAndUp" class="app-brand-header">
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

      <AccountMenu>
        <template #activator="{ props: menuProps }">
          <button
            type="button"
            class="app-brand-header__account"
            :aria-label="`Menu du compte ${auth.account?.displayName ?? ''}`"
            :title="auth.account?.displayName"
            v-bind="menuProps"
          >
            {{ initial }}
          </button>
        </template>
      </AccountMenu>
    </div>
  </div>
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

/* Pastille du compte : initiale sur fond succès, comme dans la maquette PC. */
.app-brand-header__account {
  width: 42px;
  height: 42px;
  flex-shrink: 0;
  border-radius: 50%;
  border: none;
  background: rgb(var(--v-theme-success));
  color: rgb(var(--v-theme-surface));
  font-family: var(--font-heading);
  font-weight: 700;
  font-size: 18px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
}

.app-brand-header__account:hover {
  filter: brightness(0.92);
}
</style>
