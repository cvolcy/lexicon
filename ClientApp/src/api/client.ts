import { createFetch } from '@vueuse/core';
import { formatProblemMessage, parseProblemDetails } from '@/api/problemDetails';
import type { ProblemDetails } from '@/api/types';

export type { ProblemDetails };

/**
 * Shared VueUse `useFetch` factory for the backend API.
 * - Prefixes every request with `/api`
 * - Always asks for JSON
 * - Turns RFC 9457 ProblemDetails responses (including validation errors) into a readable `error` string
 */
export const useApi = createFetch({
  baseUrl: '/api',
  options: {
    onFetchError(ctx) {
      const problem = parseProblemDetails(ctx.data);
      const message =
        formatProblemMessage(problem) ??
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
