import { fileURLToPath, URL } from 'node:url';
import { env } from 'node:process';
import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';
import ui from '@nuxt/ui/vite';

export default defineConfig({
  plugins: [
    vue(),
    ui({
      colorMode: false, // Prevent from conflicting with store color mode management
      ui: {
        colors: {
          primary: 'emerald',
          neutral: 'slate',
        },
      },
      components: { dts: 'src/components.d.ts' },
      autoImport: { dts: 'src/auto-imports.d.ts' },
    }),
  ],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  server: {
    port: 5173,
    strictPort: true,
    proxy: {
      '/api': {
        target: env.ASPNETCORE_URLS?.split(';')[0] ?? 'http://localhost:5094',
        changeOrigin: true,
        secure: false,
      },
    },
  },
  build: {
    outDir: 'dist',
    emptyOutDir: true,
  },
});
