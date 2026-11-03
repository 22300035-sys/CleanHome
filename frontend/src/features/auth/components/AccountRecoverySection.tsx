import { useState } from 'react';
import type { FormEvent } from 'react';
import { useAccountRecovery } from '../useAccountRecovery';
import type { EmailAddressRequest, VerifyEmailRequest } from '../../../types/auth';

export function AccountRecoverySection() {
  const { state, resend, verify, requestReset } = useAccountRecovery();

  const [email, setEmail] = useState('');
  const [token, setToken] = useState('');
  const [newPassword, setNewPassword] = useState('');

  const submitResend = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const request: EmailAddressRequest = { email };

    await resend(request.email);
  };

  const submitVerify = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const request: VerifyEmailRequest = { token };

    await verify(request.token);
  };

  const submitReset = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    await requestReset(email);
  };

  return (
    <section className="worker-panel">
      <h2>Recuperación de cuenta</h2>
      <form onSubmit={submitResend}>
        <label>
          Correo
          <input
            type="email"
            value={email}
            onChange={(event) => setEmail(event.target.value)}
          />
        </label>
        <button type="submit">Enviar instrucciones</button>
      </form>
      <form onSubmit={submitVerify}>
        <label>
          Token de verificación
          <input
            value={token}
            onChange={(event) => setToken(event.target.value)}
          />
        </label>
        <button type="submit">Verificar correo</button>
      </form>
      <form onSubmit={submitReset}>
        <label>
          Token de restablecimiento
          <input
            value={token}
            onChange={(event) => setToken(event.target.value)}
          />
        </label>
        <label>
          Nueva contraseña
          <input
            type="password"
            value={newPassword}
            onChange={(event) => setNewPassword(event.target.value)}
          />
        </label>
        <button type="submit">Restablecer contraseña</button>
      </form>
      {state.kind === 'message' ? <p>{state.message}</p> : null}
      {state.kind === 'error' ? <p role="alert">{state.message}</p> : null}
    </section>
  );
}
