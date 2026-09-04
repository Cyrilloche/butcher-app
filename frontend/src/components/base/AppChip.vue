<!-- Pastille sélectionnable (sélecteur produit, filtres...) -->
<script setup lang="ts">
defineProps<{ selected?: boolean }>()
defineEmits<{ click: [] }>()
</script>

<template>
  <button
    type="button"
    class="app-chip"
    :class="{ 'app-chip--selected': selected }"
    @click="$emit('click')"
  >
    <slot />
  </button>
</template>

<style scoped>
.app-chip {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  border: 2px solid rgb(var(--v-theme-field-border));
  background: rgb(var(--v-theme-field-surface));
  color: rgb(var(--v-theme-on-surface));
  font-family: var(--font-body);
  font-size: 17px;
  font-weight: 500;
  padding: 12px 16px;
  border-radius: 12px;
  cursor: pointer;
  min-height: 48px;
}

.app-chip:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.app-chip--selected {
  border-color: rgb(var(--v-theme-primary));
  background: rgb(var(--v-theme-primary));
  color: rgb(var(--v-theme-surface));
}
</style>
