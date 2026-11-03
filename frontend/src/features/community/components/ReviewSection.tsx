import { useState } from 'react';
import type { FormEvent } from 'react';
import { useCommunity } from '../useCommunity';

interface ReviewSectionProps {
  bookingId: string;
}

export function ReviewSection({ bookingId }: ReviewSectionProps) {
  const { createReview } = useCommunity();
  const [rating, setRating] = useState('5');
  const [comment, setComment] = useState('');

  const submit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    await createReview(bookingId, Number(rating), comment);
  };

  return (
    <section className="worker-panel">
      <h2>Reseña</h2>
      <form onSubmit={submit}>
        <label>
          Calificación
          <select value={rating} onChange={(event) => setRating(event.target.value)}>
            <option value="5">5 — Excelente</option>
            <option value="4">4 — Muy bueno</option>
            <option value="3">3 — Bueno</option>
            <option value="2">2 — Regular</option>
            <option value="1">1 — Malo</option>
          </select>
        </label>
        <label>
          Comentario
          <textarea value={comment} onChange={(event) => setComment(event.target.value)} />
        </label>
        <button type="submit">Publicar reseña</button>
      </form>
    </section>
  );
}
