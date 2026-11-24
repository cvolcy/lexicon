<script setup lang="ts">
import { computed, nextTick, ref, watch } from 'vue';
import type { CitationViewModel } from '@/api/chat';
import ChatCitationSlideover from '@/components/ChatCitationSlideover.vue';
import ChatConversationSidebar from '@/components/ChatConversationSidebar.vue';
import ChatHealthBanner from '@/components/ChatHealthBanner.vue';
import ChatMessageBubble from '@/components/ChatMessageBubble.vue';
import { useChat } from '@/composables/useChat';
import { useDocuments } from '@/composables/useDocuments';

const { documents } = useDocuments();
const {
  conversations,
  activeConversationId,
  messages,
  loadingConversations,
  loadingMessages,
  streaming,
  streamingMessageId,
  error,
  clearError,
  selectConversation,
  startNewConversation,
  deleteConversation,
  sendMessage,
  stopStreaming,
} = useChat();

const draft = ref('');
const selectedDocumentIds = ref<string[]>([]);
const mobileSidebarOpen = ref(false);
const messagesContainerRef = ref<HTMLElement | null>(null);

const citationOpen = ref(false);
const activeCitations = ref<CitationViewModel[]>([]);
const activeCitationIndex = ref(1);

const readyDocuments = computed(() => documents.value.filter((d) => d.status === 'Ready'));
const documentOptions = computed(() =>
  readyDocuments.value.map((d) => ({
    label: d.fileName,
    value: d.documentId,
  })),
);

const activeConversation = computed(
  () =>
    conversations.value.find((c) => c.conversationId === activeConversationId.value) ?? null,
);

const starterPrompts = [
  'Summarize the main topics covered in my documents.',
  'What key concepts, deadlines, or requirements are mentioned?',
  'List the most important definitions found in the indexed files.',
];

async function scrollToBottom(): Promise<void> {
  await nextTick();
  const el = messagesContainerRef.value;
  if (el) {
    el.scrollTop = el.scrollHeight;
  }
}

watch(
  () => [messages.value.length, messages.value[messages.value.length - 1]?.content],
  () => {
    void scrollToBottom();
  },
);

async function handleSend(): Promise<void> {
  const text = draft.value.trim();
  if (!text || streaming.value) return;
  draft.value = '';
  await sendMessage(text, selectedDocumentIds.value);
}

async function handleStarterPrompt(prompt: string): Promise<void> {
  if (streaming.value) return;
  await sendMessage(prompt, selectedDocumentIds.value);
}

function handleSelectCitation(index: number, citations: CitationViewModel[]): void {
  activeCitations.value = citations;
  activeCitationIndex.value = index;
  citationOpen.value = true;
}

async function handleSelectConversation(conversationId: string): Promise<void> {
  mobileSidebarOpen.value = false;
  await selectConversation(conversationId);
}

function handleNewConversation(): void {
  mobileSidebarOpen.value = false;
  startNewConversation();
}
</script>

<template>
  <div class="space-y-4">
    <ChatHealthBanner />

    <div
      class="flex h-[calc(100dvh-8.5rem)] min-h-[520px] overflow-hidden rounded-xl border border-default bg-default"
    >
      <aside class="hidden w-64 shrink-0 flex-col border-r border-default bg-elevated/30 lg:flex">
        <ChatConversationSidebar
          :conversations="conversations"
          :active-conversation-id="activeConversationId"
          :loading="loadingConversations"
          :disabled="streaming"
          @select="handleSelectConversation"
          @create="handleNewConversation"
          @delete="deleteConversation"
        />
      </aside>

      <USlideover
        v-model:open="mobileSidebarOpen"
        side="left"
        title="Conversations"
        :ui="{ body: 'p-0 sm:p-0' }"
      >
        <template #body>
          <ChatConversationSidebar
            :conversations="conversations"
            :active-conversation-id="activeConversationId"
            :loading="loadingConversations"
            :disabled="streaming"
            @select="handleSelectConversation"
            @create="handleNewConversation"
            @delete="deleteConversation"
          />
        </template>
      </USlideover>

      <div class="flex min-w-0 flex-1 flex-col">
        <header class="flex flex-wrap items-center justify-between gap-2 border-b border-default px-4 py-2.5">
          <div class="flex min-w-0 items-center gap-2">
            <UButton
              icon="i-lucide-panel-left"
              color="neutral"
              variant="ghost"
              size="xs"
              class="lg:hidden"
              aria-label="Open conversations"
              @click="mobileSidebarOpen = true"
            />
            <h1 class="truncate text-sm font-semibold text-highlighted">
              {{ activeConversation?.title ?? 'New Conversation' }}
            </h1>
          </div>

          <div class="flex items-center gap-2">
            <USelect
              v-model="selectedDocumentIds"
              :items="documentOptions"
              value-key="value"
              label-key="label"
              multiple
              placeholder="All indexed documents"
              icon="i-lucide-files"
              size="xs"
              class="w-48 sm:w-60"
              :disabled="documentOptions.length === 0 || streaming"
            />
            <UButton
              v-if="selectedDocumentIds.length > 0"
              icon="i-lucide-x"
              color="neutral"
              variant="ghost"
              size="xs"
              aria-label="Clear document filter"
              :disabled="streaming"
              @click="selectedDocumentIds = []"
            />
          </div>
        </header>

        <div v-if="error" class="px-4 pt-3">
          <UAlert
            color="error"
            variant="subtle"
            icon="i-lucide-circle-alert"
            :title="error"
            close
            @update:open="clearError"
          />
        </div>

        <div ref="messagesContainerRef" class="flex-1 overflow-y-auto px-4 py-6 sm:px-6">
          <div v-if="loadingMessages" class="space-y-4">
            <USkeleton class="ml-auto h-12 w-2/3 rounded-2xl" />
            <USkeleton class="h-24 w-4/5 rounded-2xl" />
            <USkeleton class="ml-auto h-10 w-1/2 rounded-2xl" />
          </div>

          <div
            v-else-if="messages.length === 0"
            class="mx-auto flex h-full max-w-xl flex-col items-center justify-center text-center"
          >
            <span
              class="flex size-12 items-center justify-center rounded-2xl bg-primary/10 text-primary ring-1 ring-inset ring-primary/20"
            >
              <UIcon name="i-lucide-message-square-text" class="size-6" />
            </span>

            <h2 class="mt-4 text-lg font-semibold text-highlighted">Ask Lexicon</h2>
            <p class="mt-1.5 text-sm text-muted">
              Ask questions grounded in your uploaded documents and click any citation chip to inspect
              the original passage.
            </p>

            <div v-if="readyDocuments.length === 0" class="mt-5">
              <UButton to="/documents" icon="i-lucide-upload" size="sm">
                Upload documents first
              </UButton>
            </div>

            <div v-else class="mt-6 grid w-full gap-2">
              <button
                v-for="prompt in starterPrompts"
                :key="prompt"
                type="button"
                class="flex cursor-pointer items-center justify-between gap-3 rounded-lg border border-default bg-elevated/30 px-3.5 py-2.5 text-left text-xs text-default transition-colors hover:border-primary/50 hover:bg-primary/5"
                :disabled="streaming"
                @click="handleStarterPrompt(prompt)"
              >
                <span>{{ prompt }}</span>
                <UIcon name="i-lucide-arrow-up-right" class="size-3.5 shrink-0 text-muted" />
              </button>
            </div>
          </div>

          <div v-else class="mx-auto max-w-3xl space-y-6">
            <ChatMessageBubble
              v-for="message in messages"
              :key="message.messageId"
              :message="message"
              :is-streaming="streaming && message.messageId === streamingMessageId"
              @select-citation="handleSelectCitation"
            />
          </div>
        </div>

        <footer class="border-t border-default bg-elevated/20 p-3 sm:p-4">
          <UChatPrompt
            v-model="draft"
            placeholder="Ask a question about your documents..."
            :disabled="streaming"
            @submit="handleSend"
          >
            <template #footer>
              <div class="flex w-full items-center justify-between gap-2">
                <span class="text-[11px] text-muted">
                  Enter to send · Shift+Enter for new line
                </span>

                <UButton
                  v-if="streaming"
                  type="button"
                  icon="i-lucide-square"
                  color="error"
                  variant="soft"
                  size="sm"
                  @click="stopStreaming"
                >
                  Stop
                </UButton>
                <UButton
                  v-else
                  type="submit"
                  icon="i-lucide-arrow-up"
                  size="sm"
                  :disabled="!draft.trim()"
                >
                  Send
                </UButton>
              </div>
            </template>
          </UChatPrompt>
        </footer>
      </div>
    </div>

    <ChatCitationSlideover
      v-model:open="citationOpen"
      v-model:active-index="activeCitationIndex"
      :citations="activeCitations"
    />
  </div>
</template>
