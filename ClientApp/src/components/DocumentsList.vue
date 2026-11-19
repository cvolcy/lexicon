<script setup lang="ts">
import { useFileDialog } from '@vueuse/core';
import type { BadgeProps } from '@nuxt/ui';
import { getDocumentDownloadUrl, type DocumentStatus } from '@/api/documents';
import { useDocuments } from '@/composables/useDocuments';

const { documents, loading, busy, error, refresh, create, remove } = useDocuments();

const statusColors: Record<DocumentStatus, BadgeProps['color']> = {
  Pending: 'neutral',
  Processing: 'info',
  Ready: 'success',
  Failed: 'error',
};

function formatSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

const { open: openFileDialog, reset: resetFileDialog, onChange } = useFileDialog({
  multiple: false,
});

onChange(async (files) => {
  const file = files?.[0];
  if (!file) return;
  resetFileDialog();
  await create(file);
});
</script>

<template>
  <UCard>
    <template #header>
      <div class="flex items-center justify-between">
        <h2 class="text-lg font-semibold">Documents</h2>
        <div class="flex gap-2">
          <UButton
            icon="i-lucide-refresh-cw"
            color="neutral"
            variant="outline"
            size="sm"
            :loading="loading"
            :disabled="busy"
            @click="refresh"
          >
            Refresh
          </UButton>
          <UButton
            icon="i-lucide-plus"
            size="sm"
            :loading="busy"
            :disabled="loading"
            @click="() => openFileDialog()"
          >
            New
          </UButton>
        </div>
      </div>
    </template>

    <UAlert v-if="error" color="error" variant="subtle" icon="i-lucide-circle-alert" :title="error" />
    <p v-else-if="loading && documents.length === 0" class="text-sm text-muted">Loading...</p>
    <p v-else-if="documents.length === 0" class="text-sm text-muted">No documents yet.</p>

    <ul v-else class="divide-y divide-default">
      <li
        v-for="document in documents"
        :key="document.documentId"
        class="flex items-center justify-between gap-4 py-3"
      >
        <div class="min-w-0">
          <div class="flex items-center gap-2">
            <span class="truncate font-medium">{{ document.fileName }}</span>
            <UBadge
              :label="document.status"
              :color="statusColors[document.status]"
              variant="subtle"
              size="sm"
            />
          </div>
          <div class="text-xs text-muted">
            {{ formatSize(document.sizeBytes) }} · {{ document.contentType }} ·
            {{ new Date(document.createdAt).toLocaleString() }}
          </div>
          <div v-if="document.error" class="text-xs text-error">{{ document.error }}</div>
        </div>
        <div class="flex shrink-0 items-center gap-1">
          <UButton
            icon="i-lucide-download"
            color="neutral"
            variant="ghost"
            size="sm"
            :to="getDocumentDownloadUrl(document.documentId)"
            external
            :download="document.fileName"
            :aria-label="`Download ${document.fileName}`"
          />
          <UButton
            icon="i-lucide-trash-2"
            color="error"
            variant="ghost"
            size="sm"
            :disabled="busy"
            :aria-label="`Delete ${document.fileName}`"
            @click="remove(document.documentId)"
          />
        </div>
      </li>
    </ul>
  </UCard>
</template>
