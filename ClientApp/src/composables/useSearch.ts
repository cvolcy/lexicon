import { computed, ref } from 'vue';
import { useApi } from '@/api/client';
import type { SearchRequest, SearchResultViewModel } from '@/api/search';
import type { CollectionResponse } from '@/api/types';

export function useSearch() {
  const payload = ref<SearchRequest>({ query: '' });

  const searchRequest = useApi('/search', { immediate: false })
    .post(payload, 'json')
    .json<CollectionResponse<SearchResultViewModel>>();

  const hasSearched = ref(false);
  const lastQuery = ref('');
  const durationMs = ref<number | null>(null);

  const results = computed(() => searchRequest.data.value?.items ?? []);
  const loading = computed(() => searchRequest.isFetching.value);
  const error = computed<string | null>(() => searchRequest.error.value ?? null);

  function clearError(): void {
    searchRequest.error.value = null;
  }

  async function search(request: SearchRequest): Promise<void> {
    const trimmed = request.query.trim();
    if (!trimmed) {
      return;
    }

    clearError();
    payload.value = {
      query: trimmed,
      topK: request.topK,
      documentIds: request.documentIds?.length ? request.documentIds : undefined,
    };

    const startedAt = performance.now();
    await searchRequest.execute();

    durationMs.value = Math.round(performance.now() - startedAt);
    lastQuery.value = trimmed;
    hasSearched.value = true;
  }

  function reset(): void {
    hasSearched.value = false;
    lastQuery.value = '';
    durationMs.value = null;
    searchRequest.data.value = null;
    clearError();
  }

  return {
    results,
    loading,
    error,
    hasSearched,
    lastQuery,
    durationMs,
    search,
    clearError,
    reset,
  };
}
