import { useEffect } from 'react';
import { useDocumentHistory } from '../useDocumentHistory';

export function DocumentHistorySection() {
  const { documents, load } = useDocumentHistory();

  useEffect(() => {
    void load();
  }, [load]);

  if (documents.length === 0) {
    return <p>No tienes documentos subidos.</p>;
  }

  return (
    <section className="worker-panel">
      <h2>Mis documentos</h2>
      <ul className="worker-offerings">
        {documents.map((document) => (
          <li key={document.id}>
            <span>
              {document.documentType} — {document.reviewStatus} —{' '}
              {new Date(document.uploadedAt).toLocaleString()}
            </span>
          </li>
        ))}
      </ul>
    </section>
  );
}
