import { useEffect, useMemo, useRef, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import { useAction, useApp, useRemote } from '../app/hooks';
import { Avatar, Badge, Button, Empty, ErrorBox, Icon, Info, Loading, PageTitle, Pagination } from '../components/ui';
import { HUB_URL, jsonBody, liveApi } from '../services/api';
import type { Booking, BookingDetail, Message } from '../services/types';
import { date, shortId } from '../services/types';

export function Inbox() {
  const [page, setPage] = useState(1);
  const bookings = useRemote<Booking[]>(`/bookings?page=${page}`);
  return <div className="page-stack"><PageTitle title="Mensajes" subtitle="Conversaciones privadas de tus reservas." /><ErrorBox message={bookings.error} retry={bookings.reload} />{bookings.loading ? <Loading /> : bookings.data?.length ? bookings.data.map(b => <Link className="conversation-link" to={`/mensajes/${b.id}`} key={b.id}><Avatar name="CleanHome" /><div><h2>Reserva #CH-{shortId(b.id)}</h2><p>{date(b.startsAt)}</p></div><Badge status={b.status} /><span>→</span></Link>) : <Empty title="Aún no hay conversaciones" text="El chat se habilita al confirmar una reserva con un profesional." to="/reservas" action="Ver reservas" />}<Pagination page={page} hasNext={bookings.data?.length === 20} onPage={setPage} /></div>;
}

export function Conversation() {
  const { id = '' } = useParams();
  const { user, api, demo } = useApp();
  const booking = useRemote<BookingDetail>(`/bookings/${id}`);
  const [page, setPage] = useState(1);
  const history = useRemote<Message[]>(`/bookings/${id}/messages?page=${page}`);
  const [incoming, setIncoming] = useState<Message[]>([]);
  const [connectionStatus, setConnectionStatus] = useState('connecting');
  const [draft, setDraft] = useState('');
  const action = useAction();
  const bottom = useRef<HTMLDivElement>(null);
  const historyReload = history.reload;
  useEffect(() => {
    const receive = (message: Message) => { if (message.bookingId === id && typeof message.text === 'string') setIncoming(items => [...items.filter(m => m.id !== message.id), message]); };
    if (demo) { const handler = (event: Event) => receive((event as CustomEvent<Message>).detail); window.addEventListener('cleanhome:demo-message', handler); return () => window.removeEventListener('cleanhome:demo-message', handler); }
    const connection = new HubConnectionBuilder().withUrl(HUB_URL, { accessTokenFactory: liveApi.getAccessToken }).withAutomaticReconnect().configureLogging(LogLevel.Error).build();
    let current = true;
    connection.on('MessageReceived', receive);
    connection.onreconnecting(() => { if (current) setConnectionStatus('reconnecting'); });
    connection.onreconnected(async () => { if (current) { setConnectionStatus('connected'); historyReload(); await connection.invoke('JoinBooking', id); } });
    connection.onclose(() => { if (current) setConnectionStatus('offline'); });
    void connection.start().then(async () => { if (current) { await connection.invoke('JoinBooking', id); setConnectionStatus('connected'); } }).catch(() => { if (current) setConnectionStatus('offline'); });
    const timer = window.setInterval(() => { if (connection.state !== HubConnectionState.Connected) historyReload(); }, 15000);
    return () => { current = false; window.clearInterval(timer); void connection.stop(); };
  }, [demo, id, user?.id, historyReload]);
  const messages = useMemo(() => {
    const items = new Map<string, Message>();
    for (const m of [...history.data ?? [], ...incoming]) if (m.bookingId === id) items.set(m.id, m);
    return [...items.values()].sort((a, b) => +new Date(a.createdAt) - +new Date(b.createdAt));
  }, [history.data, incoming, id]);
  useEffect(() => { if (page === 1) bottom.current?.scrollIntoView({ block: 'end', behavior: 'smooth' }); }, [messages.length, page]);
  const peer = user?.role === 'Worker' ? booking.data?.participants?.customerName || (demo ? 'Leonardo Sandoval' : 'Cliente de tu reserva') : booking.data?.participants?.workerName || (demo ? 'Sophia Carter' : 'Profesional asignado');
  const send = () => void action.run(async () => {
    if (!draft.trim()) return;
    const message = await api.request<Message>(`/bookings/${id}/messages`, jsonBody({ text: draft.trim() }));
    setIncoming(items => [...items.filter(m => m.id !== message.id), message]); setDraft('');
  });
  return <div className="conversation"><header className="chat-header"><Link to="/mensajes" className="back-button" aria-label="Volver a mensajes">←</Link><Avatar name={peer} src={demo ? '/design/85299d98-1a6d-4227-9937-eef82b53aac3.png' : undefined} /><div><h1>{peer}</h1><span className="verified-text">{demo ? 'Chat de demostración' : connectionStatus === 'connected' ? 'Chat disponible' : connectionStatus === 'connecting' ? 'Conectando…' : 'Actualizando por conexión segura'}</span></div></header><div className="chat-context"><Icon name="imgContainer3" /><span>#CH-{shortId(id)} · {booking.data ? date(booking.data.booking.startsAt) : 'Reserva'}</span><Link className="text-link" to={`/reservas/${id}`}>Ver ficha</Link></div><div className="message-history"><Info><p>Conversación privada entre participantes de la reserva. No compartas contraseñas ni datos bancarios.</p></Info><ErrorBox message={booking.error || history.error} retry={() => { booking.reload(); history.reload(); }} />{history.loading ? <Loading /> : !messages.length && <Empty title="Empieza la conversación" text="Aquí puedes coordinar los detalles de tu servicio." />}{messages.map(m => <div key={m.id} className={`message-row ${m.senderId === user?.id ? 'own' : ''}`}><div className="message-bubble">{m.text}</div><time>{date(m.createdAt, { hour: '2-digit', minute: '2-digit' })}{m.senderId === user?.id && <span aria-label="Enviado"> ✓</span>}</time></div>)}{(history.data?.length === 50 || page > 1) && <Pagination page={page} hasNext={history.data?.length === 50} onPage={setPage} />}<div ref={bottom} /></div><div className="compose-panel"><div className="quick-replies">{['¿A qué hora llegas?', 'El interfón es el 402', 'Insumos listos', 'Ya estoy en recepción', '¿Necesitas estacionamiento?'].map(text => <button key={text} type="button" onClick={() => setDraft(text)}>{text}</button>)}</div><ErrorBox message={action.error} />{booking.data?.booking.status === 'Cancelled' ? <Info><p>La reserva está cancelada. Puedes consultar el historial, pero no enviar nuevos mensajes.</p></Info> : <form onSubmit={e => { e.preventDefault(); send(); }}><label className="sr-only" htmlFor="chat-message">Escribe un mensaje</label><input id="chat-message" autoComplete="off" maxLength={2000} value={draft} onChange={e => setDraft(e.target.value)} placeholder="Escribe un mensaje seguro…" /><Button type="submit" busy={action.pending} disabled={!draft.trim() || !booking.data} aria-label="Enviar mensaje"><Icon screen="chat" name="imgContainer10" /></Button></form>}</div></div>;
}



