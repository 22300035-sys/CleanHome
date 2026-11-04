import { defineConfig, loadEnv } from 'vite';
import react from '@vitejs/plugin-react';
import { cleanHomePwa } from './build/pwa.ts';

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '');
  const target = env.CLEANHOME_DEV_API_TARGET || 'http://127.0.0.1:5186';
  return {
    plugins: [react(), cleanHomePwa()],
    server: { host: '127.0.0.1', port: 5173, strictPort: true, proxy: { '/api': { target, changeOrigin: true }, '/hubs': { target, changeOrigin: true, ws: true } } },
    preview: { host: '127.0.0.1', port: 5174, strictPort: true, proxy: { '/api': { target, changeOrigin: true }, '/hubs': { target, changeOrigin: true, ws: true } } },
    build: { outDir: 'dist' },
  };
});

