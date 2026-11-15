<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref } from 'vue';

interface HealthResponse {
  status: string;
  environment: string;
  timestampUtc: string;
}

const health = ref<HealthResponse | null>(null);
const error = ref<string | null>(null);
const loading = ref(false);

let abortController: AbortController | null = null;

async function fetchHealth(): Promise<void> {
  abortController?.abort();
  abortController = new AbortController();

  loading.value = true;
  error.value = null;

  try {
    const response = await fetch('/api/health', {
      headers: { Accept: 'application/json' },
      signal: abortController.signal,
    });

    if (!response.ok) {
      throw new Error(`HTTP ${response.status} ${response.statusText}`);
    }

    health.value = (await response.json()) as HealthResponse;
  } catch (err) {
    if (err instanceof DOMException && err.name === 'AbortError') {
      return;
    }
    error.value = err instanceof Error ? err.message : 'Unable to reach backend API.';
  } finally {
    loading.value = false;
  }
}

onMounted(fetchHealth);
onBeforeUnmount(() => abortController?.abort());
</script>

<template>
  <div class="layout">
    <header class="header">
      <span class="brand">Lexicon</span>
      <span class="badge">Vue 3 + ASP.NET Core</span>
    </header>

    <main>
      <section class="card">
        <h1>System Status</h1>
        <p class="subtitle">
          Served via <strong>SpaProxy</strong> in Development and prebuilt static assets in Release.
        </p>

        <div class="status-box">
          <div class="status-header">
            <span>Backend Health (<code>/api/health</code>)</span>
            <button type="button" class="btn" :disabled="loading" @click="fetchHealth">
              {{ loading ? 'Checking...' : 'Refresh' }}
            </button>
          </div>

          <p v-if="error" class="error" role="alert">{{ error }}</p>
          <dl v-else-if="health" class="details">
            <div>
              <dt>Status</dt>
              <dd>{{ health.status }}</dd>
            </div>
            <div>
              <dt>Environment</dt>
              <dd>{{ health.environment }}</dd>
            </div>
            <div>
              <dt>Timestamp (UTC)</dt>
              <dd>{{ health.timestampUtc }}</dd>
            </div>
          </dl>
        </div>
      </section>
    </main>
  </div>
</template>

<style>
:root {
  --bg: #f8fafc;
  --surface: #ffffff;
  --text: #0f172a;
  --muted: #64748b;
  --border: #e2e8f0;
  --accent: #059669;
}

@media (prefers-color-scheme: dark) {
  :root {
    --bg: #0f172a;
    --surface: #1e293b;
    --text: #f1f5f9;
    --muted: #94a3b8;
    --border: #334155;
    --accent: #34d399;
  }
}

*,
*::before,
*::after {
  box-sizing: border-box;
}

body {
  margin: 0;
  font-family: system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
  background-color: var(--bg);
  color: var(--text);
  -webkit-font-smoothing: antialiased;
}
</style>

<style scoped>
.layout {
  max-width: 820px;
  margin: 0 auto;
  padding: 2rem 1.5rem;
}

.header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding-bottom: 1rem;
  margin-bottom: 2rem;
  border-bottom: 1px solid var(--border);
}

.brand {
  font-size: 1.25rem;
  font-weight: 700;
  letter-spacing: -0.02em;
}

.badge {
  font-size: 0.75rem;
  font-weight: 600;
  color: var(--accent);
  border: 1px solid var(--border);
  border-radius: 999px;
  padding: 0.25rem 0.75rem;
}

.card {
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: 10px;
  padding: 1.75rem;
}

h1 {
  margin: 0 0 0.35rem;
  font-size: 1.5rem;
}

.subtitle {
  margin: 0 0 1.5rem;
  color: var(--muted);
  line-height: 1.5;
}

.status-box {
  border-top: 1px solid var(--border);
  padding-top: 1.25rem;
}

.status-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 1rem;
  font-weight: 600;
}

.btn {
  padding: 0.4rem 0.85rem;
  border-radius: 6px;
  border: 1px solid var(--border);
  background: transparent;
  color: var(--text);
  cursor: pointer;
  font-size: 0.875rem;
}

.btn:hover:not(:disabled) {
  border-color: var(--accent);
  color: var(--accent);
}

.btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.details {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
  gap: 1rem;
  margin: 0;
}

.details dt {
  font-size: 0.75rem;
  text-transform: uppercase;
  letter-spacing: 0.04em;
  color: var(--muted);
  margin-bottom: 0.25rem;
}

.details dd {
  margin: 0;
  font-weight: 500;
}

.error {
  color: #dc2626;
  margin: 0;
}
</style>
