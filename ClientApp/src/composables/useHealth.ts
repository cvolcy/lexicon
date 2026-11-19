import { computed } from 'vue';
import { useApi } from '@/api/client';
import type { HealthViewModel } from '@/api/health';

export function useHealth() {
  const { data, error, isFetching, execute } = useApi('/health').get().json<HealthViewModel>();

  return {
    health: data,
    error: computed<string | null>(() => error.value ?? null),
    loading: isFetching,
    refresh: () => execute(),
  };
}
