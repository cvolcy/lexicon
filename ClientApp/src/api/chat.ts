export type ChatRole = 'user' | 'assistant';

export interface CitationViewModel {
  index: number;
  chunkId: string;
  documentId: string;
  documentFileName: string;
  chunkIndex: number;
  text: string;
  pageNumber: number | null;
  section: string | null;
  score: number;
}

export interface ChatMessageViewModel {
  messageId: string;
  conversationId: string;
  role: ChatRole;
  content: string;
  citations: CitationViewModel[];
  createdAt: string;
}

export interface ConversationSummaryViewModel {
  conversationId: string;
  title: string;
  messageCount: number;
  createdAt: string;
  updatedAt: string;
}

export interface ConversationDetailViewModel extends ConversationSummaryViewModel {
  messages: ChatMessageViewModel[];
}

export interface SendMessageRequest {
  content: string;
  documentIds?: string[];
}

export type ChatStreamEvent =
  | { type: 'token'; content: string }
  | { type: 'citations'; citations: CitationViewModel[] }
  | { type: 'done'; message: ChatMessageViewModel }
  | { type: 'error'; message: string };

export interface ChatClient {
  listConversations(signal?: AbortSignal): Promise<ConversationSummaryViewModel[]>;
  getConversation(conversationId: string, signal?: AbortSignal): Promise<ConversationDetailViewModel>;
  createConversation(title?: string, signal?: AbortSignal): Promise<ConversationSummaryViewModel>;
  deleteConversation(conversationId: string, signal?: AbortSignal): Promise<void>;
  streamMessage(
    conversationId: string,
    request: SendMessageRequest,
    onEvent: (event: ChatStreamEvent) => void,
    signal?: AbortSignal,
  ): Promise<void>;
}
