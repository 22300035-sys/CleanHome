import { useCallback, useEffect, useMemo, useState } from 'react';
import type { ReactNode } from 'react';
import { AppContext } from './state';
import { ApiError, jsonBody, liveApi } from '../services/api';
import { DemoApi, demoAccount, makeDemoStore } from '../services/demo';
import type { Account, Profile, Role, Tokens, Transport } from '../services/types';

const initialDemo = new URLSearchParams(window.location.search).get('demo') === '1';
const initialRole = new URLSearchParams(window.location.search).get('role');
const demoRole = initialRole === 'Worker' || initialRole === 'Admin' ? initialRole : 'Customer';

export function AppProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<Account | null>(() => initialDemo ? demoAccount(demoRole) : null);
  const [demo, setDemo] = useState(initialDemo);
  const [store] = useState(makeDemoStore);
  const [notice, setNotice] = useState('');
  const api = useMemo<Transport>(() => demo && user ? new DemoApi(store, user) : liveApi, [demo, store, user]);
  const toast = useCallback((message: string) => setNotice(message), []);
  useEffect(() => { if (!notice) return; const timer = window.setTimeout(() => setNotice(''), 5000); return () => window.clearTimeout(timer); }, [notice]);
  useEffect(() => {
    const expire = () => { liveApi.setTokens(null); setUser(null); setDemo(false); setNotice('Tu sesión terminó. Inicia sesión de nuevo.'); };
    window.addEventListener('cleanhome:session-expired', expire);
    return () => window.removeEventListener('cleanhome:session-expired', expire);
  }, []);
  const login = useCallback(async (email: string, password: string, role: Role, name?: string, factor?: string) => {
    liveApi.setTokens(null);
    const tokens = await liveApi.request<Tokens>('/auth/login', jsonBody({ email, password, ...(factor ? { twoFactorCode: factor } : {}) }));
    liveApi.setTokens(tokens);
    try {
      let isAdmin = false;
      try { await liveApi.request('/admin/profiles?page=1'); isAdmin = true; } catch (e) { if (!(e instanceof ApiError) || e.status !== 403) throw e; }
      let profile: Profile;
      if (isAdmin) profile = { id: 'admin', userId: 'admin', displayName: 'Admin Operaciones', role: 'Customer', isVerified: true, isSuspended: false, createdAt: '' };
      else {
        try { profile = await liveApi.request<Profile>('/profiles/me'); }
        catch (e) { if (!(e instanceof ApiError) || e.status !== 409) throw e; profile = await liveApi.request<Profile>('/profiles/me', jsonBody({ displayName: name || email.split('@')[0], role })); }
      }
      setDemo(false); setUser({ ...profile, email, isAdmin });
    } catch (error) { liveApi.setTokens(null); throw error; }
  }, []);
  const register = useCallback(async (email: string, password: string, role: Role, name: string) => {
    await liveApi.request('/auth/register', jsonBody({ email, password }));
    try { await login(email, password, role, name); return true; }
    catch (error) { if (error instanceof ApiError && [401, 403].includes(error.status)) return false; throw error; }
  }, [login]);
  const startDemo = useCallback((role: Role | 'Admin' = 'Customer') => { liveApi.setTokens(null); setDemo(true); const account = demoAccount(role); const profile = store.profiles.find(p => p.id === account.id); setUser(profile ? { ...account, ...profile } : account); }, [store]);
  const logout = useCallback(async () => {
    if (!demo) { try { await liveApi.request('/auth/logout', { method: 'POST' }); } catch { setNotice('Sesión cerrada en este dispositivo. No se pudo confirmar el cierre remoto.'); } }
    liveApi.setTokens(null); setUser(null); setDemo(false);
  }, [demo]);
  const reloadProfile = useCallback(async () => {
    if (!user || user.isAdmin) return;
    const profile = await api.request<Profile>('/profiles/me');
    setUser({ ...profile, email: user.email, isAdmin: false });
  }, [api, user]);
  return <AppContext.Provider value={{ user, api, demo, login, register, startDemo, logout, reloadProfile, toast }}>
    {children}
    {notice && <div className="toast" role="status"><span>{notice}</span><button type="button" onClick={() => setNotice('')} aria-label="Cerrar aviso">×</button></div>}
  </AppContext.Provider>;
}


