import { computed, ref, shallowRef } from 'vue';
import { useApi } from '@/api/client';
import type { DocumentViewModel } from '@/api/documents';
import type { CollectionResponse } from '@/api/types';

export function useDocuments() {
  const list = useApi('/documents').get().json<CollectionResponse<DocumentViewModel>>();

  const createPayload = shallowRef<FormData | null>(null);
  const createRequest = useApi('/documents', { immediate: false })
    .post(createPayload)
    .json<DocumentViewModel>();

  const deleteId = ref('');
  const deleteUrl = computed(() => `/documents/${encodeURIComponent(deleteId.value)}`);
  const deleteRequest = useApi(deleteUrl, { immediate: false }).delete();

  const deletingId = ref<string | null>(null);
  const actionError = ref<string | null>(null);

  const documents = computed(() => list.data.value?.items ?? []);
  const loading = computed(() => list.isFetching.value);
  const creating = computed(() => createRequest.isFetching.value);
  const busy = computed(() => creating.value || deletingId.value !== null);
  const error = computed<string | null>(() => actionError.value ?? list.error.value ?? null);

  function clearError(): void {
    actionError.value = null;
    list.error.value = null;
  }

  async function refresh(): Promise<void> {
    clearError();
    await list.execute();
  }

  async function mutate(request: { execute: () => Promise<unknown>; error: { value: unknown } }) {
    actionError.value = null;
    await request.execute();

    if (request.error.value) {
      actionError.value = String(request.error.value);
      return;
    }

    await list.execute();
  }

  async function create(file: File): Promise<void> {
    const formData = new FormData();
    formData.append('file', file);
    createPayload.value = formData;

    try {
      await mutate(createRequest);
    } finally {
      createPayload.value = null;
    }
  }

  async function remove(documentId: string): Promise<void> {
    deleteId.value = documentId;
    deletingId.value = documentId;

    try {
      await mutate(deleteRequest);
    } finally {
      deletingId.value = null;
    }
  }

  return { documents, loading, creating, deletingId, busy, error, clearError, refresh, create, remove };
}
