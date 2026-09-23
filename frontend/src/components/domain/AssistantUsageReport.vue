<script setup lang="ts">
import { watch } from 'vue'
import AppBadge from '@/components/base/AppBadge.vue'
import AppSortableTable from '@/components/base/AppSortableTable.vue'
import { getAssistantRequests, getAssistantUsage } from '@/api/reports'
import type { AssistantRequestDto, AssistantUsageDto } from '@/api/types'
import { useAsyncData } from '@/composables/useAsyncData'
import {
  formatDuration,
  inputModeLabels,
  outcomeLabels,
  outcomeTones,
  requestTime,
  weekLabel,
} from '@/composables/useAssistantReport'
import type { TableColumn } from '@/composables/useTableSort'

/**
 * Usage de l'assistant vocal, section de l'écran Rapports (RF-36, FR-025) : l'adoption et le délai par
 * compte et par semaine, puis les dernières demandes avec ce qui a été entendu, pour comprendre un raté.
 * La durée est celle du traitement par le serveur ; le délai ressenti y ajoute l'envoi de l'audio.
 */
const props = defineProps<{ from: string; to: string }>()

const { data: usage, error: usageError, reload: reloadUsage } = useAsyncData(
  () => getAssistantUsage(props.from, props.to),
  [] as AssistantUsageDto[],
)
const { data: requests, error: requestsError, reload: reloadRequests } = useAsyncData(
  () => getAssistantRequests(props.from, props.to),
  [] as AssistantRequestDto[],
)

watch(
  () => [props.from, props.to],
  () => {
    reloadUsage()
    reloadRequests()
  },
)

const usageColumns: TableColumn<never>[] = [
  { id: 'week', label: 'Semaine' },
  { id: 'account', label: 'Compte' },
  { id: 'requests', label: 'Demandes', numeric: true },
  { id: 'stock', label: 'Stock', numeric: true },
  { id: 'sale', label: 'Vente', numeric: true },
  { id: 'misunderstood', label: 'Pas compris', numeric: true },
  { id: 'errors', label: 'Erreurs', numeric: true },
  { id: 'duration', label: 'Durée médiane', numeric: true },
]
</script>

<template>
  <section class="assistant-usage">
    <div class="assistant-usage__head">
      <h2 class="assistant-usage__title">Assistant vocal</h2>
      <span class="text-secondary">Durée de traitement par le serveur, sans l'envoi de l'audio</span>
    </div>

    <p v-if="usageError" class="text-error">{{ usageError }}</p>
    <p v-else-if="usage.length === 0" class="text-secondary assistant-usage__empty">
      Aucune demande à l'assistant sur cette période.
    </p>
    <AppSortableTable
      v-else
      :columns="usageColumns"
      :rows="usage"
      :row-key="(u: AssistantUsageDto) => `${u.accountId}-${u.weekStart}`"
    >
      <template #row="{ row: u }">
        <td class="app-table__cell--strong">{{ weekLabel(u.weekStart) }}</td>
        <td>{{ u.accountName }}</td>
        <td class="app-table__cell--numeric app-table__cell--amount">{{ u.requests }}</td>
        <td class="app-table__cell--numeric">{{ u.stockAnswers }}</td>
        <td class="app-table__cell--numeric">{{ u.saleDrafts }}</td>
        <td class="app-table__cell--numeric">
          <AppBadge v-if="u.notUnderstood > 0" tone="warning">{{ u.notUnderstood }}</AppBadge>
          <span v-else class="text-secondary">—</span>
        </td>
        <td class="app-table__cell--numeric">
          <AppBadge v-if="u.errors + u.rateLimited > 0" tone="error">{{ u.errors + u.rateLimited }}</AppBadge>
          <span v-else class="text-secondary">—</span>
        </td>
        <td class="app-table__cell--numeric">{{ formatDuration(u.medianDurationMs) }}</td>
      </template>
    </AppSortableTable>

    <h3 class="assistant-usage__subtitle">Dernières demandes</h3>
    <p v-if="requestsError" class="text-error">{{ requestsError }}</p>
    <p v-else-if="requests.length === 0" class="text-secondary assistant-usage__empty">Aucune demande.</p>
    <ul v-else class="assistant-usage__requests">
      <li v-for="request in requests" :key="request.id" class="assistant-usage__request">
        <div class="assistant-usage__request-head">
          <AppBadge :tone="outcomeTones[request.outcome]">{{ outcomeLabels[request.outcome] }}</AppBadge>
          <span class="text-secondary">
            {{ requestTime(request.occurredAt) }} · {{ request.accountName }} · {{ inputModeLabels[request.inputMode] }}
            · {{ formatDuration(request.durationMs) }}
          </span>
        </div>
        <div class="assistant-usage__heard">
          <template v-if="request.heardText">« {{ request.heardText }} »</template>
          <span v-else class="text-secondary">Rien d'entendu</span>
        </div>
        <div v-if="request.replySpeech" class="text-secondary">{{ request.replySpeech }}</div>
      </li>
    </ul>
  </section>
</template>

<style scoped>
.assistant-usage {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.assistant-usage__head {
  display: flex;
  flex-wrap: wrap;
  align-items: baseline;
  gap: 4px 16px;
}

.assistant-usage__title {
  font-family: var(--font-heading);
  font-size: 24px;
  font-weight: 700;
  margin: 0;
  padding: 0 4px;
}

.assistant-usage__subtitle {
  font-size: 17px;
  font-weight: 600;
  margin: 8px 0 0;
}

.assistant-usage__empty {
  margin: 0;
}

.assistant-usage__requests {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.assistant-usage__request {
  background: rgb(var(--v-theme-surface));
  border-radius: 12px;
  padding: 12px 14px;
  display: flex;
  flex-direction: column;
  gap: 4px;
  font-size: 15px;
}

.assistant-usage__request-head {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 6px 10px;
}

.assistant-usage__heard {
  font-weight: 500;
}
</style>
