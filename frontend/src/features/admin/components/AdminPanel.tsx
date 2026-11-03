import { useCallback, useEffect, useState } from 'react';
import { useAdministration } from '../useAdministration';

export function AdminPanel() {
  const {
    state,
    loadPermissions,
    loadWorkers,
    loadWorkerDocuments,
    reviewWorker,
    loadIncidents,
    changeIncidentStatus,
  } = useAdministration();

  const [selectedWorkerId, setSelectedWorkerId] = useState('');

  useEffect(() => {
    void loadPermissions();
  }, [loadPermissions]);

  useEffect(() => {
    void loadWorkers();
  }, [loadWorkers]);

  useEffect(() => {
    void loadIncidents();
  }, [loadIncidents]);

  useEffect(() => {
    if (!selectedWorkerId) {
      return;
    }

    void loadWorkerDocuments(selectedWorkerId);
  }, [loadWorkerDocuments, selectedWorkerId]);

  const review = useCallback(
    async (workerId: string, status: string) => {
      await reviewWorker(workerId, { status, reason: null });
    },
    [reviewWorker]
  );

  const changeStatus = useCallback(
    async (incidentId: string, status: string) => {
      await changeIncidentStatus(incidentId, { status, reason: null });
    },
    [changeIncidentStatus]
  );

  if (state.kind === 'error') {
    return <p role="alert">{state.message}</p>;
  }

  return (
    <section className="worker-panel">
      <h2>Administración</h2>

      {state.kind === 'permissions' ? (
        <p>
          Rol: {state.permissions.role} · Revisión de trabajadores:{' '}
          {state.permissions.canReviewWorkers ? 'permitida' : 'denegada'} ·
          Incidencias:{' '}
          {state.permissions.canManageIncidents ? 'permitida' : 'denegada'}
        </p>
      ) : null}

      {state.kind === 'workers' ? (
        <div>
          <h3>Trabajadores</h3>
          <ul className="worker-offerings">
            {state.items.map((worker) => (
              <li key={worker.id}>
                <span>
                  {worker.displayName} — {worker.serviceCity} —{' '}
                  {worker.verificationStatus} — Publicado:{' '}
                  {worker.isPublished ? 'sí' : 'no'}
                </span>
                <span>
                  <button
                    type="button"
                    onClick={() => void review(worker.id, 'approved')}
                  >
                    Aprobar
                  </button>
                  <button
                    type="button"
                    onClick={() => void review(worker.id, 'rejected')}
                  >
                    Rechazar
                  </button>
                  <button
                    type="button"
                    onClick={() => void review(worker.id, 'suspended')}
                  >
                    Suspender
                  </button>
                </span>
              </li>
            ))}
          </ul>
        </div>
      ) : null}

      {state.kind === 'workerDocuments' ? (
        <div>
          <h3>Documentos del trabajador</h3>
          <label>
            Trabajador
            <select
              value={selectedWorkerId}
              onChange={(event) =>
                setSelectedWorkerId(event.target.value)
              }
            >
              <option value="">Selecciona un trabajador</option>
              {state.items.map((document) => (
                <option key={document.id} value={document.workerProfileId}>
                  {document.documentType} — {document.reviewStatus}
                </option>
              ))}
            </select>
          </label>
        </div>
      ) : null}

      {state.kind === 'incidents' ? (
        <div>
          <h3>Incidencias</h3>
          <ul className="worker-offerings">
            {state.items.map((incident) => (
              <li key={incident.id}>
                <span>
                  {incident.subject} — {incident.status}
                </span>
                <span>
                  <button
                    type="button"
                    onClick={() => void changeStatus(incident.id, 'in_review')}
                  >
                    Iniciar revisión
                  </button>
                  <button
                    type="button"
                    onClick={() => void changeStatus(incident.id, 'resolved')}
                  >
                    Resolver
                  </button>
                  <button
                    type="button"
                    onClick={() => void changeStatus(incident.id, 'closed')}
                  >
                    Cerrar
                  </button>
                </span>
              </li>
            ))}
          </ul>
        </div>
      ) : null}
    </section>
  );
}
