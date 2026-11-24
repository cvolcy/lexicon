import { createRouter, createWebHistory } from 'vue-router';

declare module 'vue-router' {
  interface RouteMeta {
    /** Layout used to render the page. Defaults to `default`. */
    layout?: 'default';
    title?: string;
    icon?: string;
    description?: string;
  }
}

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'home',
      component: () => import('@/pages/HomePage.vue'),
      meta: {
        title: 'Overview',
        icon: 'i-lucide-layout-dashboard',
        description: 'Workspace summary and recent documents',
      },
    },
    {
      path: '/chat',
      name: 'chat',
      component: () => import('@/pages/ChatPage.vue'),
      meta: {
        title: 'Chat',
        icon: 'i-lucide-message-square-text',
        description: 'Ask questions grounded in your documents with inline citations',
      },
    },
    {
      path: '/documents',
      name: 'documents',
      component: () => import('@/pages/DocumentsPage.vue'),
      meta: {
        title: 'Documents',
        icon: 'i-lucide-files',
        description: 'Upload, index, and manage your local knowledge base',
      },
    },
    {
      path: '/search',
      name: 'search',
      component: () => import('@/pages/SearchPage.vue'),
      meta: {
        title: 'Search',
        icon: 'i-lucide-search',
        description: 'Hybrid semantic and keyword search across your indexed documents',
      },
    },
    {
      path: '/health',
      name: 'health',
      component: () => import('@/pages/HealthPage.vue'),
      meta: {
        title: 'System Health',
        icon: 'i-lucide-activity',
        description: 'Live diagnostics for database, storage, and AI providers',
      },
    },
    {
      path: '/:pathMatch(.*)*',
      name: 'not-found',
      component: () => import('@/pages/NotFoundPage.vue'),
      meta: {
        title: 'Not Found',
        icon: 'i-lucide-search-x',
      },
    },
  ],
});

router.afterEach((to) => {
  document.title = to.meta.title ? `${to.meta.title} · Lexicon` : 'Lexicon';
});

export default router;
