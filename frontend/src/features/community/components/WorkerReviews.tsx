import { useEffect } from 'react';
import { useCommunity } from '../useCommunity';

interface WorkerReviewsProps {
  workerProfileId: string;
}

export function WorkerReviews({ workerProfileId }: WorkerReviewsProps) {
  const { state, loadWorkerRating } = useCommunity();

  useEffect(() => {
    void loadWorkerRating(workerProfileId);
  }, [loadWorkerRating, workerProfileId]);

  if (state.kind !== 'workerRating') {
    return <p>Cargando reseñas…</p>;
  }

  const rating = state.rating;

  return (
    <section className="worker-panel">
      <h2>Reseñas del trabajador</h2>
      <p>
        Promedio: {rating.averageRating.toFixed(1)} ·{' '}
        {rating.reviewCount}{' '}
        reseñas
      </p>
      {rating.reviews.length === 0 ? (
        <p>Sin reseñas publicadas.</p>
      ) : (
        <ul className="worker-offerings">
          {rating.reviews.map((review) => (
            <li key={review.id}>
              <span>
                {review.rating}/5 — {review.comment}
              </span>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
