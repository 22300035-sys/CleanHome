import { useCallback, useEffect, useState } from 'react';
import { useBookings } from '../useBookings';
import { ServiceQuotesPanel } from './ServiceQuotesPanel';

export function AvailableServiceRequests() {
  const { state, getAvailableRequests, getRequestDetail } = useBookings();
  const [detailId, setDetailId] = useState<string | null>(null);

  const load = useCallback(
    () => getAvailableRequests(),
    [getAvailableRequests]
  );

  useEffect(() => {
    void load();
  }, [load]);

  const openRequest = useCallback(
    async (requestId: string) => {
      setDetailId(requestId);

      const detail = await getRequestDetail(requestId);

      return detail;
    },
    [getRequestDetail]
  );

  if (state.kind !== 'available') {
    return <p>Cargando solicitudes disponibles…</p>;
  }

  if (state.items.length === 0) {
    return <p>No hay solicitudes disponibles.</p>;
  }

  return (
    <section className="worker-panel">
      <h2>Solicitudes disponibles</h2>
      <ul className="worker-offerings">
        {state.items.map((request) => (
          <li key={request.id}>
            <button type="button" onClick={() => void openRequest(request.id)}>
              {request.title} — {request.city} — {request.status}
            </button>
          </li>
        ))}
      </ul>
      {detailId ? <ServiceQuotesPanel requestId={detailId} /> : null}
    </section>
  );
}
