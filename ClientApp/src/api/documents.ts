export interface CollectionResponse<T> {
  items: T[];
  nextLink: string | null;
}

export interface DocumentViewModel {
  documentId: string;
  createdAt: string;
}
