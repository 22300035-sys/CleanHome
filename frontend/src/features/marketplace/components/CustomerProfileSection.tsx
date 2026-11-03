import { useCallback, useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { useMarketplace } from '../useMarketplace';
import type { UpdateUserProfileRequest, UserProfileResponse } from '../../../types/marketplace';

export function CustomerProfileSection() {
  const { state, getUserProfile, updateUserProfile } = useMarketplace();
  const [city, setCity] = useState('');
  const [phone, setPhone] = useState('');

  const load = useCallback(() => getUserProfile(), [getUserProfile]);

  useEffect(() => {
    void load();
  }, [load]);

  const submit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const input: UpdateUserProfileRequest = {
      city,
      phone: phone || null,
    };

    await updateUserProfile(input);
  };

  if (state.kind === 'error') {
    return <p role="alert">{state.message}</p>;
  }

  if (state.kind !== 'userProfile' || state.profile === null) {
    return <p>Cargando perfil…</p>;
  }

  const profile: UserProfileResponse = state.profile;

  return (
    <section className="worker-panel">
      <h2>Mi perfil</h2>
      <p>{profile.city}</p>
      <form onSubmit={submit}>
        <label>
          Ciudad
          <input value={city} onChange={(event) => setCity(event.target.value)} />
        </label>
        <label>
          Teléfono
          <input value={phone} onChange={(event) => setPhone(event.target.value)} />
        </label>
        <button type="submit">Guardar perfil</button>
      </form>
    </section>
  );
}
