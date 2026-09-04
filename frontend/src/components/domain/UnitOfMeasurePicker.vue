<!--
  Sélecteur d'unité de vente (référentiel unit_of_measure), avec création
  rapide inline — nécessaire car il n'existe pas encore de vue dédiée pour
  gérer ce référentiel, et une base vide (aucune unité) bloquerait sinon
  totalement la création de produit.
-->
<script setup lang="ts">
import { ref } from 'vue'
import AppChip from '@/components/base/AppChip.vue'
import AppTextField from '@/components/base/AppTextField.vue'
import AppButton from '@/components/base/AppButton.vue'
import { listUnitsOfMeasure, createUnitOfMeasure } from '@/api/unitsOfMeasure'
import { useAsyncData } from '@/composables/useAsyncData'
import { ApiError } from '@/api/http'

defineProps<{ modelValue: number | null; disabled?: boolean }>()
const emit = defineEmits<{ 'update:modelValue': [value: number | null] }>()

const { data: units, loading, error, reload } = useAsyncData(() => listUnitsOfMeasure(false), [])

const creating = ref(false)
const newLabel = ref('')
const newAbbreviation = ref('')
const saving = ref(false)
const createError = ref<string | null>(null)

async function submitNew() {
  if (!newLabel.value.trim() || !newAbbreviation.value.trim()) return
  saving.value = true
  createError.value = null
  try {
    const unit = await createUnitOfMeasure({
      label: newLabel.value.trim(),
      abbreviation: newAbbreviation.value.trim(),
    })
    await reload()
    emit('update:modelValue', unit.id)
    creating.value = false
    newLabel.value = ''
    newAbbreviation.value = ''
  } catch (err) {
    createError.value = err instanceof ApiError ? err.message : 'Erreur, réessaie.'
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <div class="unit-of-measure-picker">
    <p v-if="loading" class="text-secondary mb-0">Chargement des unités...</p>
    <p v-else-if="error" class="text-error mb-0">{{ error }}</p>
    <template v-else>
      <div class="unit-of-measure-picker__chips">
        <AppChip
          v-for="unit in units"
          :key="unit.id"
          :selected="unit.id === modelValue"
          :disabled="disabled"
          @click="emit('update:modelValue', unit.id)"
        >
          {{ unit.label }}
        </AppChip>
        <AppChip v-if="!disabled" :selected="creating" @click="creating = !creating">
          <v-icon size="16">phosphor:plus</v-icon>
          Nouvelle unité
        </AppChip>
      </div>

      <div v-if="creating" class="unit-of-measure-picker__form">
        <AppTextField v-model="newLabel" label="Nom" placeholder="Ex. kilogramme" />
        <AppTextField v-model="newAbbreviation" label="Abréviation" placeholder="Ex. kg" style="max-width: 140px" />
        <p v-if="createError" class="text-error mb-0">{{ createError }}</p>
        <AppButton
          height="48"
          color="secondary"
          :disabled="saving || !newLabel.trim() || !newAbbreviation.trim()"
          @click="submitNew"
        >
          {{ saving ? 'Création...' : 'Ajouter cette unité' }}
        </AppButton>
      </div>
    </template>
  </div>
</template>

<style scoped>
.unit-of-measure-picker__chips {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
}

.unit-of-measure-picker__form {
  display: flex;
  flex-direction: column;
  gap: 10px;
  margin-top: 12px;
  padding: 12px;
  background: rgb(var(--v-theme-status-neutral-container));
  border-radius: 10px;
}
</style>
