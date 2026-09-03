<script setup lang="ts">
import { RouterView } from 'vue-router'
</script>

<template>
  <v-main>
    <RouterView />
  </v-main>

  <v-bottom-navigation grow color="primary">
    <v-btn to="/" value="stock">
      <v-icon>mdi-package-variant</v-icon>
      Stock
    </v-btn>
    <v-btn to="/sales" value="sales">
      <v-icon>mdi-cash-register</v-icon>
      Ventes
    </v-btn>
    <v-btn to="/customers" value="customers">
      <v-icon>mdi-account-group</v-icon>
      Clients
    </v-btn>
    <v-btn to="/products" value="products">
      <v-icon>mdi-food-drumstick</v-icon>
      Produits
    </v-btn>
  </v-bottom-navigation>
</template>