import { useEffect, useState } from 'react';
import { useAuth } from '../useAuth';
import { getSessions } from '../../../services/auth';
import type { SessionResponse } from '../../../types/auth';

export function SessionPanel() {
  const { signOut } = useAuth();
  const [sessions, setSessions] = useState<SessionResponse[]>([]);

  useEffect(() => {
    void getSessions().then(setSessions);
  }, []);

  return (
    <section className="worker-panel">
      <h2>Sesiones activas</h2>
      {sessions.length === 0 ? (
        <p>Sin sesiones activas.</p>
      ) : (
        <ul className="worker-offerings">
          {sessions.map((session) => (
            <li key={session.id}>
              <span>
                {session.deviceName} — {session.platform} —{' '}
                {session.isCurrent ? 'actual' : 'cerrable'}
              </span>
            </li>
          ))}
        </ul>
      )}
      <button type="button" onClick={() => void signOut()}>
        Cerrar sesión actual
      </button>
    </section>
  );
}
