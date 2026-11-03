import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { useAuth } from './features/auth/useAuth';
import { SessionPanel } from './features/auth/components/SessionPanel';
import { useBookings } from './features/bookings/useBookings';
import { WorkerList } from './features/marketplace/components/WorkerList';
import { WorkerProfileSection } from './features/marketplace/components/WorkerProfileSection';
import { OfferingSection } from './features/marketplace/components/OfferingSection';
import { AvailabilitySection } from './features/marketplace/components/AvailabilitySection';
import { CustomerProfileSection } from './features/marketplace/components/CustomerProfileSection';
import { ServiceRequestForm } from './features/bookings/components/ServiceRequestForm';
import { ServiceRequestsList } from './features/bookings/components/ServiceRequestsList';
import { MyBookings } from './features/bookings/components/MyBookings';
import { AvailableServiceRequests } from './features/bookings/components/AvailableServiceRequests';
import { AdminPanel } from './features/admin/components/AdminPanel';
import { BookingChat } from './features/community/components/BookingChat';
import { NotificationsPanel } from './features/community/components/NotificationsPanel';
import { IncidentPanel } from './features/community/components/IncidentPanel';
import { ReviewSection } from './features/community/components/ReviewSection';
import { DocumentUploadSection } from './features/documents/components/DocumentUploadSection';
import { DocumentHistorySection } from './features/documents/components/DocumentHistorySection';
import { LocationSection } from './features/locations/components/LocationSection';
import { AccountRecoverySection } from './features/auth/components/AccountRecoverySection';
import { register } from './services/auth';
import './App.css';

function App() {
  const { state: authState, signIn } = useAuth();
  const { state: bookingsState, getBookings } = useBookings();
  const [mode, setMode] = useState<'login' | 'register'>('login');
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [deviceName, setDeviceName] = useState('CleanHomeWeb');
  const [platform, setPlatform] = useState('web');
  const [error, setError] = useState<string | null>(null);

  const onSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    setError(null);

    try {
      if (mode === 'register') {
        await register({
          firstName,
          lastName,
          email,
          password,
        });

        setMode('login');
        setError('Cuenta creada. Inicia sesión para continuar.');

        return;
      }

      await signIn({
        email,
        password,
        deviceName,
        platform,
      });
    } catch (caught) {
      setError(
        caught instanceof Error
          ? caught.message
          : 'No fue posible completar la operación.'
      );
    }
  };

  const switchMode = (nextMode: 'login' | 'register') => {
    setMode(nextMode);
    setError(null);
  };

  const activeBookingId =
    bookingsState.kind === 'bookings' && bookingsState.items.length > 0
      ? bookingsState.items[0].id
      : null;

  useEffect(() => {
    if (authState.kind !== 'authenticated') {
      return;
    }

    void getBookings();
  }, [authState.kind, getBookings]);

  if (authState.kind === 'loading') {
    return (
      <main className="app">
        <p>Cargando sesión…</p>
      </main>
    );
  }

  if (authState.kind === 'unauthenticated') {
    return (
      <main className="app">
        <h1>CleanHome</h1>
        <p>Marketplace de servicios de limpieza.</p>
        <form onSubmit={onSubmit}>
          {mode === 'register' ? (
            <>
              <label>
                Nombre
                <input
                  value={firstName}
                  onChange={(event) => setFirstName(event.target.value)}
                />
              </label>
              <label>
                Apellido
                <input
                  value={lastName}
                  onChange={(event) => setLastName(event.target.value)}
                />
              </label>
            </>
          ) : null}
          <label>
            Correo
            <input
              type="email"
              value={email}
              onChange={(event) => setEmail(event.target.value)}
            />
          </label>
          <label>
            Contraseña
            <input
              type="password"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
            />
          </label>
          {mode === 'login' ? (
            <>
              <label>
                Dispositivo
                <input
                  value={deviceName}
                  onChange={(event) => setDeviceName(event.target.value)}
                />
              </label>
              <label>
                Plataforma
                <input
                  value={platform}
                  onChange={(event) => setPlatform(event.target.value)}
                />
              </label>
              <button type="submit">Iniciar sesión</button>
            </>
          ) : (
            <button type="submit">Crear cuenta</button>
          )}
        </form>
        <div>
          <button type="button" onClick={() => switchMode('login')}>
            Iniciar sesión
          </button>
          <button type="button" onClick={() => switchMode('register')}>
            Crear cuenta
          </button>
        </div>
        {error ? <p role="alert">{error}</p> : null}
        <AccountRecoverySection />
      </main>
    );
  }

  return (
    <main className="app">
      <h1>CleanHome</h1>
      <p>Marketplace de servicios de limpieza.</p>
      <WorkerList />
      <WorkerProfileSection />
      <OfferingSection />
      <AvailabilitySection />
      <CustomerProfileSection />
      <ServiceRequestForm />
      <ServiceRequestsList />
      <MyBookings />
      <DocumentUploadSection />
      <DocumentHistorySection />
      <LocationSection />
      <AvailableServiceRequests />
      <AdminPanel />
      <NotificationsPanel />
      <IncidentPanel />
      {activeBookingId ? <BookingChat bookingId={activeBookingId} /> : null}
      {activeBookingId ? <ReviewSection bookingId={activeBookingId} /> : null}
      <SessionPanel />
    </main>
  );
}

export default App;
