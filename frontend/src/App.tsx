import { Component } from 'react';
import type { ReactNode } from 'react';
import { BrowserRouter, Link, Route, Routes } from 'react-router-dom';
import { AppProvider } from './app/context';
import { Shell } from './components/Shell';
import { Access, Recovery, ResetPassword } from './pages/Access';
import { Home, Requests } from './pages/Dashboard';
import { Catalog, RequestWizard } from './pages/RequestWizard';
import { QuoteForm, Quotes, WorkerProfile } from './pages/Quotes';
import { BookingPage, Bookings, Checkout, PaymentReturn } from './pages/Bookings';
import { Addresses, Documents, Help, Notifications, ProfilePage } from './pages/Account';
import { Conversation, Inbox } from './pages/Messages';
import { Payments } from './pages/Payments';
import { Admin } from './pages/Admin';
import './App.css';
import { PwaUpdate } from './components/PwaUpdate';
import { Landing } from './pages/Landing';

class ErrorBoundary extends Component<{ children: ReactNode }, { failed: boolean }> {
  state = { failed: false };
  static getDerivedStateFromError() { return { failed: true }; }
  render() {
    if (this.state.failed) return <main className="access-page"><h1>No pudimos mostrar esta pantalla</h1><p>Vuelve a cargar para intentar de nuevo. Tus reservas guardadas siguen en tu cuenta.</p><button className="button button-primary" onClick={() => window.location.reload()}>Volver a cargar</button></main>;
    return this.props.children;
  }
}
export default function App() {
  return <ErrorBoundary><BrowserRouter><AppProvider><a className="skip-link" href="#contenido">Saltar al contenido</a><Routes>
    <Route path="/" element={<Landing />} />
    <Route path="/acceso" element={<Access />} />
    <Route path="/recuperar" element={<Recovery />} />
    <Route path="/restablecer" element={<ResetPassword />} />
    <Route element={<Shell />}>
      <Route path="inicio" element={<Home />} />
      <Route path="catalogo" element={<Catalog />} />
      <Route path="solicitar/:step" element={<RequestWizard />} />
      <Route path="solicitudes" element={<Requests />} />
      <Route path="solicitudes/:id/cotizaciones" element={<Quotes />} />
      <Route path="cotizar/:id" element={<QuoteForm />} />
      <Route path="profesionales/:id" element={<WorkerProfile />} />
      <Route path="reservas" element={<Bookings />} />
      <Route path="reservas/:id" element={<BookingPage />} />
      <Route path="reservas/:id/pago" element={<Checkout />} />
      <Route path="payments/:result" element={<PaymentReturn />} />
      <Route path="mensajes" element={<Inbox />} />
      <Route path="mensajes/:id" element={<Conversation />} />
      <Route path="perfil" element={<ProfilePage />} />
      <Route path="direcciones" element={<Addresses />} />
      <Route path="documentos" element={<Documents />} />
      <Route path="notificaciones" element={<Notifications />} />
      <Route path="ganancias" element={<Payments earnings />} />
      <Route path="pagos" element={<Payments />} />
      <Route path="ayuda" element={<Help />} />
      <Route path="admin" element={<Admin />} />
      <Route path="admin/:section" element={<Admin />} />
      <Route path="*" element={<div className="page-stack"><h1>Esta página no existe</h1><Link to="/inicio" className="button button-primary">Volver al inicio</Link></div>} />
    </Route>
  </Routes><PwaUpdate /></AppProvider></BrowserRouter></ErrorBoundary>;
}

