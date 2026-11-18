<template>
  <UCard>
    <template #header>
      <h1 class="text-lg font-semibold">Welcome to Lexicon</h1>
    </template>

    <p class="text-muted">
      Upload your documents and ask questions about them. Start by managing your documents, or check that the
      backend is healthy.
    </p>

    <template #footer>
      <div class="flex gap-2">
        <UButton to="/documents" icon="i-lucide-files">Documents</UButton>
        <UButton to="/health" icon="i-lucide-activity" color="neutral" variant="outline">Health</UButton>
      </div>
    </template>
  </UCard>
</template>
