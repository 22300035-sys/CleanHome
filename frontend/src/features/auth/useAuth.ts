import { useCallback, useEffect, useState } from 'react';
import {
  clearTokens,
  getAccessToken,
  getCurrentUser,
  login as loginRequest,
  logout as logoutRequest,
  refreshSession,
  saveTokens,
} from '../../services/auth';
import { ApiError } from '../../services/api';
import type {
  AuthenticationResponse,
  CurrentUserResponse,
  LoginRequest,
} from '../../types/auth';

export type AuthState =
  | { kind: 'loading' }
  | { kind: 'authenticated'; user: CurrentUserResponse }
  | { kind: 'unauthenticated' };

export function useAuth() {
  const [state, setState] = useState<AuthState>({
    kind: 'loading',
  });

  const authenticate = useCallback(
    async (response: AuthenticationResponse) => {
      saveTokens(
        response.accessToken,
        response.refreshToken
      );

      const user = await getCurrentUser();

      setState({
        kind: 'authenticated',
        user,
      });
    },
    []
  );

  const signIn = useCallback(
    async (request: LoginRequest) => {
      const response = await loginRequest(request);

      await authenticate(response);

      return response;
    },
    [authenticate]
  );

  const signOut = useCallback(
    async () => {
      await logoutRequest();

      clearTokens();

      setState({ kind: 'unauthenticated' });
    },
    []
  );

  const refresh = useCallback(async () => {
    const response = await refreshSession();

    await authenticate(response);

    return response;
  }, [authenticate]);

  useEffect(() => {
    let cancelled = false;

    async function restoreSession() {
      const token = getAccessToken();

      if (!token) {
        if (!cancelled) {
          setState({ kind: 'unauthenticated' });
        }

        return;
      }

      try {
        const user = await getCurrentUser();

        if (!cancelled) {
          setState({
            kind: 'authenticated',
            user,
          });
        }
      } catch (error) {
        const status =
          error instanceof ApiError
            ? error.status
            : 0;

        if (!cancelled && status === 401) {
          try {
            const response = await refreshSession();

            if (!cancelled) {
              await authenticate(response);
            }
          } catch {
            if (!cancelled) {
              setState({ kind: 'unauthenticated' });
            }
          }
        }
      }
    }

    void restoreSession();

    return () => {
      cancelled = true;
    };
  }, [authenticate]);

  return {
    state,
    signIn,
    signOut,
    refresh,
  };
}
