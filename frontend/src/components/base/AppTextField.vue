<!--
  Wrapper Kraft de v-text-field.

  La maquette (Ajout Stock.dc.html) est un <input> simple : bordure plate,
  fond blanc, <label> statique au-dessus. Le variant "outlined" de Vuetify
  amène tout un système différent (label flottant dans une encoche de la
  bordure) qu'on ne veut pas ici. Plutôt que de le contourner à coups de
  CSS (source de bugs comme le "notch" resté visible), on utilise le
  variant "plain" (aucun chrome Vuetify) et on dessine nous-mêmes la boîte
  bordée, en contrôle total.
-->
<script setup lang="ts">
defineProps<{ label?: string }>()
</script>

<template>
  <div class="app-text-field">
    <label v-if="label" class="app-text-field__label">{{ label }}</label>
    <div class="app-text-field__box">
      <v-text-field variant="plain" density="compact" hide-details flat v-bind="$attrs" />
    </div>
  </div>
</template>

<style scoped>
.app-text-field {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.app-text-field__label {
  font-size: 15px;
  font-weight: 500;
  color: rgb(var(--v-theme-on-surface));
}

.app-text-field__box {
  height: 52px;
  border: 1.5px solid rgb(var(--v-theme-field-border));
  border-radius: 10px;
  background: rgb(var(--v-theme-field-surface));
  display: flex;
  align-items: center;
  padding: 0 14px;
}

.app-text-field__box:focus-within {
  border-color: rgb(var(--v-theme-primary));
}

.app-text-field__box :deep(.v-input),
.app-text-field__box :deep(.v-input__control),
.app-text-field__box :deep(.v-field),
.app-text-field__box :deep(.v-field__field),
.app-text-field__box :deep(.v-field__input) {
  height: 100%;
  width: 100%;
  min-height: 0;
  padding: 0;
  margin: 0;
  display: flex;
  align-items: center;
}

.app-text-field__box :deep(.v-field) {
  background: transparent;
  flex: 1;
}

.app-text-field__box :deep(.v-field__input) {
  font-size: 18px;
  font-family: var(--font-body);
  color: rgb(var(--v-theme-on-surface));
  line-height: normal;
}

.app-text-field__box :deep(input) {
  height: 100%;
  width: 100%;
  padding: 0;
}

.app-text-field__box :deep(input::placeholder) {
  text-align: right;
}

/* Vuetify masque prefix/suffix tant que le champ n'est pas "actif" (focus ou
   rempli) — pensé pour son système de label flottant qu'on n'utilise pas ici. */
.app-text-field__box :deep(.v-text-field__prefix),
.app-text-field__box :deep(.v-text-field__suffix) {
  opacity: 1;
  min-height: 0;
  padding-top: 0;
  padding-bottom: 0;
}

.app-text-field__box :deep(input:focus) {
  outline: none;
}
</style>
