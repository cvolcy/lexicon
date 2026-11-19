<script setup lang="ts">
import type { BadgeProps } from '@nuxt/ui';
import type { HealthStatus } from '@/api/health';
import { useHealth } from '@/composables/useHealth';
import { formatDateTime } from '@/utils/format';

const { health, error, loading, refresh } = useHealth();

const statusColors: Record<HealthStatus, BadgeProps['color']> = {
  Healthy: 'success',
  Degraded: 'warning',
  Unhealthy: 'error',
};
</script>

<template>
  <UCard>
    <template #header>
      <div class="flex items-center justify-between">
        <h1 class="text-lg font-semibold">System Status</h1>
        <UButton
          icon="i-lucide-refresh-cw"
          color="neutral"
          variant="outline"
          size="sm"
          :loading="loading"
          @click="refresh"
        >
          {{ loading ? 'Checking...' : 'Refresh' }}
        </UButton>
      </div>
    </template>

    <UAlert v-if="error" color="error" variant="subtle" icon="i-lucide-circle-alert" :title="error" />
    <div v-else-if="health" class="space-y-6">
      <dl class="grid grid-cols-1 gap-4 sm:grid-cols-3">
        <div>
          <dt class="text-xs uppercase tracking-wide text-muted">Status</dt>
          <dd class="mt-1">
            <UBadge
              :label="health.status"
              :color="statusColors[health.status]"
              variant="subtle"
              size="sm"
            />
          </dd>
        </div>
        <div>
          <dt class="text-xs uppercase tracking-wide text-muted">Environment</dt>
          <dd class="mt-1 font-medium">{{ health.environment }}</dd>
        </div>
        <div>
          <dt class="text-xs uppercase tracking-wide text-muted">Timestamp</dt>
          <dd class="mt-1 font-medium">{{ formatDateTime(health.timestampUtc) }}</dd>
        </div>
      </dl>

      <div class="border-t border-default pt-4">
        <h2 class="mb-3 text-xs font-semibold uppercase tracking-wide text-muted">Checks</h2>
        <ul class="divide-y divide-default">
          <li
            v-for="check in health.checks"
            :key="check.name"
            class="flex items-center justify-between gap-4 py-2.5"
          >
            <div class="min-w-0">
              <div class="font-medium capitalize">{{ check.name }}</div>
              <div v-if="check.description" class="text-xs text-muted">{{ check.description }}</div>
            </div>
            <div class="flex shrink-0 items-center gap-3">
              <span class="text-xs text-muted">{{ check.durationMs }} ms</span>
              <UBadge
                :label="check.status"
                :color="statusColors[check.status]"
                variant="subtle"
                size="sm"
              />
            </div>
          </li>
        </ul>
      </div>
    </div>
    <p v-else class="text-sm text-muted">Checking...</p>
  </UCard>
</template>
