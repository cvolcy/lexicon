<script setup lang="ts">
import type { BadgeProps } from '@nuxt/ui';
import {
  getDocumentDownloadUrl,
  type DocumentStatus,
  type DocumentViewModel,
} from '@/api/documents';

defineProps<{
  document: DocumentViewModel;
  disabled?: boolean;
}>();

const emit = defineEmits<{
  delete: [documentId: string];
}>();

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
</script>

<template>
  <li class="flex items-center justify-between gap-4 py-3">
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
        :disabled="disabled"
        :aria-label="`Delete ${document.fileName}`"
        @click="emit('delete', document.documentId)"
      />
    </div>
  </li>
</template>
