<script setup lang="ts">
import type { NavigationMenuItem } from '@nuxt/ui';

const items: NavigationMenuItem[] = [
  { label: 'Home', icon: 'i-lucide-house', to: '/' },
  { label: 'Documents', icon: 'i-lucide-files', to: '/documents' },
  { label: 'Health', icon: 'i-lucide-activity', to: '/health' },
];
</script>

<template>
  <UContainer class="max-w-3xl py-6">
    <header class="flex items-center justify-between gap-4 border-b border-default pb-4">
      <RouterLink to="/" class="text-xl font-bold tracking-tight">Lexicon</RouterLink>
      <UNavigationMenu :items="items" />
    </header>

    <main class="py-8">
      <slot />
    </main>
  </UContainer>
</template>
