<script setup lang="ts">
import { computed } from 'vue'

/**
 * Auteur d'un enregistrement (RF-27, FR-020a), visible par tous les comptes.
 *
 * `null` désigne un enregistrement antérieur aux comptes nominatifs : il est nommé comme tel plutôt
 * que laissé vide, pour qu'on ne le prenne pas pour un oubli (FR-026).
 */
const props = defineProps<{ name: string | null | undefined }>()

const label = computed(() =>
  props.name ? `Saisie par ${props.name}` : 'Compte partagé (avant comptes nominatifs)',
)
</script>

<template>
  <span class="author-label text-secondary">
    <v-icon size="14" class="author-label__icon">phosphor:user</v-icon>
    {{ label }}
  </span>
</template>

<style scoped>
.author-label {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  font-size: 13px;
  font-weight: 500;
  line-height: 1.3;
}

.author-label__icon {
  flex-shrink: 0;
}
</style>
