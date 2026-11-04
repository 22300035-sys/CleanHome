import { createHash } from 'node:crypto';
import { readdirSync } from 'node:fs';
import { join } from 'node:path';
import type { Plugin } from 'vite';

// Caché limitada a archivos estáticos. Nunca intercepta respuestas de cuentas o de la API.
export function cleanHomePwa(): Plugin {
  return {
    name: 'cleanhome-static-pwa',
    apply: 'build',
    generateBundle(_, bundle) {
      const walk = (directory: string, prefix = ''): string[] => readdirSync(directory, { withFileTypes: true }).flatMap(entry => entry.isDirectory() ? walk(join(directory, entry.name), prefix + entry.name + '/') : [prefix + entry.name]);
      const files = [...new Set(['index.html', 'manifest.webmanifest', ...Object.keys(bundle).filter(name => !name.endsWith('.map')), ...walk('public')])].sort();
      const version = createHash('sha256').update(files.join('|')).digest('hex').slice(0, 12);
      this.emitFile({ type: 'asset', fileName: 'manifest.webmanifest', source: JSON.stringify({ id: '/', name: 'CleanHome — Servicios de limpieza', short_name: 'CleanHome', description: 'Tu hogar, en buenas manos.', lang: 'es-MX', start_url: '/', scope: '/', display: 'standalone', theme_color: '#0d232e', background_color: '#f8f9ff', icons: [{ src: '/icons/icon.svg', sizes: 'any', type: 'image/svg+xml', purpose: 'any' }] }) });
      this.emitFile({ type: 'asset', fileName: 'sw.js', source: `
const CACHE = 'cleanhome-ui-${version}';
const ASSETS = ${JSON.stringify(files.map(f => '/' + f))};
const STATIC = new Set(ASSETS);
self.addEventListener('install', event => event.waitUntil(caches.open(CACHE).then(cache => cache.addAll(ASSETS))));
self.addEventListener('activate', event => event.waitUntil((async () => {
  const keys = await caches.keys();
  await Promise.all(keys.filter(key => key.startsWith('cleanhome-ui-') && key !== CACHE).map(key => caches.delete(key)));
  await self.clients.claim();
})()));
self.addEventListener('message', event => { if (event.data === 'SKIP_WAITING') self.skipWaiting(); });
self.addEventListener('fetch', event => {
  const request = event.request;
  const url = new URL(request.url);
  if (request.method !== 'GET' || url.origin !== self.location.origin || request.headers.has('Authorization') || url.pathname.startsWith('/api/') || url.pathname.startsWith('/hubs/')) return;
  if (request.mode === 'navigate') {
    event.respondWith(fetch(request).then(async response => {
      if (response.ok && response.headers.get('content-type')?.includes('text/html')) (await caches.open(CACHE)).put('/index.html', response.clone());
      return response;
    }).catch(async () => (await caches.match('/index.html')) || new Response('Sin conexión. Inténtalo de nuevo.', { status: 503 })));
  } else if (STATIC.has(url.pathname)) {
    event.respondWith(caches.open(CACHE).then(async cache => (await cache.match(url.pathname)) || fetch(request).then(response => { if (response.ok) cache.put(url.pathname, response.clone()); return response; })));
  }
});
` });
    },
  };
}
