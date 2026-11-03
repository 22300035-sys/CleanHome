import type { WorkerListingResponse } from '../../../types/marketplace';

interface WorkerCardProps {
  worker: WorkerListingResponse;
}

export function WorkerCard({ worker }: WorkerCardProps) {
  return (
    <li className="worker-card">
      <header className="worker-card__header">
        <h3>{worker.displayName}</h3>
        <span>{worker.serviceCity}</span>
      </header>
      <p>{worker.description}</p>
      <ul className="worker-card__offerings">
        {worker.offerings.length === 0 ? (
          <li>Sin servicios publicados.</li>
        ) : (
          worker.offerings.map((offering) => (
            <li key={offering.id}>
              {offering.categoryName}: ${offering.referencePrice.toFixed(2)} —{' '}
              {offering.description}
            </li>
          ))
        )}
      </ul>
    </li>
  );
}
