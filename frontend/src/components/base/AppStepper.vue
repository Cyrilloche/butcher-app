<script setup lang="ts">
withDefaults(defineProps<{ modelValue: number; min?: number }>(), { min: 0 })
defineEmits<{ 'update:modelValue': [value: number] }>()
</script>

<template>
  <div class="app-stepper">
    <button
      type="button"
      class="app-stepper__btn app-stepper__btn--minus"
      aria-label="Diminuer"
      :disabled="modelValue <= min"
      @click="$emit('update:modelValue', modelValue - 1)"
    >
      <v-icon size="22">phosphor:minus</v-icon>
    </button>
    <div class="app-stepper__value">{{ modelValue }}</div>
    <button
      type="button"
      class="app-stepper__btn app-stepper__btn--plus"
      aria-label="Augmenter"
      @click="$emit('update:modelValue', modelValue + 1)"
    >
      <v-icon size="22">phosphor:plus</v-icon>
    </button>
  </div>
</template>

<style scoped>
.app-stepper {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 20px;
}

.app-stepper__btn {
  width: 60px;
  height: 60px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
}

.app-stepper__btn--minus {
  border: 2px solid rgb(var(--v-theme-background));
  background: rgb(var(--v-theme-surface));
  color: rgb(var(--v-theme-on-surface));
}

.app-stepper__btn--minus:disabled {
  opacity: 0.4;
  cursor: not-allowed;
}

.app-stepper__btn--plus {
  border: none;
  background: rgb(var(--v-theme-secondary));
  color: rgb(var(--v-theme-surface));
}

.app-stepper__value {
  font-family: var(--font-heading);
  font-size: 44px;
  font-weight: 700;
  min-width: 80px;
  text-align: center;
}
</style>
