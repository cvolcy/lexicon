<script setup lang="ts">
import { computed, ref } from 'vue';
import { useHealth } from '@/composables/useHealth';

const { health } = useHealth();
const dismissed = ref(false);

const aiCheck = computed(() =>
  health.value?.checks.find((c) => c.name.toLowerCase() === 'ai') ?? null,
);

const isVisible = computed(
  () =>
    !dismissed.value &&
    aiCheck.value !== null &&
    (aiCheck.value.status === 'Degraded' || aiCheck.value.status === 'Unhealthy'),
);
</script>

<template>
  <UAlert
    v-if="isVisible && aiCheck"
    :color="aiCheck.status === 'Unhealthy' ? 'error' : 'warning'"
    variant="subtle"
    icon="i-lucide-triangle-alert"
    :title="`AI Provider ${aiCheck.status}`"
    :description="aiCheck.description ?? 'Check your local Ollama configuration.'"
    :actions="[{ label: 'System Health', to: '/health', color: 'neutral', variant: 'outline', size: 'xs' }]"
    close
    @update:open="dismissed = true"
  />
</template>
