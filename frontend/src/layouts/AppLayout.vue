<script setup lang="ts">
import { computed } from 'vue'
import { RouterView, useRoute } from 'vue-router'
import { useDisplay } from 'vuetify'
import AccountMenu from '@/components/domain/AccountMenu.vue'
import { accountRoleLabels } from '@/composables/useAccounts'
import { useAuthStore } from '@/stores/auth'

/**
 * Gabarit de l'application : une seule application, deux présentations (FR-015, FR-016).
 * - Écran large (à partir de `md`, 840 px avec Vuetify 4) : barre latérale permanente, d'après la maquette
 *   `design/backoffice/Backoffice Overview.dc.html`.
 * - Téléphone et tablette portrait : barre de navigation en bas d'écran, inchangée.
 */
const route = useRoute()
const auth = useAuthStore()
const { mdAndUp } = useDisplay()

// La page de connexion est hors application : pas de navigation métier
// tant que la session n'est pas ouverte.
const showNavigation = computed(() => route.name !== 'login')

const now = new Date()
const todayWeekday = now.toLocaleDateString('fr-FR', { weekday: 'long' })
const todayDate = now.toLocaleDateString('fr-FR', { day: 'numeric', month: 'long', year: 'numeric' })

interface NavItem {
  to: string
  label: string
  icon: string
}

/**
 * Entrées de la barre latérale ; l'ordre suit la maquette PC. La vue d'ensemble, les rapports, le
 * journal et les comptes sont réservés à l'administrateur, comme leurs routes.
 */
const sidebarItems = computed<NavItem[]>(() => [
  ...(auth.isAdmin ? [{ to: '/overview', label: "Vue d'ensemble", icon: 'squares-four' }] : []),
  { to: '/sales', label: 'Ventes', icon: 'cash-register' },
  { to: '/', label: 'Stock', icon: 'package' },
  { to: '/products', label: 'Produits', icon: 'cooking-pot' },
  { to: '/customers', label: 'Clients', icon: 'users' },
  ...(auth.isAdmin
    ? [
        { to: '/reports', label: 'Rapports', icon: 'chart-bar' },
        { to: '/journal', label: 'Journal', icon: 'clock-counter-clockwise' },
        { to: '/accounts', label: 'Comptes', icon: 'user' },
      ]
    : []),
])

const initial = computed(() => (auth.account?.displayName.trim().charAt(0) ?? '?').toUpperCase())
</script>

<template>
  <v-navigation-drawer v-if="showNavigation && mdAndUp" permanent :width="248" class="app-sidebar">
    <div class="app-sidebar__inner">
      <div class="app-sidebar__brand">
        <div class="app-sidebar__logo">
          <v-icon size="24">phosphor:storefront</v-icon>
        </div>
        <div class="app-sidebar__names">
          <div class="app-sidebar__name">Saloir</div>
          <div class="app-sidebar__tagline">Charcuterie artisanale</div>
        </div>
      </div>

      <div class="app-sidebar__today">
        <div class="app-sidebar__weekday">{{ todayWeekday }}</div>
        <div class="app-sidebar__date">{{ todayDate }}</div>
      </div>

      <nav class="app-sidebar__nav" aria-label="Navigation principale">
        <RouterLink
          v-for="item in sidebarItems"
          :key="item.to"
          :to="item.to"
          class="app-sidebar__link"
          active-class="app-sidebar__link--active"
        >
          <v-icon size="22">phosphor:{{ item.icon }}</v-icon>
          <span>{{ item.label }}</span>
        </RouterLink>
      </nav>

      <div class="app-sidebar__spacer" />

      <AccountMenu location="top end">
        <template #activator="{ props: menuProps }">
          <button type="button" class="app-sidebar__account" v-bind="menuProps">
            <span class="app-sidebar__avatar">{{ initial }}</span>
            <span class="app-sidebar__account-names">
              <span class="app-sidebar__account-name">{{ auth.account?.displayName }}</span>
              <span v-if="auth.account" class="app-sidebar__account-role">
                {{ accountRoleLabels[auth.account.role] }}
              </span>
            </span>
          </button>
        </template>
      </AccountMenu>
    </div>
  </v-navigation-drawer>

  <v-main>
    <RouterView />
  </v-main>

  <v-bottom-navigation v-if="showNavigation && !mdAndUp" grow color="primary">
    <v-btn to="/" value="stock">
      <v-icon>phosphor:package</v-icon>
      Stock
    </v-btn>
    <v-btn to="/sales" value="sales">
      <v-icon>phosphor:cash-register</v-icon>
      Ventes
    </v-btn>
    <v-btn to="/customers" value="customers">
      <v-icon>phosphor:users</v-icon>
      Clients
    </v-btn>
    <v-btn to="/products" value="products">
      <v-icon>phosphor:cooking-pot</v-icon>
      Produits
    </v-btn>
  </v-bottom-navigation>
</template>

<style scoped>
.app-sidebar {
  background: rgb(var(--v-theme-surface));
  border-right: 1px solid rgb(var(--v-theme-field-border));
}

.app-sidebar__inner {
  display: flex;
  flex-direction: column;
  gap: 28px;
  min-height: 100%;
  padding: 28px 18px 24px;
}

.app-sidebar__brand {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 0 6px;
}

.app-sidebar__logo {
  width: 42px;
  height: 42px;
  border-radius: 50%;
  background: rgb(var(--v-theme-primary));
  color: rgb(var(--v-theme-surface));
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.app-sidebar__names {
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.app-sidebar__name {
  font-family: var(--font-heading);
  font-weight: 700;
  font-size: 19px;
  line-height: 1.15;
}

.app-sidebar__tagline {
  font-size: 11px;
  color: rgb(var(--v-theme-secondary));
  font-weight: 500;
  letter-spacing: 0.04em;
  text-transform: uppercase;
}

.app-sidebar__today {
  padding: 12px 14px;
  background: rgb(var(--v-theme-background));
  border-radius: 12px;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.app-sidebar__weekday {
  font-size: 13px;
  color: rgb(var(--v-theme-secondary));
  font-weight: 600;
  text-transform: capitalize;
}

.app-sidebar__date {
  font-family: var(--font-heading);
  font-size: 18px;
  font-weight: 700;
}

.app-sidebar__nav {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.app-sidebar__link {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 11px 14px;
  border-radius: 10px;
  text-decoration: none;
  color: rgb(var(--v-theme-secondary));
  font-size: 15px;
  font-weight: 500;
}

.app-sidebar__link:hover {
  background: rgb(var(--v-theme-status-neutral-container));
}

.app-sidebar__link--active {
  color: rgb(var(--v-theme-primary));
  background: rgb(var(--v-theme-status-neutral-container));
  font-weight: 600;
}

.app-sidebar__spacer {
  flex: 1;
}

.app-sidebar__account {
  display: flex;
  align-items: center;
  gap: 12px;
  width: 100%;
  padding: 10px 12px;
  border: none;
  border-top: 1px solid rgb(var(--v-theme-status-neutral-container));
  background: none;
  cursor: pointer;
  text-align: left;
  font-family: var(--font-body);
  color: rgb(var(--v-theme-on-surface));
  border-radius: 10px;
}

.app-sidebar__account:hover {
  background: rgb(var(--v-theme-status-neutral-container));
}

.app-sidebar__avatar {
  width: 36px;
  height: 36px;
  border-radius: 50%;
  background: rgb(var(--v-theme-success));
  color: rgb(var(--v-theme-surface));
  display: flex;
  align-items: center;
  justify-content: center;
  font-family: var(--font-heading);
  font-weight: 700;
  font-size: 15px;
  flex-shrink: 0;
}

.app-sidebar__account-names {
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.app-sidebar__account-name {
  font-size: 14px;
  font-weight: 600;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.app-sidebar__account-role {
  font-size: 12px;
  color: rgb(var(--v-theme-secondary));
}
</style>
