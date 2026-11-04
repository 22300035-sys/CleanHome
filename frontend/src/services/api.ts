import type { Tokens, Transport } from './types';

export const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL || '/api/v1').replace(/\/+$/, '');
export const HUB_URL = API_BASE_URL.replace(/\/api\/v1$/, '') + '/hubs/chat';
export class ApiError extends Error { status: number; constructor(status: number, message: string) { super(message); this.status = status; this.name = 'ApiError'; } }

export class HttpApi implements Transport {
  private tokens: Tokens | null = null;
  private refreshing: Promise<void> | null = null;
  setTokens(tokens: Tokens | null) { this.tokens = tokens; }
  getAccessToken = () => this.tokens?.accessToken || '';
  private async send(path: string, init: RequestInit = {}, retry = true): Promise<Response> {
    const headers = new Headers(init.headers);
    headers.set('Accept', 'application/json');
    if (init.body && !(init.body instanceof FormData)) headers.set('Content-Type', 'application/json');
    if (this.tokens) headers.set('Authorization', `Bearer ${this.tokens.accessToken}`);
    let response: Response;
    try { response = await fetch(API_BASE_URL + path, { ...init, headers, credentials: 'omit', cache: 'no-store' }); }
    catch (error) { if (error instanceof DOMException && error.name === 'AbortError') throw error; throw new ApiError(0, 'No pudimos conectar. Revisa tu conexión e inténtalo de nuevo.'); }
    if (response.status === 401 && this.tokens && retry && !path.startsWith('/auth/')) {
      this.refreshing ??= this.refresh().finally(() => { this.refreshing = null; });
      await this.refreshing;
      return this.send(path, init, false);
    }
    if (!response.ok) {
      let message = response.status === 403 ? 'Tu cuenta no tiene permiso para esta acción.' : `No se pudo completar la acción (${response.status}).`;
      try {
        const data = await response.json();
        const errors = data.errors ? Object.values(data.errors).flat().join(' ') : '';
        message = errors || data.detail || data.title || message;
      } catch { /* No todos los errores contienen JSON. */ }
      if (response.status === 429) message = 'Has realizado varias acciones seguidas. Espera un minuto y vuelve a intentarlo.';
      if (response.status === 401 && !path.startsWith('/auth/')) window.dispatchEvent(new Event('cleanhome:session-expired'));
      throw new ApiError(response.status, message);
    }
    return response;
  }
  private async refresh() {
    const refreshToken = this.tokens?.refreshToken;
    if (!refreshToken) throw new ApiError(401, 'Inicia sesión de nuevo.');
    try { this.tokens = await this.request<Tokens>('/auth/refresh', { method: 'POST', body: JSON.stringify({ refreshToken }) }); }
    catch (error) { this.tokens = null; window.dispatchEvent(new Event('cleanhome:session-expired')); throw error; }
  }
  async request<T>(path: string, init: RequestInit = {}): Promise<T> {
    const response = await this.send(path, init);
    if (response.status === 204 || response.headers.get('content-length') === '0') return undefined as T;
    const text = await response.text();
    return text ? JSON.parse(text) as T : undefined as T;
  }
  async getBlob(path: string) { return (await this.send(path)).blob(); }
}
export const liveApi = new HttpApi();
export const jsonBody = (data: unknown, method = 'POST'): RequestInit => ({ method, body: JSON.stringify(data) });

