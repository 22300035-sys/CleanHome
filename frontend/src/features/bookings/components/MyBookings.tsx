import { useCallback, useEffect, useState } from 'react';
import { useBookings } from '../useBookings';
import type { BookingDto } from '../../../types/bookings';

export function MyBookings() {
  const { state, getBookings, cancelBooking } = useBookings();
  const [confirmingCancel, setConfirmingCancel] = useState<string | null>(
    null
  );

  const load = useCallback(() => getBookings(), [getBookings]);

  useEffect(() => {
    void load();
  }, [load]);

  if (state.kind !== 'bookings') {
    return <p>Cargando reservas…</p>;
  }

  if (state.items.length === 0) {
    return <p>No tienes reservas activas.</p>;
  }

  const cancel = async (bookingId: string) => {
    await cancelBooking(bookingId, 'Cancelada por el cliente');
    setConfirmingCancel(null);
  };

  return (
    <section className="worker-panel">
      <h2>Mis reservas</h2>
      <ul className="worker-offerings">
        {state.items.map((booking: BookingDto) => (
          <li key={booking.id}>
            <span>
              Reserva #{booking.id.slice(0, 8)} — ${booking.agreedAmount.toFixed(2)}{' '}
              — {booking.status}
            </span>
            {confirmingCancel === booking.id ? (
              <button type="button" onClick={() => void cancel(booking.id)}>
                Confirmar cancelación
              </button>
            ) : (
              <button
                type="button"
                onClick={() => setConfirmingCancel(booking.id)}
              >
                Cancelar
              </button>
            )}
          </li>
        ))}
      </ul>
    </section>
  );
}
