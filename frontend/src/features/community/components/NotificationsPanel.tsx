import { useEffect } from 'react';
import { useCommunity } from '../useCommunity';

export function NotificationsPanel() {
  const { state, loadNotifications, markNotificationRead } = useCommunity();

  useEffect(() => {
    void loadNotifications();
  }, [loadNotifications]);

  if (state.kind !== 'notifications') {
    return <p>Cargando notificaciones…</p>;
  }

  return (
    <section className="worker-panel">
      <h2>Notificaciones</h2>
      {state.items.length === 0 ? (
        <p>Sin notificaciones.</p>
      ) : (
        <ul className="worker-offerings">
          {state.items.map((notification) => (
            <li key={notification.id}>
              <span>
                {notification.title} — {notification.body}
              </span>
              <button
                type="button"
                onClick={() => void markNotificationRead(notification.id)}
              >
                Marcar leída
              </button>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
