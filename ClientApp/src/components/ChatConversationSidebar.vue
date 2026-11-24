<script setup lang="ts">
import type { ConversationSummaryViewModel } from '@/api/chat';
import { formatDateTime } from '@/utils/format';

defineProps<{
  conversations: ConversationSummaryViewModel[];
  activeConversationId: string | null;
  loading?: boolean;
  disabled?: boolean;
}>();

const emit = defineEmits<{
  select: [conversationId: string];
  create: [];
  delete: [conversationId: string];
}>();
</script>

<template>
  <div class="flex h-full flex-col">
    <div class="flex items-center justify-between border-b border-default px-3 py-2.5">
      <span class="text-xs font-semibold uppercase tracking-wide text-muted">Conversations</span>
      <UButton
        icon="i-lucide-square-pen"
        color="primary"
        variant="soft"
        size="xs"
        :disabled="disabled"
        @click="emit('create')"
      >
        New Chat
      </UButton>
    </div>

    <div class="flex-1 overflow-y-auto p-2">
      <div v-if="loading && conversations.length === 0" class="space-y-2 p-1">
        <USkeleton v-for="n in 4" :key="n" class="h-12 w-full rounded-lg" />
      </div>

      <div
        v-else-if="conversations.length === 0"
        class="px-2 py-8 text-center text-xs text-muted"
      >
        No conversations yet. Send a message to start one.
      </div>

      <ul v-else class="space-y-1">
        <li
          v-for="conversation in conversations"
          :key="conversation.conversationId"
          class="group flex items-center justify-between gap-2 rounded-lg px-2.5 py-2 transition-colors"
          :class="
            conversation.conversationId === activeConversationId
              ? 'bg-primary/10 text-highlighted ring-1 ring-inset ring-primary/25'
              : 'text-default hover:bg-elevated'
          "
        >
          <button
            type="button"
            class="min-w-0 flex-1 cursor-pointer text-left"
            :disabled="disabled"
            @click="emit('select', conversation.conversationId)"
          >
            <div class="truncate text-sm font-medium">{{ conversation.title }}</div>
            <div class="mt-0.5 text-[11px] text-muted">
              {{ conversation.messageCount }} {{ conversation.messageCount === 1 ? 'msg' : 'msgs' }} ·
              {{ formatDateTime(conversation.updatedAt) }}
            </div>
          </button>

          <UButton
            icon="i-lucide-trash-2"
            color="error"
            variant="ghost"
            size="xs"
            class="shrink-0 opacity-0 transition-opacity group-hover:opacity-100 focus-visible:opacity-100"
            :disabled="disabled"
            :aria-label="`Delete conversation ${conversation.title}`"
            @click.stop="emit('delete', conversation.conversationId)"
          />
        </li>
      </ul>
    </div>
  </div>
</template>
