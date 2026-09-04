<!-- Bandeau icône + texte (fond neutre) — aperçu de numéro, indication de mode... -->
<script setup lang="ts">
defineProps<{ icon: string }>()
</script>

<template>
  <div class="app-hint-box">
    <v-icon size="20">phosphor:{{ icon }}</v-icon>
    <span><slot /></span>
  </div>
</template>

<style scoped>
.app-hint-box {
  display: flex;
  align-items: center;
  gap: 8px;
  background: rgb(var(--v-theme-status-neutral-container));
  border-radius: 10px;
  padding: 10px 14px;
  font-size: 15px;
  color: rgb(var(--v-theme-secondary));
}

.app-hint-box :deep(strong) {
  color: rgb(var(--v-theme-on-surface));
}
</style>
