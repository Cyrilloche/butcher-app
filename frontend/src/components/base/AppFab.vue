<script setup lang="ts">
import type { RouteLocationRaw } from 'vue-router'

withDefaults(
  defineProps<{
    icon?: string
    ariaLabel: string
    to?: RouteLocationRaw
  }>(),
  { icon: 'plus' },
)

defineEmits<{ click: [] }>()
</script>

<template>
  <RouterLink v-if="to" :to="to" class="app-fab" :aria-label="ariaLabel">
    <v-icon size="28">phosphor:{{ icon }}</v-icon>
  </RouterLink>
  <button v-else type="button" class="app-fab" :aria-label="ariaLabel" @click="$emit('click')">
    <v-icon size="28">phosphor:{{ icon }}</v-icon>
  </button>
</template>

<style scoped>
.app-fab {
  position: fixed;
  /* Au-dessus de la barre du bas sur mobile, dans le coin sur écran large : Vuetify publie la place
     prise par la mise en page dans ces variables, sur v-main. */
  right: calc(var(--v-layout-right, 0px) + 20px);
  bottom: calc(var(--v-layout-bottom, 0px) + 32px);
  width: 64px;
  height: 64px;
  border: none;
  border-radius: 50%;
  background: rgb(var(--v-theme-primary));
  color: rgb(var(--v-theme-surface));
  box-shadow: 0 4px 12px rgba(43, 36, 30, 0.28);
  display: flex;
  align-items: center;
  justify-content: center;
  text-decoration: none;
  cursor: pointer;
  z-index: 2;
}

.app-fab:hover {
  background: rgb(var(--v-theme-primary-darken-1));
}
</style>
