import type {
  ChatClient,
  ChatMessageViewModel,
  ChatStreamEvent,
  CitationViewModel,
  ConversationDetailViewModel,
  ConversationSummaryViewModel,
  SendMessageRequest,
} from '@/api/chat';
import type { SearchResultViewModel } from '@/api/search';
import type { CollectionResponse, ProblemDetails } from '@/api/types';

const PREVIEW_STORAGE_KEY = 'lexicon:chat-preview';

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
            Accept: 'text/event-stream',
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

function loadPreviewConversations(): ConversationDetailViewModel[] {
  try {
    const raw = localStorage.getItem(PREVIEW_STORAGE_KEY);
    if (!raw) return [];
    const parsed = JSON.parse(raw);
    return Array.isArray(parsed) ? (parsed as ConversationDetailViewModel[]) : [];
  } catch {
    return [];
  }
}

function savePreviewConversations(conversations: ConversationDetailViewModel[]): void {
  localStorage.setItem(PREVIEW_STORAGE_KEY, JSON.stringify(conversations));
}

function toSummary(detail: ConversationDetailViewModel): ConversationSummaryViewModel {
  return {
    conversationId: detail.conversationId,
    title: detail.title,
    messageCount: detail.messageCount,
    createdAt: detail.createdAt,
    updatedAt: detail.updatedAt,
  };
}

function summarizeTitle(prompt: string): string {
  const singleLine = prompt.replace(/\s+/g, ' ').trim();
  if (singleLine.length <= 60) return singleLine || 'New conversation';
  return `${singleLine.slice(0, 57)}...`;
}

function buildExcerpt(text: string, maxLength = 260): string {
  const compact = text.replace(/\s+/g, ' ').trim();
  if (compact.length <= maxLength) return compact;
  return `${compact.slice(0, maxLength - 3).trimEnd()}...`;
}

function buildPreviewAnswer(
  query: string,
  citations: CitationViewModel[],
  searchError: string | null,
): string {
  if (searchError) {
    return [
      `I couldn't retrieve passages from \`POST /api/search\` (${searchError}).`,
      '',
      'Make sure the backend is running and Ollama is reachable on the **System Health** page. Once Phase 4 (`/api/conversations`) is implemented, full LLM synthesis will replace this retrieval preview.',
    ].join('\n');
  }

  if (citations.length === 0) {
    return [
      `I searched your indexed documents for **"${query}"**, but found no matching passages.`,
      '',
      '- Verify that your files on the **Documents** page have reached **Ready** status.',
      '- Try broader keywords or clear any active document filter.',
    ].join('\n');
  }

  const lines = [
    `Here are the top **${citations.length}** retrieved passages for **"${query}"** *(retrieval preview — LLM synthesis arrives in Phase 4)*:`,
    '',
  ];

  for (const citation of citations) {
    const location = [
      citation.pageNumber !== null ? `p. ${citation.pageNumber}` : null,
      citation.section ? citation.section : null,
    ]
      .filter(Boolean)
      .join(' · ');

    const header = location
      ? `**${citation.documentFileName}** (${location}) [${citation.index}]`
      : `**${citation.documentFileName}** [${citation.index}]`;

    lines.push(`- ${header}: "${buildExcerpt(citation.text)}"`);
  }

  lines.push('', 'Click any citation chip such as [1] to inspect the full source passage.');

  return lines.join('\n');
}

function delay(ms: number, signal?: AbortSignal): Promise<void> {
  return new Promise((resolve, reject) => {
    if (signal?.aborted) {
      reject(new DOMException('Aborted', 'AbortError'));
      return;
    }

    const timer = setTimeout(() => {
      signal?.removeEventListener('abort', onAbort);
      resolve();
    }, ms);

    function onAbort(): void {
      clearTimeout(timer);
      reject(new DOMException('Aborted', 'AbortError'));
    }

    signal?.addEventListener('abort', onAbort, { once: true });
  });
}

/**
 * Local preview client persisted in localStorage and backed by real `POST /api/search` retrieval.
 * Replace with `createHttpChatClient()` once Phase 4 backend endpoints are in place.
 */
export function createLocalPreviewChatClient(): ChatClient {
  return {
    async listConversations() {
      return loadPreviewConversations()
        .map(toSummary)
        .sort((a, b) => b.updatedAt.localeCompare(a.updatedAt));
    },

    async getConversation(conversationId) {
      const match = loadPreviewConversations().find((c) => c.conversationId === conversationId);
      if (!match) {
        throw new Error('Conversation not found.');
      }
      return match;
    },

    async createConversation(title) {
      const now = new Date().toISOString();
      const created: ConversationDetailViewModel = {
        conversationId: crypto.randomUUID(),
        title: title?.trim() || 'New conversation',
        messageCount: 0,
        createdAt: now,
        updatedAt: now,
        messages: [],
      };

      const all = loadPreviewConversations();
      all.unshift(created);
      savePreviewConversations(all);
      return toSummary(created);
    },

    async deleteConversation(conversationId) {
      const remaining = loadPreviewConversations().filter((c) => c.conversationId !== conversationId);
      savePreviewConversations(remaining);
    },

    async streamMessage(conversationId, request, onEvent, signal) {
      const now = new Date().toISOString();
      const userMessage: ChatMessageViewModel = {
        messageId: crypto.randomUUID(),
        conversationId,
        role: 'user',
        content: request.content,
        citations: [],
        createdAt: now,
      };

      const conversations = loadPreviewConversations();
      const conversation = conversations.find((c) => c.conversationId === conversationId);
      if (!conversation) {
        throw new Error('Conversation not found.');
      }

      if (conversation.messages.length === 0 && conversation.title === 'New conversation') {
        conversation.title = summarizeTitle(request.content);
      }
      conversation.messages.push(userMessage);
      conversation.messageCount = conversation.messages.length;
      conversation.updatedAt = now;
      savePreviewConversations(conversations);

      let citations: CitationViewModel[] = [];
      let searchError: string | null = null;

      try {
        const searchResult = await fetchJson<CollectionResponse<SearchResultViewModel>>(
          '/api/search',
          {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
              query: request.content,
              topK: 5,
              documentIds: request.documentIds?.length ? request.documentIds : undefined,
            }),
            signal,
          },
        );

        citations = searchResult.items.map((item, idx) => ({
          index: idx + 1,
          chunkId: item.chunkId,
          documentId: item.documentId,
          documentFileName: item.documentFileName,
          chunkIndex: item.chunkIndex,
          text: item.text,
          pageNumber: item.pageNumber,
          section: item.section,
          score: item.score,
        }));
      } catch (err) {
        if (err instanceof DOMException && err.name === 'AbortError') {
          throw err;
        }
        searchError = err instanceof Error ? err.message : 'Search request failed.';
      }

      if (citations.length > 0) {
        onEvent({ type: 'citations', citations });
      }

      const answer = buildPreviewAnswer(request.content, citations, searchError);
      const chunks = answer.match(/\S+\s*|\s+/g) ?? [answer];

      for (let i = 0; i < chunks.length; i += 3) {
        if (signal?.aborted) {
          throw new DOMException('Aborted', 'AbortError');
        }
        onEvent({ type: 'token', content: chunks.slice(i, i + 3).join('') });
        await delay(18, signal);
      }

      const completedAt = new Date().toISOString();
      const assistantMessage: ChatMessageViewModel = {
        messageId: crypto.randomUUID(),
        conversationId,
        role: 'assistant',
        content: answer,
        citations,
        createdAt: completedAt,
      };

      const latest = loadPreviewConversations();
      const target = latest.find((c) => c.conversationId === conversationId);
      if (target) {
        target.messages.push(assistantMessage);
        target.messageCount = target.messages.length;
        target.updatedAt = completedAt;
        savePreviewConversations(latest);
      }

      onEvent({ type: 'done', message: assistantMessage });
    },
  };
}

export const isPreviewChatMode = true;

export const chatClient: ChatClient = createLocalPreviewChatClient();
