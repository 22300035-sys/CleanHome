import { apiGet, apiPost } from './api';
import type {
  AccountRecoveryMessage,
  AuthenticationResponse,
  CurrentUserResponse,
  EmailAddressRequest,
  LoginRequest,
  RegisterUserRequest,
  RegisterUserResponse,
  ResetPasswordRequest,
  SessionResponse,
  VerifyEmailRequest,
} from '../types/auth';

const ACCESS_TOKEN_KEY = 'cleanhome.accessToken';
const REFRESH_TOKEN_KEY = 'cleanhome.refreshToken';

export function saveTokens(
  accessToken: string,
  refreshToken: string
): void {
  window.localStorage.setItem(
    ACCESS_TOKEN_KEY,
    accessToken
  );

  window.localStorage.setItem(
    REFRESH_TOKEN_KEY,
    refreshToken
  );
}

export function getAccessToken(): string | null {
  return window.localStorage.getItem(ACCESS_TOKEN_KEY);
}

export function getRefreshToken(): string | null {
  return window.localStorage.getItem(REFRESH_TOKEN_KEY);
}

export function clearTokens(): void {
  window.localStorage.removeItem(ACCESS_TOKEN_KEY);
  window.localStorage.removeItem(REFRESH_TOKEN_KEY);
}

export async function register(
  request: RegisterUserRequest
): Promise<RegisterUserResponse> {
  return apiPost<RegisterUserResponse>(
    '/api/v1/auth/register',
    request
  );
}

export async function login(
  request: LoginRequest
): Promise<AuthenticationResponse> {
  const response = await apiPost<AuthenticationResponse>(
    '/api/v1/auth/login',
    request
  );

  saveTokens(
    response.accessToken,
    response.refreshToken
  );

  return response;
}

export async function refreshSession(): Promise<AuthenticationResponse> {
  const refreshToken = getRefreshToken();

  if (!refreshToken) {
    throw new Error('No hay sesión activa.');
  }

  const response = await apiPost<AuthenticationResponse>(
    '/api/v1/auth/refresh',
    { refreshToken }
  );

  saveTokens(
    response.accessToken,
    response.refreshToken
  );

  return response;
}

export async function logout(): Promise<void> {
  const token = getAccessToken();

  if (token) {
    try {
      await apiPost('/api/v1/auth/logout', {});
    } catch {
      // El cierre de sesión local debe continuar aunque el servidor
      // no esté disponible.
    }
  }

  clearTokens();
}

export async function logoutAll(): Promise<void> {
  const token = getAccessToken();

  if (token) {
    try {
      await apiPost('/api/v1/auth/logout-all', {});
    } catch {
      // El cierre de sesión local debe continuar aunque el servidor
      // no esté disponible.
    }
  }

  clearTokens();
}

export async function getSessions(): Promise<SessionResponse[]> {
  return apiGet<SessionResponse[]>('/api/v1/auth/sessions');
}

export async function getCurrentUser(): Promise<CurrentUserResponse> {
  return apiGet<CurrentUserResponse>('/api/v1/auth/me');
}

export async function resendVerification(
  request: EmailAddressRequest
): Promise<AccountRecoveryMessage> {
  return apiPost<AccountRecoveryMessage>(
    '/api/v1/auth/resend-verification',
    request
  );
}

export async function verifyEmail(
  request: VerifyEmailRequest
): Promise<AccountRecoveryMessage> {
  return apiPost<AccountRecoveryMessage>(
    '/api/v1/auth/verify-email',
    request
  );
}

export async function requestPasswordReset(
  request: EmailAddressRequest
): Promise<AccountRecoveryMessage> {
  return apiPost<AccountRecoveryMessage>(
    '/api/v1/auth/forgot-password',
    request
  );
}

export async function resetPassword(
  request: ResetPasswordRequest
): Promise<AccountRecoveryMessage> {
  return apiPost<AccountRecoveryMessage>(
    '/api/v1/auth/reset-password',
    request
  );
}
