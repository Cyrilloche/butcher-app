<!--
  Wrapper Kraft de v-btn : hauteur alignée par défaut sur celle d'un
  AppTextField (density "comfortable" = 56px) pour que les deux s'alignent
  quand ils sont côte à côte. Personnalisable par attrs (color, height,
  block...) sans avoir à redéfinir le reste à chaque usage.
-->
<template>
  <v-btn height="52" rounded="lg" color="secondary" v-bind="$attrs">
    <slot />
  </v-btn>
</template>
