<script setup lang="ts">
import { computed, createTextVNode, h, type VNode } from 'vue';
import { useClipboard } from '@vueuse/core';
import type { CitationViewModel } from '@/api/chat';
import {
  parseMarkdown,
  type HeadingLevel,
  type InlineNode,
  type TableAlignment,
} from '@/utils/markdown';

const props = defineProps<{
  content: string;
  citations: CitationViewModel[];
}>();

const emit = defineEmits<{
  selectCitation: [index: number];
}>();

const { copy } = useClipboard();
const toast = useToast();

const citationSet = computed(() => new Set(props.citations.map((c) => c.index)));
const blocks = computed(() => parseMarkdown(props.content, citationSet.value));

async function copyCode(code: string): Promise<void> {
  await copy(code);
  toast.add({
    title: 'Code copied to clipboard',
    icon: 'i-lucide-check',
    color: 'success',
  });
}

function renderInlines(nodes: InlineNode[]): (VNode | string)[] {
  return nodes.map((node, idx) => {
    switch (node.type) {
      case 'text':
        return createTextVNode(node.text);
      case 'code':
        return h(
          'code',
          {
            key: idx,
            class: 'rounded bg-elevated px-1.5 py-0.5 font-mono text-[0.85em] text-highlighted',
          },
          node.text,
        );
      case 'bold':
        return h(
          'strong',
          { key: idx, class: 'font-semibold text-highlighted' },
          renderInlines(node.children),
        );
      case 'italic':
        return h('em', { key: idx }, renderInlines(node.children));
      case 'citation':
        return h(
          'button',
          {
            key: idx,
            type: 'button',
            class:
              'mx-0.5 inline-flex cursor-pointer items-center rounded bg-primary/15 px-1.5 py-0.5 align-baseline font-mono text-[11px] font-semibold text-primary ring-1 ring-inset ring-primary/25 transition-colors hover:bg-primary/25',
            'aria-label': `View citation ${node.index}`,
            onClick: () => emit('selectCitation', node.index),
          },
          `[${node.index}]`,
        );
    }
  });
}

const InlineContent = (p: { nodes: InlineNode[] }) => renderInlines(p.nodes);

function headingClass(level: HeadingLevel): string {
  switch (level) {
    case 1:
      return 'text-base font-semibold tracking-tight text-highlighted';
    case 2:
      return 'text-sm font-semibold tracking-tight text-highlighted';
    case 3:
      return 'text-sm font-medium text-highlighted';
    case 4:
      return 'text-sm font-medium text-default';
    default:
      return 'text-xs font-semibold uppercase tracking-wide text-muted';
  }
}

function alignmentClass(alignment: TableAlignment): string {
  switch (alignment) {
    case 'center':
      return 'text-center';
    case 'right':
      return 'text-right';
    default:
      return 'text-left';
  }
}
</script>

<template>
  <div class="space-y-2.5 text-sm leading-relaxed text-default">
    <template v-for="(block, index) in blocks" :key="index">
      <component
        :is="`h${block.level}`"
        v-if="block.type === 'heading'"
        :class="headingClass(block.level)"
      >
        <InlineContent :nodes="block.inlines" />
      </component>

      <hr v-else-if="block.type === 'hr'" class="my-2 border-default" />

      <div
        v-else-if="block.type === 'code'"
        class="overflow-hidden rounded-lg border border-default bg-elevated/60"
      >
        <div class="flex items-center justify-between border-b border-default px-3 py-1.5 text-xs text-muted">
          <span class="font-mono">{{ block.language || 'text' }}</span>
          <UButton
            icon="i-lucide-copy"
            color="neutral"
            variant="ghost"
            size="xs"
            aria-label="Copy code"
            @click="copyCode(block.content)"
          />
        </div>
        <pre class="overflow-x-auto p-3 font-mono text-xs leading-relaxed text-highlighted"><code>{{ block.content }}</code></pre>
      </div>

      <blockquote
        v-else-if="block.type === 'blockquote'"
        class="border-l-2 border-primary/40 pl-3 text-muted italic"
      >
        <InlineContent :nodes="block.inlines" />
      </blockquote>

      <component
        :is="block.ordered ? 'ol' : 'ul'"
        v-else-if="block.type === 'list'"
        class="space-y-1.5 pl-5"
        :class="block.ordered ? 'list-decimal' : 'list-disc'"
      >
        <li v-for="(item, itemIdx) in block.items" :key="itemIdx">
          <InlineContent :nodes="item" />
        </li>
      </component>

      <div
        v-else-if="block.type === 'table'"
        class="overflow-x-auto rounded-lg border border-default"
      >
        <table class="w-full border-collapse text-left text-xs sm:text-sm">
          <thead class="border-b border-default bg-elevated/60 text-highlighted">
            <tr>
              <th
                v-for="(header, colIdx) in block.headers"
                :key="colIdx"
                scope="col"
                class="px-3 py-2 font-semibold"
                :class="alignmentClass(block.alignments[colIdx] ?? null)"
              >
                <InlineContent :nodes="header" />
              </th>
            </tr>
          </thead>
          <tbody v-if="block.rows.length > 0" class="divide-y divide-default">
            <tr
              v-for="(row, rowIdx) in block.rows"
              :key="rowIdx"
              class="transition-colors hover:bg-elevated/30"
            >
              <td
                v-for="(cell, colIdx) in row"
                :key="colIdx"
                class="px-3 py-2 align-top text-default"
                :class="alignmentClass(block.alignments[colIdx] ?? null)"
              >
                <InlineContent :nodes="cell" />
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <p v-else-if="block.type === 'paragraph'">
        <InlineContent :nodes="block.inlines" />
      </p>
    </template>
  </div>
</template>
