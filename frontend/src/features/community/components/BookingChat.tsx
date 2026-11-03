import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { useCommunity } from '../useCommunity';

interface BookingChatProps {
  bookingId: string;
}

export function BookingChat({ bookingId }: BookingChatProps) {
  const { state, loadMessages, sendMessage, markRead } = useCommunity();
  const [content, setContent] = useState('');
  const messages =
    state.kind === 'messages' ? state.items : [];

  useEffect(() => {
    void loadMessages(bookingId);
  }, [loadMessages, bookingId]);

  useEffect(() => {
    void markRead(bookingId);
  }, [markRead, bookingId, messages.length]);

  const submit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    if (!content) {
      return;
    }

    await sendMessage(bookingId, content);
    setContent('');
  };

  if (state.kind !== 'messages') {
    return <p>Cargando conversación…</p>;
  }

  return (
    <section className="worker-panel">
      <h2>Conversación</h2>
      <ul className="worker-offerings">
        {state.items.map((message) => (
          <li key={message.id}>
            <span>
              {message.content} — {message.readAt ? 'leído' : 'no leído'}
            </span>
          </li>
        ))}
      </ul>
      <form onSubmit={submit}>
        <label>
          Mensaje
          <input
            value={content}
            onChange={(event) => setContent(event.target.value)}
          />
        </label>
        <button type="submit">Enviar</button>
      </form>
    </section>
  );
}
