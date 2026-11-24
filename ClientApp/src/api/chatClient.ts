import type {
  ChatClient,
  ChatMessageViewModel,
  ChatStreamEvent,
  CitationViewModel,
  ConversationDetailViewModel,
  ConversationSummaryViewModel,
} from '@/api/chat';
import type { CollectionResponse, ProblemDetails } from '@/api/types';

async function readErrorMessage(response: Response): Promise<string> {
  try {
    const problem = (await response.json()) as ProblemDetails;
    const validation = problem.errors
      ? Object.values(problem.errors).flat().filter(Boolean)
      : [];
    if (validation.length > 0) {
      return validation.join(' ');
    }
    if (problem.detail ?? problem.title) {
      return (problem.detail ?? problem.title)!;
    }
  } catch {
    // Response body is not JSON; fall back to HTTP status.
  }

  return `HTTP ${response.status} ${response.statusText}`.trim();
}

async function fetchJson<T>(url: string, init: RequestInit = {}): Promise<T> {
  const response = await fetch(url, {
    ...init,
    headers: {
      Accept: 'application/json',
      ...init.headers,
    },
  });

  if (!response.ok) {
    throw new Error(await readErrorMessage(response));
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

function dispatchSseEvent(
  eventName: string,
  data: string,
  onEvent: (event: ChatStreamEvent) => void,
): void {
  if (!data) return;

  switch (eventName) {
    case 'token': {
      const parsed = JSON.parse(data) as { content?: string } | string;
      const content = typeof parsed === 'string' ? parsed : (parsed.content ?? '');
      if (content) {
        onEvent({ type: 'token', content });
      }
      break;
    }
    case 'citations': {
      const citations = JSON.parse(data) as CitationViewModel[];
      onEvent({ type: 'citations', citations });
      break;
    }
    case 'done': {
      const message = JSON.parse(data) as ChatMessageViewModel;
      onEvent({ type: 'done', message });
      break;
    }
    case 'error': {
      const parsed = JSON.parse(data) as { message?: string } | string;
      const message = typeof parsed === 'string' ? parsed : (parsed.message ?? 'Streaming failed.');
      onEvent({ type: 'error', message });
      break;
    }
  }
}

function parseSseBlock(block: string, onEvent: (event: ChatStreamEvent) => void): void {
  let eventName = 'message';
  const dataLines: string[] = [];

  for (const line of block.split('\n')) {
    if (line.startsWith(':')) continue;
    if (line.startsWith('event:')) {
      eventName = line.slice(6).trim();
    } else if (line.startsWith('data:')) {
      dataLines.push(line.slice(5).trimStart());
    }
  }

  if (dataLines.length > 0) {
    dispatchSseEvent(eventName, dataLines.join('\n'), onEvent);
  }
}

/**
 * Production HTTP + SSE client targeting the Phase 4 Conversations API.
 */
export function createHttpChatClient(): ChatClient {
  return {
    async listConversations(signal) {
      const result = await fetchJson<CollectionResponse<ConversationSummaryViewModel>>(
        '/api/conversations',
        { signal },
      );
      return result.items;
    },

    getConversation(conversationId, signal) {
      return fetchJson<ConversationDetailViewModel>(
        `/api/conversations/${encodeURIComponent(conversationId)}`,
        { signal },
      );
    },

    createConversation(title, signal) {
      return fetchJson<ConversationSummaryViewModel>('/api/conversations', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(title ? { title } : {}),
        signal,
      });
    },

    async deleteConversation(conversationId, signal) {
      await fetchJson<void>(`/api/conversations/${encodeURIComponent(conversationId)}`, {
        method: 'DELETE',
        signal,
      });
    },

    async streamMessage(conversationId, request, onEvent, signal) {
      const response = await fetch(
        `/api/conversations/${encodeURIComponent(conversationId)}/messages`,
        {
          method: 'POST',
          headers: {
            Accept: 'text/event-stream, application/json',
            'Content-Type': 'application/json',
          },
          body: JSON.stringify(request),
          signal,
        },
      );

      if (!response.ok) {
        throw new Error(await readErrorMessage(response));
      }

      if (!response.body) {
        throw new Error('Streaming response body is empty.');
      }

      const reader = response.body.getReader();
      const decoder = new TextDecoder();
      let buffer = '';

      while (true) {
        const { done, value } = await reader.read();
        if (done) break;

        buffer += decoder.decode(value, { stream: true }).replace(/\r\n/g, '\n');

        let boundary = buffer.indexOf('\n\n');
        while (boundary !== -1) {
          const block = buffer.slice(0, boundary);
          buffer = buffer.slice(boundary + 2);
          if (block.trim()) {
            parseSseBlock(block, onEvent);
          }
          boundary = buffer.indexOf('\n\n');
        }
      }

      const trailing = (buffer + decoder.decode()).trim();
      if (trailing) {
        parseSseBlock(trailing, onEvent);
      }
    },
  };
}

export const chatClient: ChatClient = createHttpChatClient();
