<script setup lang="ts">
import { computed } from 'vue'
import AppBadge from '@/components/base/AppBadge.vue'

const props = defineProps<{ isActive: boolean }>()

const config = computed(() =>
  props.isActive ? { label: 'Actif', tone: 'success' as const } : { label: 'Désactivé', tone: 'neutral' as const },
)
</script>

<template>
  <AppBadge :tone="config.tone">{{ config.label }}</AppBadge>
</template>
