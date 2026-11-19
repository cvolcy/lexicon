import { computed } from 'vue';
import { useApi } from '@/api/client';
import type { HealthViewModel } from '@/api/health';

function isHealthViewModel(value: unknown): value is HealthViewModel {
  return (
    typeof value === 'object' &&
    value !== null &&
    'status' in value &&
    'checks' in value &&
    Array.isArray((value as HealthViewModel).checks)
  );
}

export function useHealth() {
  const { data, error, isFetching, execute } = useApi('/health', {
    updateDataOnError: true,
  })
    .get()
    .json<HealthViewModel>();

  const health = computed<HealthViewModel | null>(() =>
    isHealthViewModel(data.value) ? data.value : null,
  );

  const requestError = computed<string | null>(() =>
    health.value ? null : (error.value ?? null),
  );

  return {
    health,
    error: requestError,
    loading: isFetching,
    refresh: () => execute(),
  };
}
