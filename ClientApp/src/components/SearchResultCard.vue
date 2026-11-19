<script setup lang="ts">
import { useClipboard } from '@vueuse/core';
import { getDocumentDownloadUrl } from '@/api/documents';
import type { SearchResultViewModel } from '@/api/search';

const props = defineProps<{
  result: SearchResultViewModel;
  rank: number;
}>();

const { copy, copied } = useClipboard();
const toast = useToast();

async function copySnippet(): Promise<void> {
  await copy(props.result.text);
  toast.add({
    title: 'Passage copied to clipboard',
    icon: 'i-lucide-check',
    color: 'success',
  });
}
</script>

<template>
  <UCard>
    <template #header>
      <div class="flex flex-wrap items-center justify-between gap-3">
        <div class="flex min-w-0 flex-wrap items-center gap-2">
          <UBadge
            :label="`#${rank}`"
            color="primary"
            variant="subtle"
            size="sm"
            class="font-mono"
          />
          <span class="truncate font-medium text-highlighted">{{ result.documentFileName }}</span>
          <UBadge
            v-if="result.pageNumber !== null"
            :label="`Page ${result.pageNumber}`"
            color="neutral"
            variant="outline"
            size="xs"
          />
          <UBadge
            v-if="result.section"
            :label="result.section"
            color="neutral"
            variant="soft"
            size="xs"
            class="max-w-48 truncate"
          />
          <span class="font-mono text-xs text-muted">Chunk #{{ result.chunkIndex + 1 }}</span>
        </div>

        <div class="flex shrink-0 flex-wrap items-center gap-1.5">
          <UBadge
            :label="`RRF ${result.score.toFixed(4)}`"
            color="primary"
            variant="subtle"
            size="xs"
            class="font-mono"
          />
          <UBadge
            v-if="result.vectorScore !== null"
            :label="`Vec ${result.vectorScore.toFixed(3)}`"
            color="success"
            variant="outline"
            size="xs"
            class="font-mono"
          />
          <UBadge
            v-if="result.keywordScore !== null"
            :label="`FTS ${result.keywordScore.toFixed(2)}`"
            color="info"
            variant="outline"
            size="xs"
            class="font-mono"
          />

          <UTooltip :text="copied ? 'Copied' : 'Copy passage'">
            <UButton
              :icon="copied ? 'i-lucide-check' : 'i-lucide-copy'"
              color="neutral"
              variant="ghost"
              size="xs"
              aria-label="Copy passage"
              @click="copySnippet"
            />
          </UTooltip>
          <UTooltip text="Download source document">
            <UButton
              icon="i-lucide-download"
              color="neutral"
              variant="ghost"
              size="xs"
              :to="getDocumentDownloadUrl(result.documentId)"
              external
              :download="result.documentFileName"
              :aria-label="`Download ${result.documentFileName}`"
            />
          </UTooltip>
        </div>
      </div>
    </template>

    <p class="whitespace-pre-wrap text-sm leading-relaxed text-default">{{ result.text }}</p>
  </UCard>
</template>
