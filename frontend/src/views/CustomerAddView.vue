<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import AppPageHeader from '@/components/base/AppPageHeader.vue'
import AppCard from '@/components/base/AppCard.vue'
import AppTextField from '@/components/base/AppTextField.vue'
import AppButton from '@/components/base/AppButton.vue'
import { createCustomer } from '@/api/customers'
import { ApiError } from '@/api/http'

const router = useRouter()

const state = reactive({ firstName: '', lastName: '', phone: '', notes: '' })
const canSave = computed(() => state.lastName.trim().length > 1)
const saving = ref(false)
const saveError = ref<string | null>(null)

async function save() {
  if (!canSave.value) return
  saving.value = true
  saveError.value = null
  try {
    await createCustomer({
      lastName: state.lastName.trim(),
      firstName: state.firstName.trim() || undefined,
      phone: state.phone.trim() || undefined,
      notes: state.notes.trim() || undefined,
    })
    await router.push('/customers')
  } catch (err) {
    saveError.value = err instanceof ApiError ? err.message : "Erreur lors de l'enregistrement, réessaie."
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <v-container class="customer-add-view">
    <AppPageHeader to="/customers" back-label="Clients" title="Nouveau client" />

    <AppCard class="customer-add-view__card">
      <AppTextField v-model="state.firstName" label="Prénom" placeholder="Ex. Marie" />
      <AppTextField v-model="state.lastName" label="Nom" placeholder="Ex. Perrin" />
      <AppTextField v-model="state.phone" type="tel" inputmode="tel" label="Téléphone" placeholder="06 00 00 00 00" />
      <div class="customer-add-view__notes">
        <label class="customer-add-view__notes-label">Notes</label>
        <textarea
          v-model="state.notes"
          rows="4"
          placeholder="Préférences, allergies, habitudes de commande…"
          class="customer-add-view__textarea"
        />
      </div>
    </AppCard>

    <div class="customer-add-view__footer">
      <p v-if="saveError" class="customer-add-view__save-error text-error">{{ saveError }}</p>
      <AppButton block height="60" :color="canSave ? 'primary' : undefined" :disabled="!canSave || saving" @click="save">
        {{ saving ? 'Création...' : 'Créer le client' }}
      </AppButton>
    </div>
  </v-container>
</template>

<style scoped>
.customer-add-view {
  padding-bottom: 110px;
}

.customer-add-view__card {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.customer-add-view__notes {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.customer-add-view__notes-label {
  font-size: 15px;
  font-weight: 500;
}

.customer-add-view__textarea {
  border: 1.5px solid rgb(var(--v-theme-field-border));
  border-radius: 10px;
  background: rgb(var(--v-theme-field-surface));
  padding: 12px 14px;
  font-family: var(--font-body);
  font-size: 17px;
  color: rgb(var(--v-theme-on-surface));
  resize: none;
  line-height: 1.4;
}

.customer-add-view__save-error {
  font-size: 14px;
  font-weight: 500;
  text-align: center;
  margin: 0 0 10px;
}

.customer-add-view__footer {
  position: fixed;
  left: 0;
  right: 0;
  bottom: 56px;
  padding: 14px 16px;
  background: linear-gradient(to top, rgb(var(--v-theme-background)) 70%, transparent);
}
</style>
