<script setup lang="ts">
import type { RouteLocationRaw } from 'vue-router'

defineProps<{
  to: RouteLocationRaw
  backLabel: string
  title: string
  subtitle?: string
}>()
</script>

<template>
  <header class="app-page-header">
    <RouterLink :to="to" class="app-page-header__back text-secondary">
      <v-icon size="18">phosphor:caret-left</v-icon>
      {{ backLabel }}
    </RouterLink>
    <div class="app-page-header__title-row">
      <h1 class="app-page-header__title text-h4 font-weight-bold">{{ title }}</h1>
      <slot name="badge" />
    </div>
    <div v-if="subtitle" class="app-page-header__subtitle text-secondary font-weight-medium">{{ subtitle }}</div>
  </header>
</template>

<style scoped>
.app-page-header {
  display: flex;
  flex-direction: column;
  gap: 2px;
  padding: 8px 4px 12px;
}

.app-page-header__back {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  text-decoration: none;
  font-size: 16px;
  font-weight: 500;
  min-height: 44px;
}

.app-page-header__title-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.app-page-header__title {
  margin: 0;
  line-height: 1.1;
}

.app-page-header__subtitle {
  font-size: 16px;
}
</style>
