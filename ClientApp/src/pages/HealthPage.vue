<script setup lang="ts">
import { useHealth } from '@/composables/useHealth';

const { health, error, loading, refresh } = useHealth();
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
    <dl v-else-if="health" class="grid grid-cols-1 gap-4 sm:grid-cols-3">
      <div>
        <dt class="text-xs uppercase tracking-wide text-muted">Status</dt>
        <dd class="font-medium">{{ health.status }}</dd>
      </div>
      <div>
        <dt class="text-xs uppercase tracking-wide text-muted">Environment</dt>
        <dd class="font-medium">{{ health.environment }}</dd>
      </div>
      <div>
        <dt class="text-xs uppercase tracking-wide text-muted">Timestamp</dt>
        <dd class="font-medium">{{ new Date(health.timestampUtc).toLocaleString() }}</dd>
      </div>
    </dl>
    <p v-else class="text-sm text-muted">Checking...</p>
  </UCard>
</template>
