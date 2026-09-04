<script setup lang="ts">
import type { SaleMode } from '@/api/types'

defineProps<{ modelValue: SaleMode }>()
defineEmits<{ 'update:modelValue': [value: SaleMode] }>()
</script>

<template>
  <div class="sale-mode-toggle">
    <button
      type="button"
      class="sale-mode-toggle__option"
      :class="{ 'sale-mode-toggle__option--selected': modelValue === 'by_weight' }"
      @click="$emit('update:modelValue', 'by_weight')"
    >
      <v-icon size="26">phosphor:scales</v-icon>
      Au poids
    </button>
    <button
      type="button"
      class="sale-mode-toggle__option"
      :class="{ 'sale-mode-toggle__option--selected': modelValue === 'by_piece' }"
      @click="$emit('update:modelValue', 'by_piece')"
    >
      <v-icon size="26">phosphor:hand-coins</v-icon>
      À la pièce
    </button>
  </div>
</template>

<style scoped>
.sale-mode-toggle {
  display: flex;
  gap: 10px;
}

.sale-mode-toggle__option {
  flex: 1;
  border: 2px solid rgb(var(--v-theme-field-border));
  background: rgb(var(--v-theme-field-surface));
  color: rgb(var(--v-theme-on-surface));
  font-family: var(--font-body);
  font-size: 17px;
  font-weight: 500;
  padding: 14px 10px;
  border-radius: 12px;
  cursor: pointer;
  min-height: 76px;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 6px;
}

.sale-mode-toggle__option--selected {
  border-color: rgb(var(--v-theme-primary));
  background: rgb(var(--v-theme-primary));
  color: rgb(var(--v-theme-surface));
}
</style>
