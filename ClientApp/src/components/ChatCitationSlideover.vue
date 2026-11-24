<script setup lang="ts">
import { computed } from 'vue';
import { useClipboard } from '@vueuse/core';
import type { CitationViewModel } from '@/api/chat';
import { getDocumentDownloadUrl } from '@/api/documents';

const props = defineProps<{
  open: boolean;
  citations: CitationViewModel[];
  activeIndex: number;
}>();

const emit = defineEmits<{
  'update:open': [value: boolean];
  'update:activeIndex': [value: number];
}>();

const { copy, copied } = useClipboard();
const toast = useToast();

const activeCitation = computed(
  () => props.citations.find((c) => c.index === props.activeIndex) ?? props.citations[0] ?? null,
);

async function copyPassage(): Promise<void> {
  if (!activeCitation.value) return;
  await copy(activeCitation.value.text);
  toast.add({
    title: 'Passage copied to clipboard',
    icon: 'i-lucide-check',
    color: 'success',
  });
}
</script>

<template>
  <USlideover
    side="right"
    :open="open"
    title="Source Passage"
    :description="activeCitation ? activeCitation.documentFileName : undefined"
    @update:open="emit('update:open', $event)"
  >
    <template #body>
      <div v-if="activeCitation" class="space-y-5">
        <div v-if="citations.length > 1" class="flex flex-wrap gap-1.5">
          <UButton
            v-for="citation in citations"
            :key="citation.chunkId"
            size="xs"
            class="font-mono"
            :color="citation.index === activeCitation.index ? 'primary' : 'neutral'"
            :variant="citation.index === activeCitation.index ? 'soft' : 'outline'"
            @click="emit('update:activeIndex', citation.index)"
          >
            [{{ citation.index }}]
          </UButton>
        </div>

        <div class="flex flex-wrap items-center gap-2">
          <UBadge
            :label="`Citation [${activeCitation.index}]`"
            color="primary"
            variant="subtle"
            size="sm"
            class="font-mono"
          />
          <UBadge
            v-if="activeCitation.pageNumber !== null"
            :label="`Page ${activeCitation.pageNumber}`"
            color="neutral"
            variant="outline"
            size="sm"
          />
          <UBadge
            v-if="activeCitation.section"
            :label="activeCitation.section"
            color="neutral"
            variant="soft"
            size="sm"
          />
          <UBadge
            :label="`Chunk #${activeCitation.chunkIndex + 1}`"
            color="neutral"
            variant="outline"
            size="sm"
            class="font-mono"
          />
          <UBadge
            :label="`Score ${activeCitation.score.toFixed(4)}`"
            color="success"
            variant="subtle"
            size="sm"
            class="font-mono"
          />
        </div>

        <div class="rounded-lg border border-default bg-elevated/40 p-4">
          <p class="whitespace-pre-wrap text-sm leading-relaxed text-default">
            {{ activeCitation.text }}
          </p>
        </div>

        <div class="flex flex-wrap gap-2">
          <UButton
            :icon="copied ? 'i-lucide-check' : 'i-lucide-copy'"
            color="neutral"
            variant="outline"
            size="sm"
            @click="copyPassage"
          >
            {{ copied ? 'Copied' : 'Copy Passage' }}
          </UButton>
          <UButton
            icon="i-lucide-download"
            color="primary"
            variant="soft"
            size="sm"
            :to="getDocumentDownloadUrl(activeCitation.documentId)"
            external
            :download="activeCitation.documentFileName"
          >
            Download Original
          </UButton>
        </div>
      </div>
    </template>
  </USlideover>
</template>
