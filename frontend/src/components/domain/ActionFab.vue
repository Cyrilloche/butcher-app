<script setup lang="ts">
import { ref } from 'vue'
import { useRouter, type RouteLocationRaw } from 'vue-router'
import { useAssistant } from '@/composables/useAssistant'
import { useAuthStore } from '@/stores/auth'

/**
 * Le bouton « + » des listes, qui porte aussi l'assistant vocal (FR-001) : pour un compte où
 * l'administrateur l'a activé, un appui déplie deux choix étiquetés, l'action de l'écran
 * (« Nouvelle vente »…) et « Dicter ». Pour les autres comptes, un appui déclenche l'action, comme
 * `AppFab`, dont il reprend la place et l'aspect.
 */
const props = withDefaults(defineProps<{ icon?: string; label: string; to?: RouteLocationRaw }>(), { icon: 'plus' })
const emit = defineEmits<{ click: [] }>()

const router = useRouter()
const auth = useAuthStore()
const { start } = useAssistant()
const expanded = ref(false)

function onMainClick() {
  if (!auth.assistantEnabled) return runAction()
  expanded.value = !expanded.value
}

function runAction() {
  expanded.value = false
  if (props.to) router.push(props.to)
  else emit('click')
}

function dictate() {
  expanded.value = false
  void start()
}
</script>

<template>
  <div v-if="expanded" class="action-fab__scrim" @click="expanded = false" />
  <div class="action-fab">
    <div v-if="expanded" class="action-fab__choices">
      <button type="button" class="action-fab__choice" @click="dictate">
        <v-icon size="22">phosphor:microphone</v-icon>
        Dicter
      </button>
      <button type="button" class="action-fab__choice" @click="runAction">
        <v-icon size="22">phosphor:{{ icon }}</v-icon>
        {{ label }}
      </button>
    </div>
    <button
      type="button"
      class="action-fab__main"
      :class="{ 'action-fab__main--expanded': expanded }"
      :aria-label="!auth.assistantEnabled ? label : expanded ? 'Fermer' : `${label} ou dicter`"
      :aria-expanded="auth.assistantEnabled ? expanded : undefined"
      @click="onMainClick"
    >
      <v-icon size="28">phosphor:{{ icon }}</v-icon>
    </button>
  </div>
</template>

<style scoped>
.action-fab {
  position: fixed;
  /* Même place qu'AppFab : au-dessus de la barre du bas sur mobile, dans le coin sur écran large. */
  right: calc(var(--v-layout-right, 0px) + 20px);
  bottom: calc(var(--v-layout-bottom, 0px) + 32px);
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 12px;
  z-index: 3;
}

.action-fab__scrim {
  position: fixed;
  inset: 0;
  z-index: 2;
}

.action-fab__main {
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
  cursor: pointer;
  transition: transform 0.15s ease;
}

.action-fab__main:hover {
  background: rgb(var(--v-theme-primary-darken-1));
}

/* Le « + » tourne en « × » quand les choix sont ouverts. */
.action-fab__main--expanded {
  transform: rotate(45deg);
}

.action-fab__choices {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 10px;
}

.action-fab__choice {
  display: flex;
  align-items: center;
  gap: 10px;
  min-height: 48px;
  padding: 0 18px;
  border: none;
  border-radius: 24px;
  background: rgb(var(--v-theme-surface));
  color: rgb(var(--v-theme-on-surface));
  font: inherit;
  font-size: 16px;
  font-weight: 500;
  box-shadow: 0 2px 8px rgba(43, 36, 30, 0.22);
  cursor: pointer;
}
</style>
