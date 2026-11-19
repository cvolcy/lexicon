<script setup lang="ts">
import { computed, ref } from 'vue';
import { useDropZone, useFileDialog } from '@vueuse/core';
import type { DocumentStatus } from '@/api/documents';
import DocumentsListRowItem from '@/components/DocumentsListRowItem.vue';
import { useDocuments } from '@/composables/useDocuments';

const toast = useToast();

const {
  documents,
  loading,
  creating,
  deletingId,
  reindexingId,
  busy,
  hasInFlight,
  error,
  clearError,
  refresh,
  create,
  reindex,
  remove,
} = useDocuments();

type StatusFilter = 'All' | DocumentStatus;

const searchQuery = ref('');
const statusFilter = ref<StatusFilter>('All');

const counts = computed(() => {
  const ready = documents.value.filter((d) => d.status === 'Ready').length;
  const processing = documents.value.filter(
    (d) => d.status === 'Pending' || d.status === 'Processing',
  ).length;
  const failed = documents.value.filter((d) => d.status === 'Failed').length;

  return {
    All: documents.value.length,
    Ready: ready,
    Processing: processing,
    Failed: failed,
  };
});

const filterOptions = computed<{ value: StatusFilter; label: string; count: number }[]>(() => [
  { value: 'All', label: 'All', count: counts.value.All },
  { value: 'Ready', label: 'Ready', count: counts.value.Ready },
  { value: 'Processing', label: 'Indexing', count: counts.value.Processing },
  { value: 'Failed', label: 'Failed', count: counts.value.Failed },
]);

const filteredDocuments = computed(() => {
  const query = searchQuery.value.trim().toLowerCase();

  return documents.value.filter((doc) => {
    const matchesStatus =
      statusFilter.value === 'All'
        ? true
        : statusFilter.value === 'Processing'
          ? doc.status === 'Pending' || doc.status === 'Processing'
          : doc.status === statusFilter.value;

    const matchesQuery = !query || doc.fileName.toLowerCase().includes(query);
    return matchesStatus && matchesQuery;
  });
});

function resetFilters(): void {
  searchQuery.value = '';
  statusFilter.value = 'All';
}

async function handleUpload(file: File): Promise<void> {
  if (busy.value) return;

  const ok = await create(file);
  if (ok) {
    toast.add({
      title: 'Document queued for indexing',
      description: file.name,
      icon: 'i-lucide-upload',
      color: 'success',
    });
  }
}

async function handleReindex(documentId: string): Promise<void> {
  const doc = documents.value.find((d) => d.documentId === documentId);
  const ok = await reindex(documentId);
  if (ok) {
    toast.add({
      title: 'Reindexing queued',
      description: doc?.fileName,
      icon: 'i-lucide-rotate-cw',
      color: 'info',
    });
  }
}

async function handleRemove(documentId: string): Promise<void> {
  const doc = documents.value.find((d) => d.documentId === documentId);
  const ok = await remove(documentId);
  if (ok) {
    toast.add({
      title: 'Document deleted',
      description: doc?.fileName,
      icon: 'i-lucide-trash-2',
      color: 'neutral',
    });
  }
}

const { open: openFileDialog, reset: resetFileDialog, onChange } = useFileDialog({
  multiple: false,
  accept: '.txt,.md,.pdf',
});

onChange(async (files) => {
  const file = files?.[0];
  if (!file) return;
  resetFileDialog();
  await handleUpload(file);
});

const dropZoneRef = ref<HTMLElement | null>(null);
const { isOverDropZone } = useDropZone(dropZoneRef, {
  onDrop: async (files) => {
    const file = files?.[0];
    if (!file) return;
    await handleUpload(file);
  },
});
</script>

<template>
  <div class="space-y-5">
    <div class="flex flex-wrap items-center justify-between gap-4">
      <div>
        <h1 class="text-2xl font-semibold tracking-tight text-highlighted">Documents</h1>
        <p class="mt-1 text-sm text-muted">
          Upload, extract, and chunk documents for your local knowledge base.
        </p>
      </div>

      <div class="flex items-center gap-2">
        <UBadge v-if="hasInFlight" color="info" variant="subtle" size="md">
          <span class="mr-1.5 inline-block size-1.5 animate-pulse rounded-full bg-current" />
          Indexing...
        </UBadge>
        <UButton
          icon="i-lucide-refresh-cw"
          color="neutral"
          variant="outline"
          :loading="loading"
          :disabled="busy"
          @click="refresh"
        >
          Refresh
        </UButton>
        <UButton
          icon="i-lucide-upload"
          :loading="creating"
          :disabled="busy || loading"
          @click="() => openFileDialog()"
        >
          Upload Document
        </UButton>
      </div>
    </div>

    <div
      ref="dropZoneRef"
      role="button"
      tabindex="0"
      aria-label="Drop a file to upload or click to browse"
      class="flex cursor-pointer flex-col items-center justify-center gap-2 rounded-xl border border-dashed px-6 py-5 text-center transition-all focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
      :class="
        isOverDropZone
          ? 'border-primary bg-primary/10 ring-2 ring-primary/20'
          : 'border-accented bg-elevated/30 hover:border-primary/60 hover:bg-primary/5'
      "
      @click="() => openFileDialog()"
      @keydown.enter.prevent="() => openFileDialog()"
      @keydown.space.prevent="() => openFileDialog()"
    >
      <div
        class="flex size-10 items-center justify-center rounded-full bg-primary/10 text-primary ring-1 ring-inset ring-primary/20"
      >
        <UIcon :name="creating ? 'i-lucide-loader-circle' : 'i-lucide-cloud-upload'" class="size-5" :class="{ 'animate-spin': creating }" />
      </div>
      <div>
        <p class="text-sm font-medium text-highlighted">
          {{ isOverDropZone ? 'Drop file to upload' : 'Drop a file here or click to browse' }}
        </p>
        <p class="mt-0.5 text-xs text-muted">
          Supports PDF, Markdown (.md), and Plain Text (.txt) up to 25 MB
        </p>
      </div>
    </div>

    <UAlert
      v-if="error"
      color="error"
      variant="subtle"
      icon="i-lucide-circle-alert"
      :title="error"
      close
      @update:open="clearError"
    />

    <UCard :ui="{ body: 'p-0 sm:p-0' }">
      <template #header>
        <div class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <div class="flex flex-wrap items-center gap-1.5">
            <UButton
              v-for="option in filterOptions"
              :key="option.value"
              size="xs"
              :color="statusFilter === option.value ? 'primary' : 'neutral'"
              :variant="statusFilter === option.value ? 'soft' : 'ghost'"
              @click="statusFilter = option.value"
            >
              {{ option.label }}
              <span class="ml-1 font-mono text-[11px] opacity-75">{{ option.count }}</span>
            </UButton>
          </div>

          <UInput
            v-model="searchQuery"
            icon="i-lucide-search"
            placeholder="Search documents..."
            size="sm"
            class="w-full sm:w-64"
          />
        </div>
      </template>

      <div v-if="loading && documents.length === 0" class="divide-y divide-default">
        <div v-for="n in 3" :key="n" class="flex items-center justify-between gap-4 px-4 py-3.5">
          <div class="flex items-center gap-3.5">
            <USkeleton class="size-10 rounded-lg" />
            <div class="space-y-2">
              <USkeleton class="h-4 w-48" />
              <USkeleton class="h-3 w-64" />
            </div>
          </div>
          <div class="flex gap-1">
            <USkeleton class="size-7 rounded-md" />
            <USkeleton class="size-7 rounded-md" />
            <USkeleton class="size-7 rounded-md" />
          </div>
        </div>
      </div>

      <UEmpty
        v-else-if="documents.length === 0"
        icon="i-lucide-folder-open"
        title="No documents yet"
        description="Upload a PDF, Markdown, or text file to start building your local Lexicon."
        :actions="[{ label: 'Upload document', icon: 'i-lucide-upload', onClick: () => openFileDialog() }]"
        class="py-12"
      />

      <UEmpty
        v-else-if="filteredDocuments.length === 0"
        icon="i-lucide-search-x"
        title="No matching documents"
        description="Try adjusting your search query or status filter."
        :actions="[{ label: 'Clear filters', color: 'neutral', variant: 'outline', onClick: resetFilters }]"
        class="py-10"
      />

      <ul v-else class="divide-y divide-default">
        <DocumentsListRowItem
          v-for="document in filteredDocuments"
          :key="document.documentId"
          :document="document"
          :deleting="deletingId === document.documentId"
          :reindexing="reindexingId === document.documentId"
          :disabled="busy"
          @reindex="handleReindex"
          @delete="handleRemove"
        />
      </ul>
    </UCard>
  </div>
</template>
