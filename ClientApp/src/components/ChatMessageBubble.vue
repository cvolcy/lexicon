<script setup lang="ts">
import { useClipboard } from '@vueuse/core';
import type { ChatMessageViewModel, CitationViewModel } from '@/api/chat';
import ChatMessageContent from '@/components/ChatMessageContent.vue';
import { formatDateTime } from '@/utils/format';

const props = defineProps<{
  message: ChatMessageViewModel;
  isStreaming?: boolean;
}>();

const emit = defineEmits<{
  selectCitation: [citationIndex: number, citations: CitationViewModel[]];
}>();

const { copy, copied } = useClipboard();
const toast = useToast();

async function copyMessage(): Promise<void> {
  await copy(props.message.content);
  toast.add({
    title: 'Response copied to clipboard',
    icon: 'i-lucide-check',
    color: 'success',
  });
}
</script>

<template>
  <div v-if="message.role === 'user'" class="flex justify-end">
    <div class="max-w-[85%] space-y-1 sm:max-w-[75%]">
      <div
        class="rounded-2xl rounded-tr-sm bg-primary/10 px-4 py-2.5 text-sm leading-relaxed text-highlighted ring-1 ring-inset ring-primary/20"
      >
        <p class="whitespace-pre-wrap">{{ message.content }}</p>
      </div>
      <div class="text-right text-[11px] text-muted">
        {{ formatDateTime(message.createdAt) }}
      </div>
    </div>
  </div>

  <div v-else class="flex items-start gap-3">
    <span
      class="mt-0.5 flex size-8 shrink-0 items-center justify-center rounded-lg bg-primary/10 text-primary ring-1 ring-inset ring-primary/20"
    >
      <UIcon
        :name="isStreaming ? 'i-lucide-loader-circle' : 'i-lucide-sparkles'"
        class="size-4"
        :class="{ 'animate-spin': isStreaming }"
      />
    </span>

    <div class="min-w-0 flex-1 space-y-2.5">
      <div v-if="isStreaming && !message.content" class="flex items-center gap-1.5 py-2">
        <span class="size-2 animate-bounce rounded-full bg-primary/60 [animation-delay:-0.3s]" />
        <span class="size-2 animate-bounce rounded-full bg-primary/60 [animation-delay:-0.15s]" />
        <span class="size-2 animate-bounce rounded-full bg-primary/60" />
      </div>

      <template v-else>
        <ChatMessageContent
          :content="message.content"
          :citations="message.citations"
          @select-citation="emit('selectCitation', $event, message.citations)"
        />
        <span
          v-if="isStreaming"
          class="inline-block h-4 w-1.5 animate-pulse rounded-xs bg-primary align-middle"
          aria-hidden="true"
        />
      </template>

      <div v-if="message.citations.length > 0" class="flex flex-wrap items-center gap-1.5 pt-1">
        <span class="mr-1 text-[11px] font-medium uppercase tracking-wide text-muted">Sources</span>
        <button
          v-for="citation in message.citations"
          :key="citation.chunkId"
          type="button"
          class="inline-flex max-w-60 cursor-pointer items-center gap-1.5 rounded-md border border-default bg-elevated/50 px-2 py-1 text-xs text-default transition-colors hover:border-primary/50 hover:bg-primary/5"
          @click="emit('selectCitation', citation.index, message.citations)"
        >
          <span class="font-mono font-semibold text-primary">[{{ citation.index }}]</span>
          <span class="truncate">{{ citation.documentFileName }}</span>
          <span v-if="citation.pageNumber !== null" class="shrink-0 text-muted">
            p.{{ citation.pageNumber }}
          </span>
        </button>
      </div>

      <div v-if="!isStreaming && message.content" class="flex items-center gap-2 pt-0.5">
        <UTooltip :text="copied ? 'Copied' : 'Copy response'">
          <UButton
            :icon="copied ? 'i-lucide-check' : 'i-lucide-copy'"
            color="neutral"
            variant="ghost"
            size="xs"
            aria-label="Copy response"
            @click="copyMessage"
          />
        </UTooltip>
        <span class="text-[11px] text-muted">{{ formatDateTime(message.createdAt) }}</span>
      </div>
    </div>
  </div>
</template>
