import { useCallback, useState } from 'react';
import {
  requestPasswordReset,
  resendVerification,
  verifyEmail,
} from '../../services/auth';

export type AccountRecoveryState =
  | { kind: 'idle' }
  | { kind: 'message'; message: string }
  | { kind: 'error'; status: number; message: string };

export function useAccountRecovery() {
  const [state, setState] = useState<AccountRecoveryState>({
    kind: 'idle',
  });

  const resend = useCallback(async (email: string) => {
    const response = await resendVerification({ email });

    setState({ kind: 'message', message: response.message });

    return response;
  }, []);

  const verify = useCallback(async (token: string) => {
    const response = await verifyEmail({ token });

    setState({ kind: 'message', message: response.message });

    return response;
  }, []);

  const requestReset = useCallback(async (email: string) => {
    const response = await requestPasswordReset({ email });

    setState({ kind: 'message', message: response.message });

    return response;
  }, []);

  return { state, resend, verify, requestReset };
}
