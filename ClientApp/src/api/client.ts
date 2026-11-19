import { createFetch } from '@vueuse/core';
import type { ProblemDetails } from '@/api/types';

export type { ProblemDetails };

function parseProblemDetails(data: unknown): ProblemDetails | null {
  if (data && typeof data === 'object') {
    return data as ProblemDetails;
  }

  if (typeof data === 'string' && data.trimStart().startsWith('{')) {
    try {
      return JSON.parse(data) as ProblemDetails;
    } catch {
      return null;
    }
  }

  return null;
}

function formatProblemMessage(problem: ProblemDetails | null): string | null {
  if (!problem) {
    return null;
  }

  const validationMessages = problem.errors
    ? Object.values(problem.errors).flat().filter(Boolean)
    : [];

  if (validationMessages.length > 0) {
    return validationMessages.join(' ');
  }

  return problem.detail ?? problem.title ?? null;
}

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
