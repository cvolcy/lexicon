export interface SearchRequest {
  query: string;
  topK?: number;
  documentIds?: string[];
}

export interface SearchResultViewModel {
  chunkId: string;
  documentId: string;
  documentFileName: string;
  chunkIndex: number;
  text: string;
  pageNumber: number | null;
  section: string | null;
  score: number;
  vectorScore: number | null;
  keywordScore: number | null;
}
