import { useCallback, useContext, useEffect, useState } from 'react';
import { AppContext } from './state';

export function useApp() { const value = useContext(AppContext); if (!value) throw new Error('Falta AppProvider'); return value; }

export function useRemote<T>(path: string | null) {
  const { api } = useApp();
  const [revision, setRevision] = useState(0);
  const key = `${path}:${revision}`;
  const [result, setResult] = useState<{ key: string; data?: T; error?: string }>({ key: '' });
  useEffect(() => {
    if (!path) return;
    let current = true;
    const controller = new AbortController();
    api.request<T>(path, { signal: controller.signal }).then(data => { if (current) setResult({ key, data }); }).catch(error => { if (current && error.name !== 'AbortError') setResult({ key, error: error.message }); });
    return () => { current = false; controller.abort(); };
  }, [api, key, path]);
  const reload = useCallback(() => setRevision(x => x + 1), []);
  return { data: result.key === key ? result.data : undefined, loading: Boolean(path) && result.key !== key, error: result.key === key ? result.error : undefined, reload };
}

export function useAction() {
  const [pending, setPending] = useState(false);
  const [error, setError] = useState('');
  const run = async (action: () => Promise<void>) => {
    if (pending) return;
    setError(''); setPending(true);
    try { await action(); } catch (e) { setError(e instanceof Error ? e.message : 'No se pudo completar la acción.'); } finally { setPending(false); }
  };
  return { pending, error, run, clear: () => setError('') };
}

export function useClock(interval = 60000) {
  const [now, setNow] = useState(Date.now);
  useEffect(() => { const timer = window.setInterval(() => setNow(Date.now()), interval); return () => window.clearInterval(timer); }, [interval]);
  return now;
}
