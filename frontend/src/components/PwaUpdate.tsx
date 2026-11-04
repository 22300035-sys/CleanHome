import { useEffect, useState } from 'react';
import { Button } from './ui';

export function PwaUpdate() {
  const [registration, setRegistration] = useState<ServiceWorkerRegistration | null>(null);
  useEffect(() => {
    if (!import.meta.env.PROD || !('serviceWorker' in navigator)) return;
    let current = true;
    const check = (reg: ServiceWorkerRegistration) => { if (current && reg.waiting && navigator.serviceWorker.controller) setRegistration(reg); };
    void navigator.serviceWorker.register('/sw.js').then(reg => {
      check(reg);
      reg.addEventListener('updatefound', () => { reg.installing?.addEventListener('statechange', () => check(reg)); });
    }).catch(() => { /* La aplicación sigue funcionando si el navegador no permite instalar la PWA. */ });
    return () => { current = false; };
  }, []);
  if (!registration) return null;
  return <aside className="pwa-update" role="status"><div><strong>Nueva versión disponible</strong><p>Guarda cualquier formulario antes de actualizar.</p></div><Button variant="secondary" onClick={() => { navigator.serviceWorker.addEventListener('controllerchange', () => window.location.reload(), { once: true }); registration.waiting?.postMessage('SKIP_WAITING'); }}>Actualizar</Button><button aria-label="Recordar más tarde" onClick={() => setRegistration(null)}>×</button></aside>;
}
