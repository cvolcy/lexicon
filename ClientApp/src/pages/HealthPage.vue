<script setup lang="ts">
import { computed } from 'vue';
import type { BadgeProps } from '@nuxt/ui';
import type { HealthStatus } from '@/api/health';
import { useHealth } from '@/composables/useHealth';
import { formatDateTime } from '@/utils/format';

const { health, error, loading, refresh } = useHealth();

const statusMeta: Record<
  HealthStatus,
  {
    color: BadgeProps['color'];
    headline: string;
    icon: string;
    tileClass: string;
  }
> = {
  Healthy: {
    color: 'success',
    headline: 'All systems operational',
    icon: 'i-lucide-check-circle-2',
    tileClass: 'bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 ring-emerald-500/20',
  },
  Degraded: {
    color: 'warning',
    headline: 'System degraded',
    icon: 'i-lucide-alert-triangle',
    tileClass: 'bg-amber-500/10 text-amber-600 dark:text-amber-400 ring-amber-500/20',
  },
  Unhealthy: {
    color: 'error',
    headline: 'Service disruption detected',
    icon: 'i-lucide-x-circle',
    tileClass: 'bg-rose-500/10 text-rose-600 dark:text-rose-400 ring-rose-500/20',
  },
};

const checkIcons: Record<string, string> = {
  database: 'i-lucide-database',
  storage: 'i-lucide-hard-drive',
};

const overall = computed(() => (health.value ? statusMeta[health.value.status] : null));
</script>

<template>
  <div class="space-y-6">
    <div class="flex flex-wrap items-center justify-between gap-4">
      <div>
        <h1 class="text-2xl font-semibold tracking-tight text-highlighted">System Health</h1>
        <p class="mt-1 text-sm text-muted">
          Real-time status of Lexicon's local database and storage providers.
        </p>
      </div>

      <UButton
        icon="i-lucide-refresh-cw"
        color="neutral"
        variant="outline"
        :loading="loading"
        @click="refresh"
      >
        {{ loading ? 'Checking...' : 'Re-run Checks' }}
      </UButton>
    </div>

    <UAlert v-if="error" color="error" variant="subtle" icon="i-lucide-circle-alert" :title="error" />

    <template v-else-if="loading && !health">
      <UCard>
        <div class="flex items-center gap-4">
          <USkeleton class="size-11 rounded-xl" />
          <div class="space-y-2">
            <USkeleton class="h-5 w-48" />
            <USkeleton class="h-3.5 w-64" />
          </div>
        </div>
      </UCard>
      <div class="grid grid-cols-1 gap-4 md:grid-cols-2">
        <UCard v-for="n in 2" :key="n">
          <div class="space-y-3">
            <USkeleton class="h-5 w-28" />
            <USkeleton class="h-4 w-52" />
          </div>
        </UCard>
      </div>
    </template>

    <template v-else-if="health && overall">
      <UCard>
        <div class="flex flex-col justify-between gap-4 sm:flex-row sm:items-center">
          <div class="flex items-center gap-4">
            <span
              class="flex size-11 shrink-0 items-center justify-center rounded-xl ring-1 ring-inset"
              :class="overall.tileClass"
            >
              <UIcon :name="overall.icon" class="size-6" />
            </span>

            <div>
              <div class="flex flex-wrap items-center gap-2">
                <h2 class="text-base font-semibold text-highlighted">{{ overall.headline }}</h2>
                <UBadge :label="health.status" :color="overall.color" variant="subtle" size="sm" />
              </div>
              <div class="mt-1 flex flex-wrap items-center gap-x-2 text-xs text-muted">
                <span>Environment: <strong class="font-medium text-default">{{ health.environment }}</strong></span>
                <span aria-hidden="true">·</span>
                <span class="font-mono">Checked {{ formatDateTime(health.timestampUtc) }}</span>
              </div>
            </div>
          </div>
        </div>
      </UCard>

      <div class="grid grid-cols-1 gap-4 md:grid-cols-2">
        <UCard v-for="check in health.checks" :key="check.name">
          <div class="flex items-start justify-between gap-4">
            <div class="flex items-start gap-3">
              <span
                class="flex size-10 shrink-0 items-center justify-center rounded-lg border border-default bg-elevated text-default"
              >
                <UIcon :name="checkIcons[check.name.toLowerCase()] ?? 'i-lucide-server'" class="size-5" />
              </span>
              <div class="min-w-0">
                <div class="flex items-center gap-2">
                  <h3 class="font-semibold capitalize text-highlighted">{{ check.name }}</h3>
                  <UBadge
                    :label="check.status"
                    :color="statusMeta[check.status].color"
                    variant="subtle"
                    size="sm"
                  />
                </div>
                <p v-if="check.description" class="mt-1 text-sm text-muted">
                  {{ check.description }}
                </p>
              </div>
            </div>

            <span class="shrink-0 rounded-md bg-elevated px-2 py-0.5 font-mono text-xs text-muted">
              {{ check.durationMs }} ms
            </span>
          </div>
        </UCard>
      </div>
    </template>
  </div>
</template>
