import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useApp, useRemote } from '../app/hooks';
import { Badge, Card, Empty, ErrorBox, Info, Loading, PageTitle, Pagination } from '../components/ui';
import type { Booking, Payment } from '../services/types';
import { date, money, shortId } from '../services/types';

function PaymentRow({ booking }: { booking: Booking }) {
  const payment = useRemote<Payment | null>(`/bookings/${booking.id}/payment`);
  return <Card><div className="row between"><h3>Reserva #CH-{shortId(booking.id)}</h3><Badge status={payment.data?.status || 'Pending'} /></div><p className="small">{date(booking.startsAt)}</p><div className="total-row"><span>Importe del servicio</span><strong>{money(booking.amount)} MXN</strong></div><ErrorBox message={payment.error} retry={payment.reload} />{payment.loading ? <Loading /> : payment.data ? <p className="small">{payment.data.provider === 'stripe' ? 'Stripe' : 'Mercado Pago'} · Referencia #{shortId(payment.data.id)}</p> : <p className="muted">Sin pago registrado.</p>}<Link className="button button-secondary full" to={`/reservas/${booking.id}`}>Consultar reserva →</Link></Card>;
}
export function Payments({ earnings = false }: { earnings?: boolean }) {
  const { user } = useApp();
  const [page, setPage] = useState(1);
  const bookings = useRemote<Booking[]>(`/bookings?page=${page}`);
  const completed = bookings.data?.filter(b => b.status === 'Completed') || [];
  return <div className="page-stack"><PageTitle title={earnings && user?.role === 'Worker' ? 'Pagos de tus servicios' : 'Pagos y transacciones'} subtitle="Consulta los importes y las confirmaciones del proveedor." back /><Info><h3>Pagos con proveedores externos</h3><p>CleanHome no es un monedero. Los estados de cobro y reembolso se confirman con Stripe o Mercado Pago. Las liquidaciones a profesionales requieren seguimiento de la administración.</p></Info>{earnings && <Card><span className="overline">SERVICIOS COMPLETADOS EN ESTA PÁGINA</span><h1 className="balance">{money(completed.reduce((n, b) => n + b.amount, 0))} <small>MXN</small></h1><div className="stats-grid"><div><strong>{completed.length}</strong><small>Servicios completados</small></div><div><strong>{bookings.data?.filter(b => ['Confirmed', 'InProgress'].includes(b.status)).length || 0}</strong><small>Reservas activas</small></div></div><p className="field-hint">Total de tarifas de servicio, no saldo disponible ni confirmación de liquidación bancaria.</p></Card>}<ErrorBox message={bookings.error} retry={bookings.reload} />{bookings.loading ? <Loading /> : bookings.data?.length ? bookings.data.map(b => <PaymentRow key={b.id} booking={b} />) : <Empty title="Aún no hay transacciones" text="El historial de pagos se mostrará junto a tus reservas." />}<Pagination page={page} hasNext={bookings.data?.length === 20} onPage={setPage} /></div>;
}

