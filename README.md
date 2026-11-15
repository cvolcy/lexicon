# Lexicon

Lexicon is a **RAG (Retrieval-Augmented Generation) document assistant**. Upload your documents, and ask questions in natural language. Answers are generated from your own content and come with citations back to the source passages.

## Goals

- Ground every answer in the user's documents, with verifiable citations.
- Keep ingestion simple: upload a file, see it become searchable.
- Keep the stack small and swappable (LLM, embedding model, vector store behind interfaces).
- Run locally with one command; deploy as a single ASP.NET Core app that serves the SPA.
