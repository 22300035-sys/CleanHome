const configuredBaseUrl =
  import.meta.env.VITE_API_BASE_URL;

if (!configuredBaseUrl) {
  throw new Error(
    'La variable VITE_API_BASE_URL es obligatoria.'
  );
}

export const API_BASE_URL =
  configuredBaseUrl.replace(/\/+$/, '');

export async function apiGet<T>(
  endpoint: string,
  signal?: AbortSignal
): Promise<T> {
  const path = endpoint.startsWith('/')
    ? endpoint
    : `/${endpoint}`;

  const response = await fetch(
    `${API_BASE_URL}${path}`,
    {
      method: 'GET',
      signal,
      credentials: 'omit',
      headers: {
        Accept: 'application/json',
      },
      cache: 'no-store',
    }
  );

  if (!response.ok) {
    throw new Error(
      `Error al consultar CleanHome: HTTP ${response.status}`
    );
  }

  return (await response.json()) as T;
}
