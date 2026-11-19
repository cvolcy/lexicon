<script setup lang="ts">
import { computed, ref } from 'vue';
import SearchResultCard from '@/components/SearchResultCard.vue';
import { useDocuments } from '@/composables/useDocuments';
import { useSearch } from '@/composables/useSearch';

const { documents } = useDocuments();
const {
  results,
  loading,
  error,
  hasSearched,
  lastQuery,
  durationMs,
  search,
  clearError,
} = useSearch();

const query = ref('');
const topK = ref(8);
const selectedDocumentIds = ref<string[]>([]);

const readyDocuments = computed(() => documents.value.filter((d) => d.status === 'Ready'));
const documentOptions = computed(() =>
  readyDocuments.value.map((d) => ({
    label: d.fileName,
    value: d.documentId,
  })),
);

const topKOptions = [5, 8, 12, 20].map((k) => ({
  label: `Top ${k}`,
  value: k,
}));

async function submit(): Promise<void> {
  await search({
    query: query.value,
    topK: topK.value,
    documentIds: selectedDocumentIds.value,
  });
}

async function clearDocumentFilterAndRetry(): Promise<void> {
  selectedDocumentIds.value = [];
  await search({
    query: lastQuery.value || query.value,
    topK: topK.value,
  });
}
</script>

<template>
  <div class="space-y-6">
    <div>
      <h1 class="text-2xl font-semibold tracking-tight text-highlighted">Search</h1>
      <p class="mt-1 text-sm text-muted">
        Hybrid semantic (vector) and keyword (FTS5) retrieval across your indexed documents.
      </p>
    </div>

    <UCard>
      <form class="space-y-3" @submit.prevent="submit">
        <div class="flex flex-col gap-2.5 sm:flex-row">
          <UInput
            v-model="query"
            icon="i-lucide-search"
            size="lg"
            placeholder="Ask a question or search for passages..."
            class="flex-1"
            :disabled="loading"
          />
          <UButton
            type="submit"
            size="lg"
            icon="i-lucide-search"
            :loading="loading"
            :disabled="!query.trim()"
          >
            Search
          </UButton>
        </div>

        <div class="flex flex-wrap items-center justify-between gap-3 pt-1">
          <div class="flex flex-wrap items-center gap-2">
            <USelect
              v-model="selectedDocumentIds"
              :items="documentOptions"
              value-key="value"
              label-key="label"
              multiple
              placeholder="All indexed documents"
              icon="i-lucide-files"
              size="sm"
              class="w-full sm:w-72"
              :disabled="documentOptions.length === 0"
            />
            <UButton
              v-if="selectedDocumentIds.length > 0"
              type="button"
              color="neutral"
              variant="ghost"
              size="xs"
              icon="i-lucide-x"
              @click="selectedDocumentIds = []"
            >
              Clear filter
            </UButton>
          </div>

          <USelect
            v-model="topK"
            :items="topKOptions"
            value-key="value"
            label-key="label"
            size="sm"
            class="w-28"
          />
        </div>
      </form>
    </UCard>

    <UAlert
      v-if="error"
      color="error"
      variant="subtle"
      icon="i-lucide-circle-alert"
      :title="error"
      close
      @update:open="clearError"
    />

    <div v-if="loading" class="space-y-4">
      <UCard v-for="n in 3" :key="n">
        <template #header>
          <div class="flex items-center justify-between gap-3">
            <USkeleton class="h-5 w-52" />
            <USkeleton class="h-5 w-32" />
          </div>
        </template>
        <div class="space-y-2">
          <USkeleton class="h-4 w-full" />
          <USkeleton class="h-4 w-5/6" />
          <USkeleton class="h-4 w-2/3" />
        </div>
      </UCard>
    </div>

    <UEmpty
      v-else-if="!hasSearched"
      icon="i-lucide-sparkles"
      title="Search your knowledge base"
      description="Enter a question or keywords to retrieve ranked passages using vector similarity and SQLite FTS5."
      :actions="
        readyDocuments.length === 0
          ? [{ label: 'Upload documents', to: '/documents', icon: 'i-lucide-upload' }]
          : undefined
      "
      class="py-12"
    />

    <UEmpty
      v-else-if="results.length === 0"
      icon="i-lucide-search-x"
      title="No matching passages found"
      description="Try different terms, clear the document filter, or verify your documents are Ready."
      :actions="
        selectedDocumentIds.length > 0
          ? [
              {
                label: 'Search all documents',
                color: 'neutral',
                variant: 'outline',
                onClick: clearDocumentFilterAndRetry,
              },
            ]
          : undefined
      "
      class="py-12"
    />

    <div v-else class="space-y-4">
      <div class="flex flex-wrap items-center justify-between gap-2 text-sm text-muted">
        <span>
          Showing {{ results.length }} {{ results.length === 1 ? 'passage' : 'passages' }} for
          <strong class="font-medium text-highlighted">"{{ lastQuery }}"</strong>
        </span>
        <UBadge
          v-if="durationMs !== null"
          :label="`${durationMs} ms`"
          color="neutral"
          variant="subtle"
          size="xs"
          class="font-mono"
        />
      </div>

      <div class="space-y-4">
        <SearchResultCard
          v-for="(result, index) in results"
          :key="result.chunkId"
          :result="result"
          :rank="index + 1"
        />
      </div>
    </div>
  </div>
</template>
