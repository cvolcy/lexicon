<script setup lang="ts">
import { computed } from 'vue';
import type { DropdownMenuItem } from '@nuxt/ui';
import { useColorModeStore, type ColorModePreference } from '@/stores/colorMode';

const colorMode = useColorModeStore();

const options: { value: ColorModePreference; label: string; icon: string }[] = [
  { value: 'system', label: 'System', icon: 'i-lucide-monitor' },
  { value: 'light', label: 'Light', icon: 'i-lucide-sun' },
  { value: 'dark', label: 'Dark', icon: 'i-lucide-moon' },
];

const currentIcon = computed(
  () => options.find((o) => o.value === colorMode.preference)?.icon ?? 'i-lucide-monitor',
);

const items = computed<DropdownMenuItem[]>(() =>
  options.map((option) => ({
    label: option.label,
    icon: option.icon,
    type: 'checkbox',
    checked: colorMode.preference === option.value,
    onSelect: () => colorMode.setPreference(option.value),
  })),
);
</script>

<template>
  <UDropdownMenu :items="items" :content="{ align: 'end' }">
    <UButton
      :icon="currentIcon"
      color="neutral"
      variant="ghost"
      size="sm"
      aria-label="Switch color mode"
    />
  </UDropdownMenu>
</template>
