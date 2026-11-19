import { computed, ref, watch } from 'vue';
import { defineStore } from 'pinia';
import { usePreferredDark } from '@vueuse/core';

export type ColorModePreference = 'system' | 'light' | 'dark';
export type ResolvedColorMode = 'light' | 'dark';

export const useColorModeStore = defineStore(
  'color-mode',
  () => {
    const preference = ref<ColorModePreference>('system');
    const prefersDark = usePreferredDark();

    const resolved = computed<ResolvedColorMode>(() => {
      if (preference.value === 'system') {
        return prefersDark.value ? 'dark' : 'light';
      }
      return preference.value;
    });

    const isDark = computed(() => resolved.value === 'dark');

    function setPreference(mode: ColorModePreference): void {
      preference.value = mode;
    }

    function applyToDocument(mode: ResolvedColorMode): void {
      const root = document.documentElement;
      root.classList.toggle('dark', mode === 'dark');
      root.classList.toggle('light', mode === 'light');
      root.style.colorScheme = mode;
    }

    watch(resolved, applyToDocument, { immediate: true });

    return {
      preference,
      resolved,
      isDark,
      setPreference,
    };
  },
  {
    persist: {
      pick: ['preference'],
    },
  },
);
