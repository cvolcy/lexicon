export type InlineNode =
  | { type: 'text'; text: string }
  | { type: 'bold'; children: InlineNode[] }
  | { type: 'italic'; children: InlineNode[] }
  | { type: 'code'; text: string }
  | { type: 'citation'; index: number };

export type TableAlignment = 'left' | 'center' | 'right' | null;

export type MarkdownBlock =
  | { type: 'heading'; level: 1 | 2 | 3; inlines: InlineNode[] }
  | { type: 'code'; language: string; content: string }
  | { type: 'blockquote'; inlines: InlineNode[] }
  | { type: 'list'; ordered: boolean; items: InlineNode[][] }
  | {
      type: 'table';
      headers: InlineNode[][];
      alignments: TableAlignment[];
      rows: InlineNode[][][];
    }
  | { type: 'paragraph'; inlines: InlineNode[] };

const INLINE_PATTERN_SOURCE = /(\*\*[^*]+\*\*|`[^`]+`|\*[^*\s][^*]*?\*|\[(\d+)\])/.source;
const MAX_INLINE_DEPTH = 4;

export function parseInlineMarkdown(
  input: string,
  availableCitations: ReadonlySet<number>,
  depth = 0,
): InlineNode[] {
  if (!input) {
    return [];
  }

  if (depth >= MAX_INLINE_DEPTH) {
    return [{ type: 'text', text: input }];
  }

  const nodes: InlineNode[] = [];
  const pattern = new RegExp(INLINE_PATTERN_SOURCE, 'g');
  let cursor = 0;
  let match: RegExpExecArray | null;

  while ((match = pattern.exec(input)) !== null) {
    const token = match[0];
    if (!token) {
      pattern.lastIndex++;
      continue;
    }

    if (match.index > cursor) {
      nodes.push({ type: 'text', text: input.slice(cursor, match.index) });
    }

    const matchEnd = match.index + token.length;

    if (token.startsWith('**') && token.endsWith('**') && token.length > 4) {
      nodes.push({
        type: 'bold',
        children: parseInlineMarkdown(token.slice(2, -2), availableCitations, depth + 1),
      });
    } else if (token.startsWith('`') && token.endsWith('`')) {
      nodes.push({ type: 'code', text: token.slice(1, -1) });
    } else if (token.startsWith('*') && token.endsWith('*') && token.length > 2) {
      nodes.push({
        type: 'italic',
        children: parseInlineMarkdown(token.slice(1, -1), availableCitations, depth + 1),
      });
    } else if (match[2]) {
      const index = Number(match[2]);
      if (availableCitations.has(index)) {
        nodes.push({ type: 'citation', index });
      } else {
        nodes.push({ type: 'text', text: token });
      }
    } else {
      nodes.push({ type: 'text', text: token });
    }

    cursor = matchEnd;
  }

  if (cursor < input.length) {
    nodes.push({ type: 'text', text: input.slice(cursor) });
  }

  return nodes;
}

function findClosingBackticks(input: string, fromIndex: number, tickCount: number): number {
  let i = fromIndex;
  while (i < input.length) {
    if (input[i] === '`') {
      let count = 1;
      while (i + count < input.length && input[i + count] === '`') {
        count++;
      }
      if (count === tickCount) {
        return i;
      }
      i += count;
      continue;
    }
    i++;
  }
  return -1;
}

function splitTableRow(line: string): string[] | null {
  const trimmed = line.trim();
  if (!trimmed) {
    return null;
  }

  const cells: string[] = [];
  let current = '';
  let hasPipe = false;
  let endedWithPipe = false;
  let codeTicks = 0;
  let i = 0;

  while (i < trimmed.length) {
    const ch = trimmed[i]!;

    if (ch === '\\' && i + 1 < trimmed.length) {
      const next = trimmed[i + 1]!;
      if (next === '\\') {
        current += '\\\\';
        endedWithPipe = false;
        i += 2;
        continue;
      }
      if (next === '|') {
        current += '|';
        endedWithPipe = false;
        i += 2;
        continue;
      }
    }

    if (ch === '`') {
      let tickCount = 1;
      while (i + tickCount < trimmed.length && trimmed[i + tickCount] === '`') {
        tickCount++;
      }

      if (codeTicks === 0) {
        if (findClosingBackticks(trimmed, i + tickCount, tickCount) !== -1) {
          codeTicks = tickCount;
        }
      } else if (codeTicks === tickCount) {
        codeTicks = 0;
      }

      current += trimmed.slice(i, i + tickCount);
      endedWithPipe = false;
      i += tickCount;
      continue;
    }

    if (ch === '|' && codeTicks === 0) {
      hasPipe = true;
      endedWithPipe = true;
      if (i > 0) {
        cells.push(current.trim());
        current = '';
      }
      i++;
      continue;
    }

    current += ch;
    endedWithPipe = false;
    i++;
  }

  if (!hasPipe) {
    return null;
  }

  if (!endedWithPipe) {
    cells.push(current.trim());
  }

  return cells.length > 0 ? cells : null;
}

function parseTableAlignments(line: string): TableAlignment[] | null {
  const cells = splitTableRow(line);
  if (!cells) {
    return null;
  }

  const alignments: TableAlignment[] = [];
  for (const cell of cells) {
    if (!/^:?-{1,}:?$/.test(cell)) {
      return null;
    }

    const startsWithColon = cell.startsWith(':');
    const endsWithColon = cell.endsWith(':');
    if (startsWithColon && endsWithColon) {
      alignments.push('center');
    } else if (startsWithColon) {
      alignments.push('left');
    } else if (endsWithColon) {
      alignments.push('right');
    } else {
      alignments.push(null);
    }
  }

  return alignments;
}

function parseTableHeader(
  lines: string[],
  index: number,
): { headerCells: string[]; alignments: TableAlignment[] } | null {
  if (index + 1 >= lines.length) {
    return null;
  }

  const headerCells = splitTableRow(lines[index]!);
  if (!headerCells) {
    return null;
  }

  const alignments = parseTableAlignments(lines[index + 1]!);
  if (!alignments || alignments.length !== headerCells.length) {
    return null;
  }

  return { headerCells, alignments };
}

function isNonTableBlockStart(trimmed: string): boolean {
  return (
    trimmed.startsWith('```') ||
    /^#{1,3}\s+\S/.test(trimmed) ||
    trimmed.startsWith('> ') ||
    /^[-*]\s+/.test(trimmed) ||
    /^\d+\.\s+/.test(trimmed)
  );
}

export function parseMarkdown(
  markdown: string,
  availableCitations: ReadonlySet<number>,
): MarkdownBlock[] {
  const lines = markdown.replace(/\r\n/g, '\n').split('\n');
  const blocks: MarkdownBlock[] = [];
  let i = 0;

  while (i < lines.length) {
    const line = lines[i]!;
    const trimmed = line.trim();

    if (!trimmed) {
      i++;
      continue;
    }

    // Fenced code block
    if (trimmed.startsWith('```')) {
      const language = trimmed.slice(3).trim();
      const codeLines: string[] = [];
      i++;

      while (i < lines.length && !lines[i]!.trim().startsWith('```')) {
        codeLines.push(lines[i]!);
        i++;
      }

      if (i < lines.length) {
        i++;
      }

      blocks.push({ type: 'code', language, content: codeLines.join('\n') });
      continue;
    }

    // Headings (#, ##, ###)
    const headingMatch = /^(#{1,3})\s+(.+)$/.exec(trimmed);
    if (headingMatch) {
      const level = headingMatch[1]!.length as 1 | 2 | 3;
      blocks.push({
        type: 'heading',
        level,
        inlines: parseInlineMarkdown(headingMatch[2]!, availableCitations),
      });
      i++;
      continue;
    }

    // Blockquote
    if (trimmed.startsWith('> ')) {
      const quoteLines: string[] = [];
      while (i < lines.length && lines[i]!.trim().startsWith('> ')) {
        quoteLines.push(lines[i]!.trim().slice(2));
        i++;
      }
      blocks.push({
        type: 'blockquote',
        inlines: parseInlineMarkdown(quoteLines.join(' '), availableCitations),
      });
      continue;
    }

    // Unordered list
    if (/^[-*]\s+/.test(trimmed)) {
      const items: InlineNode[][] = [];
      while (i < lines.length && /^[-*]\s+/.test(lines[i]!.trim())) {
        items.push(
          parseInlineMarkdown(lines[i]!.trim().replace(/^[-*]\s+/, ''), availableCitations),
        );
        i++;
      }
      blocks.push({ type: 'list', ordered: false, items });
      continue;
    }

    // Ordered list
    if (/^\d+\.\s+/.test(trimmed)) {
      const items: InlineNode[][] = [];
      while (i < lines.length && /^\d+\.\s+/.test(lines[i]!.trim())) {
        items.push(
          parseInlineMarkdown(lines[i]!.trim().replace(/^\d+\.\s+/, ''), availableCitations),
        );
        i++;
      }
      blocks.push({ type: 'list', ordered: true, items });
      continue;
    }

    // GFM table
    const tableHeader = parseTableHeader(lines, i);
    if (tableHeader) {
      const columnCount = tableHeader.headerCells.length;
      const headers = tableHeader.headerCells.map((cell) =>
        parseInlineMarkdown(cell, availableCitations),
      );
      const rows: InlineNode[][][] = [];
      i += 2;

      while (i < lines.length) {
        const rowTrimmed = lines[i]!.trim();
        if (!rowTrimmed) {
          break;
        }
        if (!rowTrimmed.startsWith('|') && isNonTableBlockStart(rowTrimmed)) {
          break;
        }

        const rawCells = splitTableRow(rowTrimmed);
        if (!rawCells) {
          break;
        }

        const row: InlineNode[][] = [];
        for (let col = 0; col < columnCount; col++) {
          const cellText = col < rawCells.length ? rawCells[col]! : '';
          row.push(parseInlineMarkdown(cellText, availableCitations));
        }
        rows.push(row);
        i++;
      }

      blocks.push({
        type: 'table',
        headers,
        alignments: tableHeader.alignments,
        rows,
      });
      continue;
    }

    // Paragraph (collect consecutive plain lines)
    const paragraphLines: string[] = [];
    while (i < lines.length) {
      const current = lines[i]!.trim();
      if (!current || isNonTableBlockStart(current) || parseTableHeader(lines, i) !== null) {
        break;
      }
      paragraphLines.push(current);
      i++;
    }

    if (paragraphLines.length === 0) {
      paragraphLines.push(trimmed);
      i++;
    }

    blocks.push({
      type: 'paragraph',
      inlines: parseInlineMarkdown(paragraphLines.join(' '), availableCitations),
    });
  }

  return blocks;
}
