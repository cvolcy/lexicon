<script setup lang="ts">
import { computed } from 'vue';
import type { BadgeProps } from '@nuxt/ui';
import type { DocumentStatus } from '@/api/documents';
import type { HealthStatus } from '@/api/health';
import { useDocuments } from '@/composables/useDocuments';
import { useHealth } from '@/composables/useHealth';
import { formatBytes, formatDateTime } from '@/utils/format';

const { documents, loading: documentsLoading } = useDocuments();
const { health, loading: healthLoading } = useHealth();

const totalChunks = computed(() => documents.value.reduce((sum, d) => sum + d.chunkCount, 0));
const totalBytes = computed(() => documents.value.reduce((sum, d) => sum + d.sizeBytes, 0));
const readyCount = computed(() => documents.value.filter((d) => d.status === 'Ready').length);
const indexingCount = computed(
  () => documents.value.filter((d) => d.status === 'Pending' || d.status === 'Processing').length,
);

const recentDocuments = computed(() => documents.value.slice(0, 4));

const healthyChecksCount = computed(
  () => health.value?.checks.filter((c) => c.status === 'Healthy').length ?? 0,
);

const documentStatusColors: Record<DocumentStatus, BadgeProps['color']> = {
  Pending: 'neutral',
  Processing: 'info',
  Ready: 'success',
  Failed: 'error',
};

const healthStatusColors: Record<HealthStatus, BadgeProps['color']> = {
  Healthy: 'success',
  Degraded: 'warning',
  Unhealthy: 'error',
};

const pipelineStages: {
  name: string;
  description: string;
  status: string;
  color: BadgeProps['color'];
  icon: string;
}[] = [
  {
    name: 'Storage',
    description: 'Local disk file persistence',
    status: 'Active',
    color: 'success',
    icon: 'i-lucide-hard-drive',
  },
  {
    name: 'Ingestion',
    description: 'PDF, Markdown & text chunking',
    status: 'Active',
    color: 'success',
    icon: 'i-lucide-cpu',
  },
  {
    name: 'Retrieval & Chat',
    description: 'Ollama embeddings & cited Q&A',
    status: 'Next',
    color: 'neutral',
    icon: 'i-lucide-sparkles',
  },
];
</script>

<template>
  <div class="space-y-6">
    <section
      class="relative overflow-hidden rounded-xl border border-default bg-gradient-to-br from-primary/10 via-default to-default p-6 sm:p-8"
    >
      <div class="max-w-2xl space-y-4">
        <UBadge
          label="Local-First RAG Workspace"
          icon="i-lucide-shield-check"
          color="primary"
          variant="subtle"
          size="sm"
        />

        <div class="space-y-2">
          <h1 class="text-2xl font-semibold tracking-tight text-highlighted sm:text-3xl">
            Welcome to Lexicon
          </h1>
          <p class="text-sm leading-relaxed text-muted sm:text-base">
            Upload PDFs, Markdown notes, and text files to extract and index searchable passages locally.
            Everything stays on your machine.
          </p>
        </div>

        <div class="flex flex-wrap gap-2.5 pt-1">
          <UButton to="/documents" icon="i-lucide-files">Manage Documents</UButton>
          <UButton to="/health" icon="i-lucide-activity" color="neutral" variant="outline">
            System Health
          </UButton>
        </div>
      </div>
    </section>

    <section class="grid grid-cols-1 gap-4 sm:grid-cols-3">
      <UCard>
        <div class="flex items-start justify-between gap-3">
          <div>
            <p class="text-xs font-medium uppercase tracking-wide text-muted">Documents</p>
            <USkeleton v-if="documentsLoading && documents.length === 0" class="mt-2 h-8 w-14" />
            <p v-else class="mt-1.5 text-2xl font-semibold tracking-tight text-highlighted">
              {{ documents.length }}
            </p>
            <p class="mt-1 text-xs text-muted">
              <template v-if="documents.length === 0">Upload files to begin</template>
              <template v-else>{{ readyCount }} ready · {{ indexingCount }} indexing</template>
            </p>
          </div>
          <span
            class="flex size-9 shrink-0 items-center justify-center rounded-lg bg-primary/10 text-primary ring-1 ring-inset ring-primary/20"
          >
            <UIcon name="i-lucide-files" class="size-4" />
          </span>
        </div>
      </UCard>

      <UCard>
        <div class="flex items-start justify-between gap-3">
          <div>
            <p class="text-xs font-medium uppercase tracking-wide text-muted">Indexed Chunks</p>
            <USkeleton v-if="documentsLoading && documents.length === 0" class="mt-2 h-8 w-16" />
            <p v-else class="mt-1.5 font-mono text-2xl font-semibold tracking-tight text-highlighted">
              {{ totalChunks }}
            </p>
            <p class="mt-1 text-xs text-muted">{{ formatBytes(totalBytes) }} total stored</p>
          </div>
          <span
            class="flex size-9 shrink-0 items-center justify-center rounded-lg bg-primary/10 text-primary ring-1 ring-inset ring-primary/20"
          >
            <UIcon name="i-lucide-layers" class="size-4" />
          </span>
        </div>
      </UCard>

      <UCard>
        <div class="flex items-start justify-between gap-3">
          <div>
            <p class="text-xs font-medium uppercase tracking-wide text-muted">System Health</p>
            <USkeleton v-if="healthLoading && !health" class="mt-2.5 h-6 w-20" />
            <div v-else-if="health" class="mt-2">
              <UBadge
                :label="health.status"
                :color="healthStatusColors[health.status]"
                variant="subtle"
                size="md"
              />
            </div>
            <p v-else class="mt-2 text-sm text-muted">Unavailable</p>
            <p class="mt-1.5 text-xs text-muted">
              <template v-if="health">
                {{ healthyChecksCount }}/{{ health.checks.length }} checks healthy
              </template>
              <template v-else>Checking services...</template>
            </p>
          </div>
          <span
            class="flex size-9 shrink-0 items-center justify-center rounded-lg bg-primary/10 text-primary ring-1 ring-inset ring-primary/20"
          >
            <UIcon name="i-lucide-shield-check" class="size-4" />
          </span>
        </div>
      </UCard>
    </section>

    <section class="grid grid-cols-1 gap-6 lg:grid-cols-5">
      <UCard class="lg:col-span-3" :ui="{ body: 'p-0 sm:p-0' }">
        <template #header>
          <div class="flex items-center justify-between">
            <h2 class="text-base font-semibold text-highlighted">Recent Documents</h2>
            <UButton to="/documents" variant="link" color="neutral" size="xs" trailing-icon="i-lucide-arrow-right">
              View all
            </UButton>
          </div>
        </template>

        <div v-if="documentsLoading && documents.length === 0" class="divide-y divide-default">
          <div v-for="n in 3" :key="n" class="flex items-center justify-between px-4 py-3">
            <USkeleton class="h-4 w-44" />
            <USkeleton class="h-5 w-16 rounded-full" />
          </div>
        </div>

        <UEmpty
          v-else-if="recentDocuments.length === 0"
          icon="i-lucide-inbox"
          title="No documents uploaded yet"
          description="Head to Documents to upload your first file."
          :actions="[{ label: 'Go to Documents', to: '/documents', size: 'sm' }]"
          class="py-8"
        />

        <ul v-else class="divide-y divide-default">
          <li
            v-for="doc in recentDocuments"
            :key="doc.documentId"
            class="flex items-center justify-between gap-3 px-4 py-3"
          >
            <div class="min-w-0">
              <div class="truncate text-sm font-medium text-highlighted">{{ doc.fileName }}</div>
              <div class="text-xs text-muted">
                {{ formatBytes(doc.sizeBytes) }}
                <template v-if="doc.status === 'Ready'"> · {{ doc.chunkCount }} chunks</template>
                · {{ formatDateTime(doc.createdAt) }}
              </div>
            </div>
            <UBadge
              :label="doc.status"
              :color="documentStatusColors[doc.status]"
              variant="subtle"
              size="sm"
            />
          </li>
        </ul>
      </UCard>

      <UCard class="lg:col-span-2">
        <template #header>
          <h2 class="text-base font-semibold text-highlighted">Local Pipeline</h2>
        </template>

        <ul class="space-y-4">
          <li v-for="stage in pipelineStages" :key="stage.name" class="flex items-start justify-between gap-3">
            <div class="flex items-start gap-3">
              <span
                class="mt-0.5 flex size-8 shrink-0 items-center justify-center rounded-lg border border-default bg-elevated text-default"
              >
                <UIcon :name="stage.icon" class="size-4" />
              </span>
              <div>
                <div class="text-sm font-medium text-highlighted">{{ stage.name }}</div>
                <div class="text-xs text-muted">{{ stage.description }}</div>
              </div>
            </div>
            <UBadge :label="stage.status" :color="stage.color" variant="subtle" size="xs" />
          </li>
        </ul>
      </UCard>
    </section>
  </div>
</template>
