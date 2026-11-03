const configuredBaseUrl =
  import.meta.env.VITE_API_BASE_URL;

if (!configuredBaseUrl) {
  throw new Error(
    'La variable VITE_API_BASE_URL es obligatoria.'
  );
}

export const API_BASE_URL =
  configuredBaseUrl.replace(/\/+$/, '');

export class ApiError extends Error {
  readonly status: number;
  readonly code?: string;

  constructor(
    message: string,
    status: number,
    code?: string
  ) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.code = code;
  }
}

export type ApiMethod =
  | 'GET'
  | 'POST'
  | 'PUT'
  | 'PATCH'
  | 'DELETE';

async function readErrorBody(
  response: Response
): Promise<{ code?: string; detail?: string } | null> {
  try {
    return (await response.json()) as {
      code?: string;
      detail?: string;
    };
  } catch {
    return null;
  }
}

export interface ApiRequestOptions {
  method?: ApiMethod;
  body?: BodyInit | null;
  signal?: AbortSignal;
  token?: string | null;
}

export async function apiRequest<T>(
  endpoint: string,
  options: ApiRequestOptions = {}
): Promise<T> {
  const path = endpoint.startsWith('/')
    ? endpoint
    : `/${endpoint}`;

  const headers: Record<string, string> = {
    Accept: 'application/json',
  };

  if (options.token) {
    headers.Authorization = `Bearer ${options.token}`;
  }

  const body: BodyInit | null = options.body ?? null;
  const isFormData = body instanceof FormData;

  if (!isFormData && body) {
    headers['Content-Type'] = 'application/json';
  }

  const response = await fetch(
    `${API_BASE_URL}${path}`,
    {
      method: options.method ?? 'GET',
      signal: options.signal,
      credentials: 'omit',
      headers,
      body,
    }
  );

  if (!response.ok) {
    const errorBody = await readErrorBody(response);

    throw new ApiError(
      errorBody?.detail ??
        `Error al consultar CleanHome: HTTP ${response.status}`,
      response.status,
      errorBody?.code
    );
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

export function apiGet<T>(
  endpoint: string,
  options: Omit<ApiRequestOptions, 'method' | 'body'> = {}
): Promise<T> {
  return apiRequest<T>(endpoint, { method: 'GET', ...options });
}

export function apiPost<T>(
  endpoint: string,
  body: unknown,
  options: Omit<ApiRequestOptions, 'method' | 'body'> = {}
): Promise<T> {
  return apiRequest<T>(endpoint, {
    method: 'POST',
    body: JSON.stringify(body),
    ...options,
  });
}

export function apiPostForm<T>(
  endpoint: string,
  formData: FormData,
  options: Omit<ApiRequestOptions, 'method' | 'body'> = {}
): Promise<T> {
  return apiRequest<T>(endpoint, {
    method: 'POST',
    body: formData,
    ...options,
  });
}

export function apiPut<T>(
  endpoint: string,
  body: unknown,
  options: Omit<ApiRequestOptions, 'method' | 'body'> = {}
): Promise<T> {
  return apiRequest<T>(endpoint, {
    method: 'PUT',
    body: JSON.stringify(body),
    ...options,
  });
}

export function apiPatch<T>(
  endpoint: string,
  body: unknown,
  options: Omit<ApiRequestOptions, 'method' | 'body'> = {}
): Promise<T> {
  return apiRequest<T>(endpoint, {
    method: 'PATCH',
    body: JSON.stringify(body),
    ...options,
  });
}
