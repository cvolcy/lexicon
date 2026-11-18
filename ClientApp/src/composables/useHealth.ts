import { computed } from 'vue';
import { useApi } from '@/api/client';
import type { HealthResponse } from '@/api/health';

export function useHealth() {
  const { data, error, isFetching, execute } = useApi('/health').get().json<HealthResponse>();

  return {
    health: data,
    error: computed<string | null>(() => error.value ?? null),
    loading: isFetching,
    refresh: () => execute(),
  };
}
