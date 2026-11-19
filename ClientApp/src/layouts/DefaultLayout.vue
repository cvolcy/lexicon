<script setup lang="ts">
import { computed } from 'vue';
import { useRoute } from 'vue-router';
import type { NavigationMenuItem } from '@nuxt/ui';
import ColorModeSwitcher from '@/components/ColorModeSwitcher.vue';

const route = useRoute();

const pageTitle = computed(() => route.meta.title ?? 'Lexicon');
const pageIcon = computed(() => route.meta.icon);

const navItems: NavigationMenuItem[] = [
  { label: 'Overview', icon: 'i-lucide-layout-dashboard', to: '/' },
  { label: 'Documents', icon: 'i-lucide-files', to: '/documents' },
  { label: 'Search', icon: 'i-lucide-search', to: '/search' },
  { label: 'System Health', icon: 'i-lucide-activity', to: '/health' },
];
</script>

<template>
  <UDashboardGroup storage="local" storage-key="lexicon-dashboard">
    <UDashboardSidebar
      collapsible
      resizable
      :min-size="14"
      :default-size="17"
      :max-size="22"
      class="bg-elevated/35"
    >
      <template #header="{ collapsed }">
        <RouterLink
          to="/"
          class="flex items-center gap-2.5 overflow-hidden"
          :class="collapsed ? 'mx-auto justify-center' : ''"
        >
          <span
            class="flex size-8 shrink-0 items-center justify-center rounded-lg bg-primary/15 text-primary ring-1 ring-inset ring-primary/25"
          >
            <UIcon name="i-lucide-book-open-text" class="size-4" />
          </span>
          <div v-if="!collapsed" class="min-w-0">
            <div class="truncate text-sm font-semibold leading-none tracking-tight text-highlighted">
              Lexicon
            </div>
            <div class="mt-0.5 truncate text-[11px] text-muted">Local RAG</div>
          </div>
        </RouterLink>
      </template>

      <template #default="{ collapsed }">
        <UNavigationMenu
          :collapsed="collapsed"
          :items="navItems"
          orientation="vertical"
          tooltip
          popover
        />
      </template>

      <template #footer="{ collapsed }">
        <div
          class="flex items-center gap-2 rounded-lg border border-default bg-default/70 px-2.5 py-2 text-xs text-muted"
          :class="collapsed ? 'justify-center px-2' : ''"
        >
          <span class="size-2 shrink-0 rounded-full bg-emerald-500" />
          <span v-if="!collapsed" class="truncate font-medium">Local Workspace</span>
        </div>
      </template>
    </UDashboardSidebar>

    <UDashboardPanel id="main">
      <template #header>
        <UDashboardNavbar :title="pageTitle" :icon="pageIcon">
          <template #leading>
            <UDashboardSidebarCollapse />
          </template>
          <template #right>
            <ColorModeSwitcher />
          </template>
        </UDashboardNavbar>
      </template>

      <template #body>
        <div class="mx-auto w-full max-w-5xl">
          <slot />
        </div>
      </template>
    </UDashboardPanel>
  </UDashboardGroup>
</template>
