import { useCallback, useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { useBookings } from '../useBookings';
import type { BookingDto } from '../../../types/bookings';

interface ServiceQuotesPanelProps {
  requestId: string;
}

export function ServiceQuotesPanel({
  requestId,
}: ServiceQuotesPanelProps) {
  const {
    state,
    getQuotes,
    createQuote,
    acceptQuote,
    withdrawQuote,
    startBooking,
    completeBooking,
    cancelBooking,
  } = useBookings();

  const [amount, setAmount] = useState('0');
  const [description, setDescription] = useState('');
  const [expiresAt, setExpiresAt] = useState('');
  const [booking, setBooking] = useState<BookingDto | null>(null);

  const load = useCallback(
    () => getQuotes(requestId),
    [getQuotes, requestId]
  );

  useEffect(() => {
    void load();
  }, [load]);

  const submit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const parsed = new Date(expiresAt);

    const input = {
      amount: Number(amount),
      description,
      expiresAt: Number.isNaN(parsed.getTime())
        ? new Date().toISOString()
        : parsed.toISOString(),
    };

    await createQuote(requestId, input);
  };

  const accept = async (quoteId: string) => {
    const created = await acceptQuote(requestId, quoteId);

    setBooking(created);
  };

  const withdraw = async (quoteId: string) => {
    await withdrawQuote(quoteId);
  };

  const start = async () => {
    if (booking) {
      await startBooking(booking.id);
    }
  };

  const complete = async () => {
    if (booking) {
      await completeBooking(booking.id);
    }
  };

  const cancel = async () => {
    if (booking) {
      await cancelBooking(booking.id, 'Cancelada por el trabajador');
    }
  };

  if (state.kind === 'error') {
    return (
      <p role="alert">{state.message}</p>
    );
  }

  if (state.kind !== 'quotes') {
    return <p>Cargando cotizaciones…</p>;
  }

  return (
    <section className="worker-panel">
      <h3>Cotizaciones</h3>
      {state.items.length === 0 ? (
        <p>Sin cotizaciones por ahora.</p>
      ) : (
        <ul className="worker-offerings">
          {state.items.map((quote) => (
            <li key={quote.id}>
              <span>
                ${quote.amount.toFixed(2)} — {quote.status}
              </span>
              <span>
                <button type="button" onClick={() => void accept(quote.id)}>
                  Aceptar cotización
                </button>
                <button type="button" onClick={() => void withdraw(quote.id)}>
                  Retirar
                </button>
              </span>
            </li>
          ))}
        </ul>
      )}
      <form onSubmit={submit}>
        <label>
          Monto
          <input
            type="number"
            step="0.01"
            min="0"
            value={amount}
            onChange={(event) => setAmount(event.target.value)}
          />
        </label>
        <label>
          Descripción
          <input
            value={description}
            onChange={(event) => setDescription(event.target.value)}
          />
        </label>
        <label>
          Expira
          <input
            type="datetime-local"
            value={expiresAt}
            onChange={(event) => setExpiresAt(event.target.value)}
          />
        </label>
        <button type="submit">Crear cotización</button>
      </form>
      {booking ? (
        <div className="worker-offerings">
          <p>
            Reserva #{booking.id.slice(0, 8)} — {booking.status}
          </p>
          {booking.status === 'confirmed' ? (
            <button type="button" onClick={() => void start()}>
              Iniciar trabajo
            </button>
          ) : null}
          {booking.status === 'in_progress' ? (
            <button type="button" onClick={() => void complete()}>
              Completar trabajo
            </button>
          ) : null}
          {(booking.status === 'confirmed' ||
            booking.status === 'in_progress') ? (
            <button type="button" onClick={() => void cancel()}>
              Cancelar reserva
            </button>
          ) : null}
        </div>
      ) : null}
    </section>
  );
}
