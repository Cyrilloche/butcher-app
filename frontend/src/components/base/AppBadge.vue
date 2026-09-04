<script setup lang="ts">
withDefaults(defineProps<{ tone?: 'success' | 'warning' | 'neutral' | 'error' }>(), {
  tone: 'neutral',
})
</script>

<template>
  <span class="app-badge" :class="`app-badge--${tone}`">
    <slot />
  </span>
</template>

<style scoped>
.app-badge {
  display: inline-flex;
  align-items: center;
  padding: 3px 10px;
  border-radius: 999px;
  font-size: 14px;
  font-weight: 600;
  white-space: nowrap;
}

.app-badge--success {
  background: rgb(var(--v-theme-success-container));
  color: rgb(var(--v-theme-success));
}

.app-badge--warning {
  background: rgb(var(--v-theme-warning-container));
  color: rgb(var(--v-theme-warning));
}

.app-badge--error {
  background: rgb(var(--v-theme-error-container));
  color: rgb(var(--v-theme-error));
}

.app-badge--neutral {
  background: rgb(var(--v-theme-status-neutral-container));
  color: rgb(var(--v-theme-status-neutral));
}
</style>
