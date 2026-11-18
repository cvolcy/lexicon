# Lexicon

Lexicon is a **RAG (Retrieval-Augmented Generation) document assistant**. Upload your documents, and ask questions in natural language. Answers are generated from your own content and come with citations back to the source passages.

## Goals

- Ground every answer in the user's documents, with verifiable citations.
- Keep ingestion simple: upload a file, see it become searchable.
- Keep the stack small and swappable (LLM, embedding model, vector store behind interfaces).
- Run locally with one command; deploy as a single ASP.NET Core app that serves the SPA.

## Current State

The foundation, database persistence, document metadata CRUD API, and Vue 3 + Nuxt UI frontend are implemented and wired end-to-end:

### Backend (ASP.NET Core on .NET 10)
- **Persistence (`Infrastructure/Persistence`)**:
  - `LexiconDbContext` registered via `AddLexiconDbContext(builder.Configuration)`, selecting the database provider from `Database:Provider` (currently `Sqlite`)
- **Models & Contracts (`Models/`)**:
  - `Document`
- **API Endpoints (`Controllers/`)**:
  - `GET /api/health` — Returns service status, active environment, and UTC timestamp.
  - `GET /api/documents` — Lists documents newest-first inside `CollectionResponse<DocumentViewModel>`.
  - `POST /api/documents` — Accepts a `multipart/form-data` upload (`IFormFile file`), validates non-empty files, persists the `Document` record with status `Pending`, and returns `201 Created`.
  - `GET /api/documents/{documentId:guid}` — Returns a single `DocumentViewModel` or `404 Not Found`.
  - `DELETE /api/documents/{documentId:guid}` — Deletes a document record and returns `204 No Content` or `404 Not Found`.
  - Unmatched `/api/{**slug}` routes return `404 Not Found` instead of falling through to the SPA fallback.

### Frontend (`ClientApp/` — Vue 3 + TypeScript + Vite)
- **UI & Libraries**: Nuxt UI v4 (`@nuxt/ui`), Tailwind CSS v4, Lucide icons (`@iconify-json/lucide`), Vue Router 4, and VueUse (`@vueuse/core`).
- **API Layer**:
  - Shared `useApi` client (`src/api/client.ts`) built on VueUse `createFetch` with RFC 9457 `ProblemDetails` error parsing.
  - `useDocuments` and `useHealth` composables in `src/composables/`.
