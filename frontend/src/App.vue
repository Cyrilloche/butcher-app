<script setup lang="ts">
import { RouterView } from 'vue-router'
import AppLayout from '@/layouts/AppLayout.vue'
</script>

<template>
  <v-app>
    <RouterView />
    <AppLayout />
  </v-app>
</template>