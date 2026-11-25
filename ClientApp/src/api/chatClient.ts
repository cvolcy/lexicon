import type {
  ChatClient,
  ChatMessageViewModel,
  ChatStreamEvent,
  CitationViewModel,
  ConversationDetailViewModel,
  ConversationSummaryViewModel,
} from '@/api/chat';
import { readResponseErrorMessage } from '@/api/problemDetails';
import type { CollectionResponse } from '@/api/types';

function dispatchSseEvent(
  eventName: string,
  data: string,
  onEvent: (event: ChatStreamEvent) => void,
): void {
  if (!data) return;

  if (eventName === 'error') {
    try {
      const parsed = JSON.parse(data) as { message?: string } | string;
      const message = typeof parsed === 'string' ? parsed : (parsed.message ?? 'Streaming failed.');
      onEvent({ type: 'error', message });
    } catch {
      onEvent({ type: 'error', message: data });
    }
    return;
  }

  try {
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
    }
  } catch {
    onEvent({
      type: 'error',
      message: 'Received a malformed streaming event from the server.',
    });
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
      dataLines.push(line.startsWith('data: ') ? line.slice(6) : line.slice(5));
    }
  }

  if (dataLines.length > 0) {
    dispatchSseEvent(eventName, dataLines.join('\n'), onEvent);
  }
}

/**
 * Production HTTP + SSE client targeting the Phase 4 Conversations API.
 */
export function createHttpChatClient(fetchFn: typeof fetch = fetch): ChatClient {
  async function fetchJson<T>(url: string, init: RequestInit = {}): Promise<T> {
    const response = await fetchFn(url, {
      ...init,
      headers: {
        Accept: 'application/json',
        ...init.headers,
      },
    });

    if (!response.ok) {
      throw new Error(await readResponseErrorMessage(response));
    }

    if (response.status === 204) {
      return undefined as T;
    }

    return (await response.json()) as T;
  }

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
      const response = await fetchFn(
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
        throw new Error(await readResponseErrorMessage(response));
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
