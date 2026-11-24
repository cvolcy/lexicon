import { onBeforeUnmount, onMounted, ref } from 'vue';
import type {
  ChatMessageViewModel,
  ConversationSummaryViewModel,
} from '@/api/chat';
import { chatClient } from '@/api/chatClient';

function isAbortError(err: unknown): boolean {
  return err instanceof DOMException && err.name === 'AbortError';
}

export function useChat() {
  const conversations = ref<ConversationSummaryViewModel[]>([]);
  const activeConversationId = ref<string | null>(null);
  const messages = ref<ChatMessageViewModel[]>([]);

  const loadingConversations = ref(false);
  const loadingMessages = ref(false);
  const streaming = ref(false);
  const streamingMessageId = ref<string | null>(null);
  const error = ref<string | null>(null);

  let streamAbortController: AbortController | null = null;

  function clearError(): void {
    error.value = null;
  }

  function stopStreaming(): void {
    streamAbortController?.abort();
    streamAbortController = null;
  }

  async function refreshConversationsList(): Promise<void> {
    conversations.value = await chatClient.listConversations();
  }

  async function selectConversation(conversationId: string): Promise<void> {
    stopStreaming();
    loadingMessages.value = true;
    error.value = null;
    activeConversationId.value = conversationId;

    try {
      const detail = await chatClient.getConversation(conversationId);
      if (activeConversationId.value === conversationId) {
        messages.value = detail.messages;
      }
    } catch (err) {
      if (!isAbortError(err)) {
        error.value = err instanceof Error ? err.message : 'Unable to load conversation.';
      }
    } finally {
      loadingMessages.value = false;
    }
  }

  async function loadConversations(): Promise<void> {
    loadingConversations.value = true;
    error.value = null;

    try {
      await refreshConversationsList();
      if (!activeConversationId.value && conversations.value.length > 0) {
        await selectConversation(conversations.value[0]!.conversationId);
      }
    } catch (err) {
      if (!isAbortError(err)) {
        error.value = err instanceof Error ? err.message : 'Unable to load conversations.';
      }
    } finally {
      loadingConversations.value = false;
    }
  }

  function startNewConversation(): void {
    stopStreaming();
    error.value = null;
    activeConversationId.value = null;
    messages.value = [];
  }

  async function deleteConversation(conversationId: string): Promise<void> {
    if (activeConversationId.value === conversationId) {
      stopStreaming();
    }

    error.value = null;

    try {
      await chatClient.deleteConversation(conversationId);
      conversations.value = conversations.value.filter(
        (c) => c.conversationId !== conversationId,
      );

      if (activeConversationId.value === conversationId) {
        const next = conversations.value[0];
        if (next) {
          await selectConversation(next.conversationId);
        } else {
          activeConversationId.value = null;
          messages.value = [];
        }
      }
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Unable to delete conversation.';
    }
  }

  async function sendMessage(content: string, documentIds?: string[]): Promise<void> {
    const trimmed = content.trim();
    if (!trimmed || streaming.value) {
      return;
    }

    error.value = null;

    let conversationId = activeConversationId.value;
    if (!conversationId) {
      try {
        const created = await chatClient.createConversation();
        conversationId = created.conversationId;
        activeConversationId.value = conversationId;
        conversations.value = [created, ...conversations.value];
      } catch (err) {
        error.value = err instanceof Error ? err.message : 'Unable to create conversation.';
        return;
      }
    }

    const now = new Date().toISOString();
    const userMessage: ChatMessageViewModel = {
      messageId: crypto.randomUUID(),
      conversationId,
      role: 'user',
      content: trimmed,
      citations: [],
      createdAt: now,
    };

    const assistantId = crypto.randomUUID();
    const assistantMessage: ChatMessageViewModel = {
      messageId: assistantId,
      conversationId,
      role: 'assistant',
      content: '',
      citations: [],
      createdAt: now,
    };

    messages.value = [...messages.value, userMessage, assistantMessage];
    streaming.value = true;
    streamingMessageId.value = assistantId;

    const controller = new AbortController();
    streamAbortController = controller;

    try {
      await chatClient.streamMessage(
        conversationId,
        {
          content: trimmed,
          documentIds: documentIds?.length ? documentIds : undefined,
        },
        (event) => {
          const target = messages.value.find((m) => m.messageId === assistantId);
          if (!target) return;

          switch (event.type) {
            case 'token':
              target.content += event.content;
              break;
            case 'citations':
              target.citations = event.citations;
              break;
            case 'done': {
              const index = messages.value.findIndex((m) => m.messageId === assistantId);
              if (index !== -1) {
                messages.value.splice(index, 1, event.message);
              }
              break;
            }
            case 'error':
              error.value = event.message;
              break;
          }
        },
        controller.signal,
      );

      await refreshConversationsList();
    } catch (err) {
      if (!isAbortError(err)) {
        error.value = err instanceof Error ? err.message : 'Failed to generate response.';
        const target = messages.value.find((m) => m.messageId === assistantId);
        if (target && !target.content) {
          messages.value = messages.value.filter((m) => m.messageId !== assistantId);
        }
      }
    } finally {
      if (streamAbortController === controller) {
        streamAbortController = null;
      }
      streaming.value = false;
      streamingMessageId.value = null;
    }
  }

  onMounted(loadConversations);
  onBeforeUnmount(stopStreaming);

  return {
    conversations,
    activeConversationId,
    messages,
    loadingConversations,
    loadingMessages,
    streaming,
    streamingMessageId,
    error,
    clearError,
    loadConversations,
    selectConversation,
    startNewConversation,
    deleteConversation,
    sendMessage,
    stopStreaming,
  };
}
