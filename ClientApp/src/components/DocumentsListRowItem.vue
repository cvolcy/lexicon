<script setup lang="ts">
import { computed } from 'vue';
import type { BadgeProps } from '@nuxt/ui';
import {
  getDocumentDownloadUrl,
  type DocumentStatus,
  type DocumentViewModel,
} from '@/api/documents';
import { formatBytes, formatDateTime } from '@/utils/format';

const props = defineProps<{
  document: DocumentViewModel;
  deleting?: boolean;
  reindexing?: boolean;
  disabled?: boolean;
}>();

const emit = defineEmits<{
  reindex: [documentId: string];
  delete: [documentId: string];
}>();

const statusConfig: Record<DocumentStatus, { color: BadgeProps['color']; icon: string }> = {
  Pending: { color: 'neutral', icon: 'i-lucide-clock' },
  Processing: { color: 'info', icon: 'i-lucide-loader-circle' },
  Ready: { color: 'success', icon: 'i-lucide-check-circle-2' },
  Failed: { color: 'error', icon: 'i-lucide-alert-circle' },
};

const fileMeta = computed(() => {
  const lower = props.document.fileName.toLowerCase();
  if (lower.endsWith('.pdf')) {
    return {
      icon: 'i-lucide-file-text',
      badgeClass: 'bg-rose-500/10 text-rose-600 dark:text-rose-400 ring-rose-500/20',
    };
  }
  if (lower.endsWith('.md') || lower.endsWith('.markdown')) {
    return {
      icon: 'i-lucide-file-code',
      badgeClass: 'bg-indigo-500/10 text-indigo-600 dark:text-indigo-400 ring-indigo-500/20',
    };
  }
  return {
    icon: 'i-lucide-file-text',
    badgeClass: 'bg-primary/10 text-primary ring-primary/20',
  };
});

const status = computed(() => statusConfig[props.document.status]);
const isInFlight = computed(
  () => props.document.status === 'Pending' || props.document.status === 'Processing',
);
</script>

<template>
  <li class="flex items-start justify-between gap-4 px-4 py-3.5 transition-colors hover:bg-elevated/50 sm:items-center">
    <div class="flex min-w-0 items-start gap-3.5">
      <div
        class="flex size-10 shrink-0 items-center justify-center rounded-lg ring-1 ring-inset"
        :class="fileMeta.badgeClass"
      >
        <UIcon :name="fileMeta.icon" class="size-5" />
      </div>

      <div class="min-w-0 space-y-1">
        <div class="flex flex-wrap items-center gap-2">
          <span class="truncate font-medium text-highlighted">{{ document.fileName }}</span>
          <UBadge
            :label="document.status"
            :icon="status.icon"
            :color="status.color"
            variant="subtle"
            size="sm"
            :ui="document.status === 'Processing' ? { leadingIcon: 'animate-spin' } : undefined"
          />
        </div>

        <div class="flex flex-wrap items-center gap-x-2 gap-y-0.5 text-xs text-muted">
          <span>{{ formatBytes(document.sizeBytes) }}</span>
          <span aria-hidden="true">·</span>
          <span>{{ document.contentType }}</span>
          <template v-if="document.status === 'Ready'">
            <span aria-hidden="true">·</span>
            <span class="font-mono text-default">
              {{ document.chunkCount }} {{ document.chunkCount === 1 ? 'chunk' : 'chunks' }}
            </span>
          </template>
          <span aria-hidden="true">·</span>
          <span>{{ formatDateTime(document.createdAt) }}</span>
        </div>

        <div
          v-if="document.error"
          class="mt-1 flex items-center gap-1.5 rounded-md bg-error/10 px-2 py-1 text-xs text-error"
        >
          <UIcon name="i-lucide-triangle-alert" class="size-3.5 shrink-0" />
          <span class="break-words">{{ document.error }}</span>
        </div>
      </div>
    </div>

    <div class="flex shrink-0 items-center gap-1">
      <UTooltip text="Reindex document">
        <UButton
          icon="i-lucide-rotate-cw"
          color="neutral"
          variant="ghost"
          size="sm"
          :loading="reindexing"
          :disabled="disabled || isInFlight"
          :aria-label="`Reindex ${document.fileName}`"
          @click="emit('reindex', document.documentId)"
        />
      </UTooltip>
      <UTooltip text="Download original file">
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
      </UTooltip>
      <UTooltip text="Delete document">
        <UButton
          icon="i-lucide-trash-2"
          color="error"
          variant="ghost"
          size="sm"
          :loading="deleting"
          :disabled="disabled"
          :aria-label="`Delete ${document.fileName}`"
          @click="emit('delete', document.documentId)"
        />
      </UTooltip>
    </div>
  </li>
</template>
