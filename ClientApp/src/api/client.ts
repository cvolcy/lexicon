import { createFetch } from '@vueuse/core';

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
}

/**
 * Shared VueUse `useFetch` factory for the backend API.
 * - Prefixes every request with `/api`
 * - Always asks for JSON
 * - Turns RFC 9457 ProblemDetails responses into a readable `error` string
 */
export const useApi = createFetch({
  baseUrl: '/api',
  options: {
    onFetchError(ctx) {
      const problem = ctx.data as ProblemDetails | null;
      const message =
        problem?.detail ??
        problem?.title ??
        (ctx.response
          ? `HTTP ${ctx.response.status} ${ctx.response.statusText}`.trim()
          : ctx.error?.message) ??
        'Unexpected error.';

      return { data: ctx.data, error: message };
    },
  },
  fetchOptions: {
    headers: { Accept: 'application/json' },
  },
});
