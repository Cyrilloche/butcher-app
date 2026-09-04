<script setup lang="ts">
import type { CustomerDto } from '@/api/types'
import { customerFullName, customerInitials } from '@/composables/useCustomers'

defineProps<{ customer: CustomerDto }>()
</script>

<template>
  <RouterLink :to="`/customers/${customer.id}`" class="customer-row">
    <div class="customer-row__avatar">{{ customerInitials(customer) }}</div>
    <div class="customer-row__info">
      <div class="customer-row__name">{{ customerFullName(customer) }}</div>
      <div class="customer-row__phone text-secondary">{{ customer.phone || '—' }}</div>
    </div>
  </RouterLink>
</template>

<style scoped>
.customer-row {
  text-decoration: none;
  color: rgb(var(--v-theme-on-surface));
  background: rgb(var(--v-theme-surface));
  border-radius: 14px;
  padding: 14px 16px;
  display: flex;
  align-items: center;
  gap: 12px;
  box-shadow: 0 1px 2px rgba(43, 36, 30, 0.06);
  min-height: 48px;
}

.customer-row:hover {
  background: rgb(var(--v-theme-status-neutral-container));
}

.customer-row__avatar {
  width: 42px;
  height: 42px;
  border-radius: 50%;
  background: rgb(var(--v-theme-status-neutral-container));
  color: rgb(var(--v-theme-secondary));
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: 600;
  font-size: 16px;
  flex-shrink: 0;
}

.customer-row__info {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.customer-row__name {
  font-family: var(--font-heading);
  font-weight: 600;
  font-size: 19px;
  line-height: 1.2;
}

.customer-row__phone {
  font-size: 15px;
}
</style>
