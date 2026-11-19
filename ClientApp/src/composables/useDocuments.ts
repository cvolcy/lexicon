import { computed, ref, shallowRef } from 'vue';
import { useApi } from '@/api/client';
import type { CollectionResponse, DocumentViewModel } from '@/api/documents';

export function useDocuments() {
  const list = useApi('/documents').get().json<CollectionResponse<DocumentViewModel>>();

  const createPayload = shallowRef<FormData | null>(null);
  const createRequest = useApi('/documents', { immediate: false })
    .post(createPayload)
    .json<DocumentViewModel>();

  const deleteId = ref('');
  const deleteUrl = computed(() => `/documents/${encodeURIComponent(deleteId.value)}`);
  const deleteRequest = useApi(deleteUrl, { immediate: false }).delete();

  const actionError = ref<string | null>(null);

  const documents = computed(() => list.data.value?.items ?? []);
  const loading = computed(() => list.isFetching.value);
  const busy = computed(() => createRequest.isFetching.value || deleteRequest.isFetching.value);
  const error = computed<string | null>(() => actionError.value ?? list.error.value ?? null);

  async function refresh(): Promise<void> {
    actionError.value = null;
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

  function create(file: File): Promise<void> {
    const formData = new FormData();
    formData.append('file', file);
    createPayload.value = formData;
    return mutate(createRequest);
  }

  function remove(documentId: string): Promise<void> {
    deleteId.value = documentId;
    return mutate(deleteRequest);
  }

  return { documents, loading, busy, error, refresh, create, remove };
}
