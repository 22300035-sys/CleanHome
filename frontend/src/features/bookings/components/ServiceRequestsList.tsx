import { useCallback, useEffect, useState } from 'react';
import { useBookings } from '../useBookings';
import type {
  ServiceRequestDetailDto,
  ServiceRequestDto,
} from '../../../types/bookings';
import { ServiceRequestDetail } from './ServiceRequestDetail';

export function ServiceRequestsList() {
  const { state, getRequests, getRequestDetail } = useBookings();
  const [detail, setDetail] =
    useState<ServiceRequestDetailDto | null>(null);

  const load = useCallback(() => getRequests(), [getRequests]);

  useEffect(() => {
    void load();
  }, [load]);

  const openRequest = useCallback(
    async (requestId: string) => {
      const requestDetail = await getRequestDetail(requestId);

      setDetail(requestDetail);
    },
    [getRequestDetail]
  );

  if (state.kind !== 'requests') {
    return <p>Cargando solicitudes…</p>;
  }

  if (state.items.length === 0) {
    return <p>No tienes solicitudes activas.</p>;
  }

  return (
    <section className="worker-panel">
      <h2>Mis solicitudes</h2>
      <ul className="worker-offerings">
        {state.items.map((request: ServiceRequestDto) => (
          <li key={request.id}>
            <button type="button" onClick={() => void openRequest(request.id)}>
              {request.title} — {request.city} — {request.status}
            </button>
          </li>
        ))}
      </ul>
      {detail ? <ServiceRequestDetail detail={detail} /> : null}
    </section>
  );
}
