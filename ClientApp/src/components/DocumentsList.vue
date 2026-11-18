<script setup lang="ts">
import { useDocuments } from '@/composables/useDocuments';

const { documents, loading, busy, error, refresh, create, remove } = useDocuments();
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
          <UButton icon="i-lucide-plus" size="sm" :loading="busy" :disabled="loading" @click="create">
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
          <div class="font-mono text-sm break-all">{{ document.documentId }}</div>
          <div class="text-xs text-muted">{{ new Date(document.createdAt).toLocaleString() }}</div>
        </div>
        <UButton
          icon="i-lucide-trash-2"
          color="error"
          variant="ghost"
          size="sm"
          :disabled="busy"
          :aria-label="`Delete document ${document.documentId}`"
          @click="remove(document.documentId)"
        />
      </li>
    </ul>
  </UCard>
</template>
