import { useState } from 'react';
import { Link, Navigate } from 'react-router-dom';
import { useApp, useRemote, useClock } from '../app/hooks';
import { Avatar, Badge, Button, Card, Empty, ErrorBox, Icon, Info, Loading, PageTitle, Pagination, SectionTitle } from '../components/ui';
import type { Booking, Page, Quote, ServiceRequest, Worker } from '../services/types';
import { date, shortId, title } from '../services/types';

export function RequestCard({ request, compact = false }: { request: ServiceRequest; compact?: boolean }) {
  const { user } = useApp();
  const worker = user?.role === 'Worker';
  const quotes = useRemote<Quote[]>(!worker && request.status === 'Open' ? `/requests/${request.id}/quotes` : null);
  return <Card className="request-card">
    <div className="row between"><Badge>{worker ? 'Cotización abierta' : request.status === 'Open' ? `${quotes.data?.length ?? '…'} cotizaciones recibidas` : request.status === 'Booked' ? 'Reserva confirmada' : request.status === 'Completed' ? 'Servicio completado' : 'Cancelada'}</Badge><span className="service-glyph"><Icon name="imgContainer12" /></span></div>
    <h3>{title(request.description)}</h3><p className="muted">{compact ? `Solicitud #${shortId(request.id)}` : request.description.split('\n').slice(1).join(' · ')}</p>
    {quotes.error && <p className="field-error">No se pudieron cargar las cotizaciones.</p>}
    <div className="detail-strip"><span><Icon name="imgContainer7" />{date(request.startsAt)}</span><span><Icon name="imgContainer8" />{worker ? request.city : `${request.durationMinutes / 60} horas estimadas`}</span></div>
    {!compact && <p className="muted small">{request.city}</p>}
    {request.status === 'Open' && <Link className="button button-primary full" to={worker ? `/cotizar/${request.id}` : `/solicitudes/${request.id}/cotizaciones`}>{worker ? 'Enviar cotización' : 'Ver cotizaciones'} <span aria-hidden="true">→</span></Link>}
  </Card>;
}

function NextBooking({ booking }: { booking: Booking }) {
  const { demo } = useApp();
  const worker = useRemote<Worker>(`/workers/${booking.workerId}`);
  const request = useRemote<ServiceRequest>(`/requests/${booking.requestId}`);
  return <Card><div className="row between align-start"><div className="row"><Avatar name={worker.data?.displayName} src={demo ? '/design/3d6ca1eb-1caf-4f00-a1d1-557b68dfbcdd.png' : undefined} /><div><h3>{request.data ? title(request.data.description) : "Limpieza profesional"}</h3><p className="small">{worker.data?.displayName || 'Especialista asignado'} {worker.data?.rating && <strong>★ {worker.data.rating}</strong>}</p></div></div><Badge status={booking.status} /></div><div className="detail-strip"><span><Icon name="imgContainer7" />{date(booking.startsAt)}</span><span><Icon name="imgContainer8" />{(new Date(booking.endsAt).getTime() - new Date(booking.startsAt).getTime()) / 3600000} hrs</span></div><div className="row"><Link to={`/reservas/${booking.id}`} className="button button-secondary grow"><Icon name="imgContainer9" />Ver reserva</Link><Link to={`/mensajes/${booking.id}`} className="icon-button soft" aria-label="Abrir chat de la próxima limpieza"><Icon name="imgContainer20" /></Link></div></Card>;
}

export function Home() {
  const { user } = useApp();
  const requests = useRemote<Page<ServiceRequest>>(user && !user.isAdmin && user.role === 'Customer' ? '/requests?page=1&size=20' : null);
  const bookings = useRemote<Booking[]>(user && !user.isAdmin && user.role === 'Customer' ? '/bookings?page=1' : null);
  if (user?.isAdmin) return <Navigate to="/admin" replace />;
  if (user?.role === 'Worker') return <Radar />;
  const next = bookings.data?.filter(b => b.status === 'Confirmed').sort((a, b) => +new Date(a.startsAt) - +new Date(b.startsAt))[0];
  const open = requests.data?.items.filter(r => r.status === 'Open') ?? [];
  return <div className="page-stack home-page"><div className="greeting"><div><h1>Hola, {user?.displayName.split(' ')[0]}<span className="wave">👋</span></h1><p>Tu hogar está cuidado con profesionales<br />con identidad verificada</p></div><span className="verified-orb"><Icon name="imgContainer" /><i /></span></div>
    <Card className="home-hero"><div className="row between"><span className="eyebrow"><Icon name="imgContainer1" /> RESPUESTA RÁPIDA</span><Icon name="imgIcon" /></div><div><h2>¿Qué necesitas limpiar?</h2><p>Publica tu solicitud y recibe cotizaciones de limpiadores verificados en tu zona. Tú eliges la propuesta que más te conviene.</p></div><Link className="button button-primary full" to="/solicitar/1"><Icon name="imgContainer2" />+ Solicitar limpieza</Link></Card>
    <section><SectionTitle to="/catalogo" action="Ver catálogo">Servicios frecuentes</SectionTitle><div className="service-grid">{[{ name: 'General', icon: 'imgContainer3', value: 'general' }, { name: 'Profunda', icon: 'imgContainer4', value: 'profunda' }, { name: 'Cocina', icon: 'imgContainer5', value: 'cocina' }, { name: 'Cristales', icon: 'imgContainer6', value: 'ventanas' }].map(s => <Link to={`/solicitar/1?servicio=${s.value}`} className="service-tile" key={s.name}><span><Icon name={s.icon} /></span>{s.name}</Link>)}</div></section>
    <section><SectionTitle>Próxima limpieza</SectionTitle><ErrorBox message={bookings.error} retry={bookings.reload} />{bookings.loading ? <Loading /> : next ? <NextBooking booking={next} /> : <Empty title="Tu próxima limpieza empieza aquí" text="Cuando aceptes una cotización, encontrarás aquí tu reserva." />}</section>
    <section><SectionTitle to="/solicitudes" action="Ver todas">Solicitudes recientes</SectionTitle><ErrorBox message={requests.error} retry={requests.reload} />{requests.loading ? <Loading /> : open.length ? open.slice(0, 2).map(r => <RequestCard key={r.id} request={r} compact />) : <Empty title="Todo en orden" text="Todavía no tienes solicitudes abiertas." action="Solicitar limpieza" to="/solicitar/1" />}</section>
    <Info><h3>Confianza CleanHome</h3><p>Consulta la verificación del profesional, compara propuestas y gestiona cada reserva desde tu cuenta.</p></Info>
  </div>;
}

export function Radar() {
  const { user, demo, reloadProfile } = useApp();
  const [page, setPage] = useState(1);
  const [filter, setFilter] = useState('Todas');
  const [search, setSearch] = useState('');
  const [available, setAvailable] = useState(true);
  const now = useClock();
  const requests = useRemote<Page<ServiceRequest>>(user?.isVerified && available ? `/requests?page=${page}&size=20` : null);
  const items = requests.data?.items.filter(r => (!search || `${r.city} ${r.description}`.toLowerCase().includes(search.toLowerCase())) && (filter !== 'Hoy / Mañana' || +new Date(r.startsAt) < now + 172800000)) ?? [];
  return <div className="page-stack"><Card><div className="row between"><div><span className="overline">PANEL DEL ESPECIALISTA</span><h1>Hola, {user?.displayName.split(' ')[0]} 👋</h1></div><button className={`switch ${available ? 'on' : ''}`} role="switch" aria-checked={available} aria-label="Mostrar oportunidades de trabajo" onClick={() => setAvailable(!available)}><span /></button></div><div className="row between"><Badge tone={user?.isVerified ? 'green' : 'blue'}>{user?.isVerified ? '● Disponible para trabajar' : 'Verificación pendiente'}</Badge><Link className="text-link" to="/ganancias">Pagos y ganancias →</Link></div></Card>
    {!user?.isVerified ? <><Info><h3>Completa tu verificación</h3><p>Sube un documento de identidad para que el equipo revise tu perfil. Podrás cotizar una vez aprobado.</p></Info><Link to="/documentos" className="button button-primary full">Subir mis documentos →</Link><Button variant="secondary" onClick={() => void reloadProfile()}>Ya envié mis documentos, actualizar estado</Button></> : <>
      <div className="tabs scroll-tabs">{['Todas', 'Hoy / Mañana'].map(f => <button key={f} className={filter === f ? 'selected' : ''} onClick={() => setFilter(f)}>{f}</button>)}</div><label className="sr-only" htmlFor="radar-search">Buscar oportunidades por ciudad o servicio</label><input id="radar-search" className="search-input" type="search" placeholder="Busca por ciudad o tipo de limpieza…" value={search} onChange={e => setSearch(e.target.value)} />
      {demo && <div className="map-preview"><img src="/design/568b8a63-f64b-485e-95c3-1d18e3a77e01.png" alt="Mapa ilustrativo de la zona de Polanco" /><span>● Polanco · Granada · Anzures</span></div>}
      <p className="small"><strong>{items.length}</strong> oportunidades en esta página</p><ErrorBox message={requests.error} retry={requests.reload} />{requests.loading ? <Loading /> : !available ? <Empty title="Exploración pausada" text="Activa el interruptor para volver a ver oportunidades." /> : items.length ? items.map(r => <RequestCard key={r.id} request={r} />) : <Empty title="No hay oportunidades por ahora" text="Prueba otra búsqueda o vuelve a consultar más tarde." />}
      {requests.data && <Pagination page={page} hasNext={requests.data.items.length === 20} onPage={setPage} />}
    </>}
    <Info><h3>Privacidad de datos protegida</h3><p>La dirección completa del cliente solo se muestra después de que tu cotización sea aceptada.</p></Info>
  </div>;
}

export function Requests() {
  const { user } = useApp();
  const [page, setPage] = useState(1);
  const requests = useRemote<Page<ServiceRequest>>(`/requests?page=${page}&size=20`);
  return <div className="page-stack"><PageTitle title={user?.role === 'Worker' ? 'Oportunidades' : 'Mis solicitudes'} subtitle="Consulta las propuestas y el estado de cada servicio." />{user?.role === 'Customer' && <Link to="/solicitar/1" className="button button-primary full">+ Solicitar limpieza</Link>}<ErrorBox message={requests.error} retry={requests.reload} />{requests.loading ? <Loading /> : requests.data?.items.length ? requests.data.items.map(r => <RequestCard key={r.id} request={r} />) : <Empty title="Aún no hay solicitudes" text="Tu próxima limpieza puede empezar hoy." />}{requests.data && <Pagination page={page} hasNext={requests.data.items.length === 20} onPage={setPage} />}</div>;
}



