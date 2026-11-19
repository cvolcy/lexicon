export interface CollectionResponse<T> {
  items: T[];
  nextLink: string | null;
}

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  errors?: Record<string, string[]>;
}
