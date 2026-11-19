<script setup lang="ts">
import { useFileDialog } from '@vueuse/core';
import DocumentsListRowItem from '@/components/DocumentsListRowItem.vue';
import { useDocuments } from '@/composables/useDocuments';

const { documents, loading, busy, error, refresh, create, remove } = useDocuments();

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
      <DocumentsListRowItem
        v-for="document in documents"
        :key="document.documentId"
        :document="document"
        :disabled="busy"
        @delete="remove"
      />
    </ul>
  </UCard>
</template>
