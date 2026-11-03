import { useCallback, useEffect } from 'react';
import { useMarketplace } from '../useMarketplace';
import { WorkerCard } from './WorkerCard';

interface WorkerListProps {
  city?: string;
  categoryId?: string;
}

export function WorkerList({ city, categoryId }: WorkerListProps) {
  const { state, browseWorkers } = useMarketplace();

  const load = useCallback(
    () => browseWorkers(city, categoryId),
    [browseWorkers, city, categoryId]
  );

  useEffect(() => {
    void load();
  }, [load]);

  if (state.kind === 'error') {
    return (
      <p className="worker-list__error" role="alert">
        {state.message}
      </p>
    );
  }

  if (state.kind !== 'workers') {
    return <p>Cargando trabajadores…</p>;
  }

  if (state.items.length === 0) {
    return <p>No hay trabajadores disponibles.</p>;
  }

  return (
    <ul className="worker-list">
      {state.items.map((worker) => (
        <WorkerCard key={worker.id} worker={worker} />
      ))}
    </ul>
  );
}
