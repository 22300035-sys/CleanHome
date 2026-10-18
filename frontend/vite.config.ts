import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { VitePWA } from 'vite-plugin-pwa';

export default defineConfig({
  plugins: [
    react(),

    VitePWA({
      registerType: 'prompt',

      includeAssets: [
        'icons/icon.svg',
      ],

      manifest: {
        id: '/',
        name: 'CleanHome - Servicios de Limpieza',
        short_name: 'CleanHome',
        description:
          'Plataforma de intermediación de servicios de limpieza.',

        start_url: '/',
        scope: '/',
        display: 'standalone',

        theme_color: '#0D232E',
        background_color: '#F8F9FF',

        icons: [
          {
            src: '/icons/icon.svg',
            sizes: 'any',
            type: 'image/svg+xml',
            purpose: 'any',
          },
        ],
      },

      workbox: {
        navigateFallback: '/index.html',

        // Nunca almacenar solicitudes HTTP de la API
        // en el caché de navegación de la PWA.
        navigateFallbackDenylist: [/^\/api\//],
      },
    }),
  ],

  build: {
    outDir: 'dist',
  },
});
