import './assets/main.css';

import { createApp } from 'vue';
import ui from '@nuxt/ui/vue-plugin';
import App from './App.vue';
import router from './router';
import { pinia } from './stores';

createApp(App)
  .use(pinia)
  .use(router)
  .use(ui)
  .mount('#app');
