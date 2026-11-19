export type DocumentStatus = 'Pending' | 'Processing' | 'Ready' | 'Failed';

export interface DocumentViewModel {
  documentId: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  status: DocumentStatus;
  error: string | null;
  chunkCount: number;
  createdAt: string;
  updatedAt: string;
}

export function getDocumentDownloadUrl(documentId: string): string {
  return `/api/documents/${encodeURIComponent(documentId)}/file`;
}
