<script setup lang="ts">
import { useFileDialog } from '@vueuse/core';
import DocumentsListRowItem from '@/components/DocumentsListRowItem.vue';
import { useDocuments } from '@/composables/useDocuments';

const {
  documents,
  loading,
  creating,
  deletingId,
  reindexingId,
  busy,
  error,
  clearError,
  refresh,
  create,
  reindex,
  remove,
} = useDocuments();

const { open: openFileDialog, reset: resetFileDialog, onChange } = useFileDialog({
  multiple: false,
  accept: '.txt,.md,.pdf',
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
        <h1 class="text-lg font-semibold">Documents</h1>
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
            :loading="creating"
            :disabled="busy || loading"
            @click="() => openFileDialog()"
          >
            New
          </UButton>
        </div>
      </div>
    </template>

    <UAlert
      v-if="error"
      color="error"
      variant="subtle"
      icon="i-lucide-circle-alert"
      :title="error"
      close
      class="mb-4"
      @update:open="clearError"
    />

    <p v-if="loading && documents.length === 0" class="text-sm text-muted">Loading...</p>
    <p v-else-if="documents.length === 0" class="text-sm text-muted">No documents yet.</p>

    <ul v-else class="divide-y divide-default">
      <DocumentsListRowItem
        v-for="document in documents"
        :key="document.documentId"
        :document="document"
        :deleting="deletingId === document.documentId"
        :reindexing="reindexingId === document.documentId"
        :disabled="busy"
        @reindex="reindex"
        @delete="remove"
      />
    </ul>
  </UCard>
</template>
