<!--
  Bandeau de marque affiché en tête des vues Dashboard (Stock, Produits...),
  pas sur les sous-pages (qui utilisent AppPageHeader). Logo et nom sont
  volontairement fixes tant qu'aucune identité visuelle définitive n'existe :
  "Saloir" est le nom d'app déjà retenu (CLAUDE.md §4, manifest PWA).
-->
<script setup lang="ts">
import { computed } from 'vue'

const now = new Date()
const todayWeekday = computed(() => now.toLocaleDateString('fr-FR', { weekday: 'long' }))
const todayDate = computed(() =>
  now.toLocaleDateString('fr-FR', { day: 'numeric', month: 'long', year: 'numeric' }),
)
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
    <div class="app-brand-header__today">
      <div class="app-brand-header__weekday">{{ todayWeekday }}</div>
      <div class="app-brand-header__date">{{ todayDate }}</div>
    </div>
  </div>
</template>

<style scoped>
.app-brand-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
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

.app-brand-header__today {
  text-align: right;
  display: flex;
  flex-direction: column;
  gap: 1px;
  flex-shrink: 0;
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
</style>
