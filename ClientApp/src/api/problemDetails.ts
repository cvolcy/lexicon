import type { ProblemDetails } from '@/api/types';

export function parseProblemDetails(data: unknown): ProblemDetails | null {
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

export function formatProblemMessage(problem: ProblemDetails | null): string | null {
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

export async function readResponseErrorMessage(response: Response): Promise<string> {
  try {
    const bodyText = await response.text();
    const message = formatProblemMessage(parseProblemDetails(bodyText));
    if (message) {
      return message;
    }
  } catch {
    // Body could not be read as text; fall back to HTTP status.
  }

  return `HTTP ${response.status} ${response.statusText}`.trim();
}
