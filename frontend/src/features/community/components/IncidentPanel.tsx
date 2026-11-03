import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { useCommunity } from '../useCommunity';

export function IncidentPanel() {
  const { state, createIncident, loadMyIncidents } = useCommunity();

  const [subject, setSubject] = useState('');
  const [description, setDescription] = useState('');

  useEffect(() => {
    void loadMyIncidents();
  }, [loadMyIncidents]);

  const submit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const incident: Parameters<typeof createIncident>[0] = {
      bookingId: null,
      subject,
      description,
    };

    await createIncident(incident);
  };

  if (state.kind === 'error') {
    return <p role="alert">{state.message}</p>;
  }

  if (state.kind !== 'incidents') {
    return <p>Cargando incidencias…</p>;
  }

  return (
    <section className="worker-panel">
      <h2>Incidencias</h2>
      <form onSubmit={submit}>
        <label>
          Asunto
          <input
            value={subject}
            onChange={(event) => setSubject(event.target.value)}
          />
        </label>
        <label>
          Descripción
          <textarea
            value={description}
            onChange={(event) => setDescription(event.target.value)}
          />
        </label>
        <button type="submit">Crear incidencia</button>
      </form>
      {state.items.length === 0 ? (
        <p>Sin incidencias.</p>
      ) : (
        <ul className="worker-offerings">
          {state.items.map((incident) => (
            <li key={incident.id}>
              <span>
                {incident.subject} — {incident.status}
              </span>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
