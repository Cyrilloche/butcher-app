<script setup lang="ts">
import { computed } from 'vue'
import AppBadge from '@/components/base/AppBadge.vue'
import type { StockUnitStatus } from '@/composables/useStock'

const props = defineProps<{ status: StockUnitStatus }>()

// Table de correspondance code -> affichage FR + couleur sémantique.
// Fait foi : CLAUDE.md §12 ("Mapping statuts métier <-> couleurs sémantiques").
const config: Record<StockUnitStatus, { label: string; tone: 'success' | 'warning' | 'neutral' | 'error' }> = {
  available: { label: 'Disponible', tone: 'success' },
  opened: { label: 'Entamé', tone: 'warning' },
  sold: { label: 'Vendu', tone: 'neutral' },
  personal: { label: 'Perso', tone: 'neutral' },
  lost: { label: 'Perdu', tone: 'error' },
}

const current = computed(() => config[props.status])
</script>

<template>
  <AppBadge :tone="current.tone">{{ current.label }}</AppBadge>

</template>
