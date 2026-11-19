<script setup lang="ts">
import { computed } from 'vue';
import { useRoute } from 'vue-router';
import DefaultLayout from '@/layouts/DefaultLayout.vue';
import { useColorModeStore } from '@/stores/colorMode';

useColorModeStore();

const layouts = {
  default: DefaultLayout,
};

const route = useRoute();
const layout = computed(() => layouts[route.meta.layout ?? 'default']);
</script>

<template>
  <UApp>
    <component :is="layout">
      <RouterView />
    </component>
  </UApp>
</template>
