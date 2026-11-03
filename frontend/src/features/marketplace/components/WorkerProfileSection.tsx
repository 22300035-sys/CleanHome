import { useCallback, useEffect, useState } from 'react';
import type { ChangeEvent, FormEvent } from 'react';
import { useMarketplace } from '../useMarketplace';
import type { UpsertWorkerProfileRequest } from '../../../types/marketplace';

interface WorkerProfileDraft {
  description: string;
  serviceCity: string;
  serviceArea: string;
}

const EMPTY_DRAFT: WorkerProfileDraft = {
  description: '',
  serviceCity: '',
  serviceArea: '',
};

export function WorkerProfileSection() {
  const { state, getWorkerDashboard, updateWorkerProfile, setPublication } =
    useMarketplace();

  const [draft, setDraft] = useState<WorkerProfileDraft | null>(null);
  const [saved, setSaved] = useState(false);

  const load = useCallback(
    () => getWorkerDashboard(),
    [getWorkerDashboard]
  );

  useEffect(() => {
    void load();
  }, [load]);

  const values: WorkerProfileDraft =
    draft ??
    (state.kind === 'workerProfile'
      ? {
          description: state.profile.profile.description,
          serviceCity: state.profile.profile.serviceCity,
          serviceArea: state.profile.profile.serviceArea,
        }
      : EMPTY_DRAFT);

  const profile = state.kind === 'workerProfile' ? state.profile.profile : null;

  const save = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const input: UpsertWorkerProfileRequest = {
      description: values.description,
      serviceCity: values.serviceCity,
      serviceArea: values.serviceArea,
    };

    await updateWorkerProfile(input);

    setDraft(input);
    setSaved(true);
  };

  const togglePublication = async () => {
    if (state.kind !== 'workerProfile') {
      return;
    }

    const isPublished = state.profile.profile.isPublished;

    await setPublication(!isPublished);
  };

  if (state.kind !== 'workerProfile' || profile === null) {
    return (
      <section className="worker-panel">
        <h2>Mi perfil de trabajador</h2>
        <p>Cargando perfil…</p>
      </section>
    );
  }

  const onDescriptionChange = (
    event: ChangeEvent<HTMLTextAreaElement>
  ) => {
    setDraft({
      description: event.target.value,
      serviceCity: draft?.serviceCity ?? values.serviceCity,
      serviceArea: draft?.serviceArea ?? values.serviceArea,
    });
  };

  const onCityChange = (event: ChangeEvent<HTMLInputElement>) => {
    setDraft({
      description: draft?.description ?? values.description,
      serviceCity: event.target.value,
      serviceArea: draft?.serviceArea ?? values.serviceArea,
    });
  };

  const onAreaChange = (event: ChangeEvent<HTMLInputElement>) => {
    setDraft({
      description: draft?.description ?? values.description,
      serviceCity: draft?.serviceCity ?? values.serviceCity,
      serviceArea: event.target.value,
    });
  };

  return (
    <section className="worker-panel">
      <h2>Mi perfil de trabajador</h2>
      <p>
        Estado de verificación: {profile.verificationStatus} · Publicado:{' '}
        {profile.isPublished ? 'sí' : 'no'}
      </p>
      <form onSubmit={save}>
        <label>
          Descripción
          <textarea
            value={values.description}
            onChange={onDescriptionChange}
          />
        </label>
        <label>
          Ciudad de servicio
          <input
            value={values.serviceCity}
            onChange={onCityChange}
          />
        </label>
        <label>
          Área de servicio
          <input
            value={values.serviceArea}
            onChange={onAreaChange}
          />
        </label>
        <button type="submit">Guardar perfil</button>
      </form>
      {saved ? <p>Perfil guardado.</p> : null}
      <button type="button" onClick={togglePublication}>
        {profile.isPublished ? 'Despublicar' : 'Publicar'}
      </button>
    </section>
  );
}
