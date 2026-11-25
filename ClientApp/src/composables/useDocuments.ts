import { computed, ref, shallowRef, watch } from 'vue';
import { useIntervalFn } from '@vueuse/core';
import { useApi } from '@/api/client';
import type { DocumentViewModel } from '@/api/documents';
import type { CollectionResponse } from '@/api/types';

export function useDocuments() {
  const list = useApi('/documents').get().json<CollectionResponse<DocumentViewModel>>();

  const createPayload = shallowRef<FormData | null>(null);
  const createRequest = useApi('/documents', { immediate: false })
    .post(createPayload)
    .json<DocumentViewModel>();

  const reindexId = ref('');
  const reindexUrl = computed(() => `/documents/${encodeURIComponent(reindexId.value)}/reindex`);
  const reindexRequest = useApi(reindexUrl, { immediate: false })
    .post()
    .json<DocumentViewModel>();

  const deleteId = ref('');
  const deleteUrl = computed(() => `/documents/${encodeURIComponent(deleteId.value)}`);
  const deleteRequest = useApi(deleteUrl, { immediate: false }).delete();

  const reindexingId = ref<string | null>(null);
  const deletingId = ref<string | null>(null);
  const isPolling = ref(false);
  const actionError = ref<string | null>(null);

  const documents = computed(() => list.data.value?.items ?? []);
  const loading = computed(() => list.isFetching.value && !isPolling.value);
  const creating = computed(() => createRequest.isFetching.value);
  const busy = computed(
    () => creating.value || deletingId.value !== null || reindexingId.value !== null,
  );
  const error = computed<string | null>(() => actionError.value ?? list.error.value ?? null);

  const hasInFlight = computed(() =>
    documents.value.some((d) => d.status === 'Pending' || d.status === 'Processing'),
  );

  async function poll(): Promise<void> {
    if (list.isFetching.value || busy.value) {
      return;
    }

    isPolling.value = true;
    try {
      await list.execute();
    } finally {
      isPolling.value = false;
    }
  }

  const { pause, resume } = useIntervalFn(poll, 2000, { immediate: false });
  watch(hasInFlight, (active) => (active ? resume() : pause()), { immediate: true });

  function clearError(): void {
    actionError.value = null;
    list.error.value = null;
  }

  async function refresh(): Promise<void> {
    clearError();
    await list.execute();
  }

  async function mutate(request: {
    execute: () => Promise<unknown>;
    error: { value: unknown };
  }): Promise<boolean> {
    actionError.value = null;
    await request.execute();

    if (request.error.value) {
      actionError.value = String(request.error.value);
      return false;
    }

    await list.execute();
    return true;
  }

  async function create(file: File): Promise<boolean> {
    if (busy.value) {
      return false;
    }

    const formData = new FormData();
    formData.append('file', file);
    createPayload.value = formData;

    try {
      return await mutate(createRequest);
    } finally {
      createPayload.value = null;
    }
  }

  async function reindex(documentId: string): Promise<boolean> {
    if (busy.value) {
      return false;
    }

    reindexId.value = documentId;
    reindexingId.value = documentId;

    try {
      return await mutate(reindexRequest);
    } finally {
      reindexingId.value = null;
    }
  }

  async function remove(documentId: string): Promise<boolean> {
    if (busy.value) {
      return false;
    }

    deleteId.value = documentId;
    deletingId.value = documentId;

    try {
      return await mutate(deleteRequest);
    } finally {
      deletingId.value = null;
    }
  }

  return {
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
  };
}
