import { ApiError } from './api';
import type { Account, AuditItem, Booking, Category, DocumentItem, Location, Message, Notice, Payment, Profile, Quote, Role, ServiceRequest, Transport, Worker } from './types';

const after = (days = 0, hours = 0) => new Date(Date.now() + days * 86400000 + hours * 3600000).toISOString();
export const DEMO_CUSTOMER = 'c1111111-1111-4111-8111-111111111111';
export const DEMO_WORKER = 'd1111111-1111-4111-8111-111111111111';
const categoryId = 'a1111111-1111-4111-8111-111111111111';
const deepId = 'a2222222-2222-4222-8222-222222222222';
export const demoAccounts: Record<string, Account> = {
  Customer: { id: DEMO_CUSTOMER, userId: 'demo-customer', displayName: 'Leonardo Sandoval', email: 'leonardo@ejemplo.test', role: 'Customer', isVerified: true, isSuspended: false, isAdmin: false, createdAt: after(-120) },
  Worker: { id: DEMO_WORKER, userId: 'demo-worker', displayName: 'Sophia Carter', email: 'sophia@ejemplo.test', role: 'Worker', isVerified: true, isSuspended: false, isAdmin: false, createdAt: after(-120) },
  Admin: { id: 'demo-admin', userId: 'demo-admin', displayName: 'Admin Operaciones', email: 'operaciones@ejemplo.test', role: 'Customer', isVerified: true, isSuspended: false, isAdmin: true, createdAt: after(-120) },
};
export function makeDemoStore() {
  const categories: Category[] = [
    { id: categoryId, name: 'Limpieza general', slug: 'limpieza-general', description: 'Mantenimiento integral y orden de tu hogar.' },
    { id: deepId, name: 'Limpieza profunda', slug: 'limpieza-profunda', description: 'Limpieza detallada de cocina, baños y áreas difíciles.' },
    { id: 'a3333333-3333-4333-8333-333333333333', name: 'Limpieza de oficinas', slug: 'limpieza-oficinas', description: 'Espacios de trabajo limpios y cómodos.' },
  ];
  const locations: Location[] = [
    { id: 'location-home', customerId: DEMO_CUSTOMER, label: 'Casa', address: 'Campos Elíseos 182, Depto 402. Torre Norte. Interfón 402.', city: 'Polanco V Sección, CDMX' },
    { id: 'location-office', customerId: DEMO_CUSTOMER, label: 'Oficina', address: 'Paseo de la Reforma 222, Piso 8. Acceso por recepción.', city: 'Juárez, Cuauhtémoc, CDMX' },
  ];
  const workers: Worker[] = [
    { id: DEMO_WORKER, displayName: 'Sophia Carter', isVerified: true, completedServices: 142, rating: 4.9, avatar: '/design/096e9aad-023f-483b-9c64-fd96119a4ed4.png' },
    { id: 'worker-mariana', displayName: 'Mariana Gómez', isVerified: true, completedServices: 89, rating: 4.8, avatar: '/design/6a1b34da-50ae-40cc-a177-042d69893bc3.png' },
    { id: 'worker-carlos', displayName: 'Carlos Valdés', isVerified: true, completedServices: 62, rating: 4.7, avatar: '/design/ed46ab3a-9e79-4c20-ac31-a6700c3314b4.png' },
  ];
  const requests: ServiceRequest[] = [
    { id: 'request-open', customerId: DEMO_CUSTOMER, locationId: 'location-home', categoryId, description: 'Limpieza general y ventanas\nDepartamento · 95 m² · 3 recámaras · 2 baños. Insumos incluidos.', city: 'Polanco V Sección, CDMX', startsAt: after(3), durationMinutes: 240, status: 'Open', createdAt: after(0, -2) },
    { id: 'request-next', customerId: DEMO_CUSTOMER, locationId: 'location-home', categoryId: deepId, description: 'Limpieza profunda\nDepartamento · 3 recámaras · 2 baños completos.', city: 'Polanco V Sección, CDMX', startsAt: after(1), durationMinutes: 240, status: 'Booked', createdAt: after(-1) },
    { id: 'request-active', customerId: DEMO_CUSTOMER, locationId: 'location-home', categoryId: deepId, description: 'Limpieza Profunda Integral\nIncluye sanitización de cocina y baños.', city: 'Polanco V Sección, CDMX', startsAt: after(0, -1), durationMinutes: 240, status: 'Booked', createdAt: after(-2) },
    { id: 'request-completed', customerId: DEMO_CUSTOMER, locationId: 'location-office', categoryId, description: 'Limpieza de Cristales y Fachada', city: 'Juárez, CDMX', startsAt: after(-1), durationMinutes: 180, status: 'Completed', createdAt: after(-3) },
    { id: 'request-radar', customerId: 'customer-other', locationId: 'location-home', categoryId, description: 'Limpieza general semanal\n2 recámaras · 1 baño. Insumos proporcionados por el cliente.', city: 'Anzures, CDMX', startsAt: after(2), durationMinutes: 180, status: 'Open', createdAt: after(0, -1) },
  ];
  const quotes: Quote[] = workers.map((w, i) => ({ id: 'quote-' + i, requestId: 'request-open', workerId: w.id, amount: [520, 480, 450][i], currency: 'MXN', notes: ['Hola, cuento con 6 años de experiencia y llevo productos ecológicos. Con gusto te ayudo con tu hogar.', 'Puntual y detallista en áreas de cocina y baños. Lista para comenzar en el horario pactado.', 'Llevo equipo básico y dedico especial atención a cristales y superficies.'][i], expiresAt: after(1), createdAt: after(0, -1) }));
  const bookings: Booking[] = [
    { id: 'booking-next', requestId: 'request-next', quoteId: 'quote-next', customerId: DEMO_CUSTOMER, workerId: DEMO_WORKER, startsAt: after(1), endsAt: after(1, 4), amount: 520, currency: 'MXN', status: 'Confirmed', createdAt: after(-1) },
    { id: 'booking-active', requestId: 'request-active', quoteId: 'quote-active', customerId: DEMO_CUSTOMER, workerId: DEMO_WORKER, startsAt: after(0, -1), endsAt: after(0, 3), amount: 550, currency: 'MXN', status: 'InProgress', createdAt: after(-2) },
    { id: 'booking-done', requestId: 'request-completed', quoteId: 'quote-done', customerId: DEMO_CUSTOMER, workerId: 'worker-carlos', startsAt: after(-1), endsAt: after(-1, 3), amount: 420, currency: 'MXN', status: 'Completed', createdAt: after(-3) },
  ];
  const payments: Payment[] = bookings.filter(b => b.status !== 'Confirmed').map((b, i) => ({ id: 'payment-' + i, bookingId: b.id, amount: b.amount, currency: 'MXN', provider: i ? 'mercadopago' : 'stripe', status: 'Paid', createdAt: b.createdAt }));
  const documents: DocumentItem[] = [{ id: 'document-demo', ownerId: DEMO_WORKER, originalName: 'Identificacion.pdf', contentType: 'application/pdf', length: 45800, status: 'Approved', reviewNotes: 'Identidad revisada.', createdAt: after(-10) }];
  const notices: Notice[] = [
    { id: 'notice-quote', recipientId: DEMO_CUSTOMER, kind: 'quote.received', resourceId: 'quote-0', createdAt: after(0, -0.25) },
    { id: 'notice-chat', recipientId: DEMO_CUSTOMER, kind: 'chat.message', resourceId: 'booking-active', createdAt: after(0, -0.5) },
    { id: 'notice-paid', recipientId: DEMO_CUSTOMER, kind: 'payment.Paid', resourceId: 'payment-0', createdAt: after(-1) },
    { id: 'notice-done', recipientId: DEMO_CUSTOMER, kind: 'booking.Completed', resourceId: 'booking-done', createdAt: after(-1, -1), readAt: after(-1) },
  ];
  const messages: Message[] = [
    { id: 'message-1', bookingId: 'booking-active', senderId: DEMO_CUSTOMER, text: 'Hola, buenos días. ¿Traerás aspiradora propia o utilizas la de casa?', createdAt: after(0, -1) },
    { id: 'message-2', bookingId: 'booking-active', senderId: DEMO_WORKER, text: 'Hola, buen día. Llevo mi equipo de microfibras y desengrasante especializado. Si cuentas con aspiradora puedo utilizar la tuya sin problema.', createdAt: after(0, -0.9) },
    { id: 'message-3', bookingId: 'booking-active', senderId: DEMO_CUSTOMER, text: 'Perfecto, te la dejo lista en la sala principal. También dejé las llaves en recepción.', createdAt: after(0, -0.8) },
  ];
  const profiles: Profile[] = Object.values(demoAccounts).filter(a => !a.isAdmin);
  const audit: AuditItem[] = [{ id: 'audit-demo', actorUserId: 'demo-admin', action: 'worker.verify', resourceId: DEMO_WORKER, createdAt: after(-10) }];
  return { categories, locations, workers, requests, quotes, bookings, payments, documents, notices, messages, profiles, audit };
}
export type DemoStore = ReturnType<typeof makeDemoStore>;

export class DemoApi implements Transport {
  private store: DemoStore;
  private account: Account;
  constructor(store: DemoStore, account: Account) { this.store = store; this.account = account; }
  async getBlob() { return new Blob(['Documento de demostración; no contiene datos personales.'], { type: 'text/plain' }); }
  async request<T>(path: string, init: RequestInit = {}): Promise<T> {
    const url = new URL('https://demo.invalid' + path);
    const parts = url.pathname.split('/').filter(Boolean);
    const method = init.method ?? 'GET';
    const body = typeof init.body === 'string' ? JSON.parse(init.body) : {};
    const s = this.store;
    const actor = this.account;
    let result: unknown = undefined;
    const fail = (status: number, message: string): never => { throw new ApiError(status, message); };
    if (!actor.isAdmin && s.profiles.find(p => p.id === actor.id)?.isSuspended) fail(403, 'Esta cuenta de demostración está suspendida.');
    const participant = (id: string) => s.bookings.find(b => b.id === id && (b.customerId === actor.id || b.workerId === actor.id)) ?? fail(404, 'Reserva no encontrada.');
    if (parts[0] === 'service-categories') result = s.categories;
    else if (parts[0] === 'profiles') {
      if (method === 'PUT') { actor.displayName = body.displayName; const p = s.profiles.find(p => p.id === actor.id); if (p) p.displayName = body.displayName; }
      result = actor;
    } else if (parts[0] === 'workers') result = s.workers.find(w => w.id === parts[1] && w.isVerified) ?? fail(404, 'Perfil no encontrado.');
    else if (parts[0] === 'locations') {
      if (parts[1] && (method === 'PUT' || method === 'DELETE')) {
        const location = s.locations.find(l => l.id === parts[1] && l.customerId === actor.id) ?? fail(404, 'Dirección no encontrada.');
        if (s.requests.some(r => r.locationId === location.id)) fail(409, 'Esta dirección forma parte del historial de una solicitud. Guarda una dirección nueva para próximos servicios.');
        if (method === 'PUT') {
          if (![body.label, body.address, body.city].every(value => typeof value === 'string' && value.trim())) fail(400, 'Dirección incompleta.');
          location.label = body.label.trim(); location.address = body.address.trim(); location.city = body.city.trim();
          result = location;
        }
        else s.locations = s.locations.filter(l => l.id !== location.id);
      }
      else if (method === 'POST') { const location = { ...body, id: crypto.randomUUID(), customerId: actor.id }; s.locations.push(location); result = location; }
      else result = s.locations.filter(l => l.customerId === actor.id);
    } else if (parts[0] === 'requests') {
      if (parts[2] === 'quotes') {
        if (method === 'POST') { if (s.quotes.some(q => q.requestId === parts[1] && q.workerId === actor.id)) fail(409, 'Ya enviaste una cotización para esta solicitud.'); const quote = { ...body, id: crypto.randomUUID(), requestId: parts[1], workerId: actor.id, currency: 'MXN', createdAt: after() }; s.quotes.push(quote); result = quote; }
        else result = s.quotes.filter(q => q.requestId === parts[1] && (actor.role === 'Customer' || q.workerId === actor.id));
      } else if (parts[2] === 'cancel') { const r = s.requests.find(r => r.id === parts[1]) ?? fail(404, 'Solicitud no encontrada.'); if (r.customerId !== actor.id) fail(404, 'Solicitud no encontrada.'); if (r.status !== 'Open') fail(409, 'La solicitud ya no está abierta.'); r.status = 'Cancelled'; }
      else if (parts[1]) result = s.requests.find(r => r.id === parts[1]) ?? fail(404, 'Solicitud no encontrada.');
      else if (method === 'POST') { const r = { ...body, id: crypto.randomUUID(), customerId: actor.id, city: s.locations.find(l => l.id === body.locationId)?.city ?? '', status: 'Open', createdAt: after() }; s.requests.unshift(r); result = r; }
      else { const page = Number(url.searchParams.get('page') ?? 1); const items = s.requests.filter(r => actor.role === 'Customer' ? r.customerId === actor.id : r.status === 'Open'); result = { items: items.slice((page - 1) * 20, page * 20), pageNumber: page, pageSize: 20 }; }
    } else if (parts[0] === 'quotes' && parts[2] === 'accept') {
      const q = s.quotes.find(q => q.id === parts[1]) ?? fail(404, 'Cotización no encontrada.');
      const r = s.requests.find(r => r.id === q.requestId) ?? fail(404, 'Solicitud no encontrada.');
      if (r.customerId !== actor.id || r.status !== 'Open') fail(409, 'La solicitud ya no admite reservas.');
      const b: Booking = { id: crypto.randomUUID(), requestId: r.id, quoteId: q.id, customerId: actor.id, workerId: q.workerId, startsAt: r.startsAt, endsAt: new Date(new Date(r.startsAt).getTime() + r.durationMinutes * 60000).toISOString(), amount: q.amount, currency: 'MXN', status: 'Confirmed', createdAt: after() };
      r.status = 'Booked'; s.bookings.unshift(b); result = b;
    } else if (parts[0] === 'bookings') {
      if (!parts[1]) result = s.bookings.filter(b => b.customerId === actor.id || b.workerId === actor.id);
      else {
        const b = participant(parts[1]);
        if (parts[2] === 'payment') result = s.payments.find(p => p.bookingId === b.id) ?? null;
        else if (parts[2] === 'checkout') { let p = s.payments.find(p => p.bookingId === b.id); if (!p) { p = { id: crypto.randomUUID(), bookingId: b.id, provider: body.provider, amount: b.amount, currency: 'MXN', status: 'Pending', createdAt: after() }; s.payments.push(p); } else if (p.provider !== body.provider) fail(409, 'La reserva ya tiene otro proveedor de pago.'); result = p; }
        else if (parts[2] === 'status') { const paid = s.payments.find(p => p.bookingId === b.id)?.status === 'Paid'; if (body.status === 'InProgress' && (!paid || actor.role !== 'Worker' || b.status !== 'Confirmed')) fail(409, 'Se requiere pago confirmado para iniciar.'); if (body.status === 'Cancelled' && paid) fail(409, 'Solicita primero un reembolso al administrador.'); if (body.status === 'Completed' && (actor.role !== 'Customer' || b.status !== 'InProgress')) fail(409, 'Solo el cliente puede confirmar el servicio en curso.'); b.status = body.status; result = b; }
        else if (parts[2] === 'messages') { if (method === 'POST') { const message = { id: crypto.randomUUID(), bookingId: b.id, senderId: actor.id, text: body.text, createdAt: after() }; s.messages.push(message); result = message; window.dispatchEvent(new CustomEvent('cleanhome:demo-message', { detail: message })); } else result = s.messages.filter(m => m.bookingId === b.id).toReversed(); }
        else { const r = s.requests.find(r => r.id === b.requestId); result = { booking: b, location: s.locations.find(l => l.id === r?.locationId) ?? s.locations[0], participants: { customerName: 'Leonardo Sandoval', workerName: s.workers.find(w => w.id === b.workerId)?.displayName || 'Profesional' } }; }
      }
    } else if (parts[0] === 'documents') {
      if (method === 'POST' && init.body instanceof FormData) { const file = init.body.get('file') as File; const d: DocumentItem = { id: crypto.randomUUID(), ownerId: actor.id, originalName: file.name, contentType: file.type, length: file.size, status: 'Pending', reviewNotes: '', createdAt: after() }; s.documents.push(d); result = d; }
      else if (method === 'DELETE') { s.documents = s.documents.filter(d => d.id !== parts[1] || d.ownerId !== actor.id); actor.isVerified = false; const profile = s.profiles.find(p => p.id === actor.id); if (profile) profile.isVerified = false; }
      else result = s.documents.filter(d => d.ownerId === actor.id);
    } else if (parts[0] === 'notifications') {
      if (parts[2] === 'read') { const n = s.notices.find(n => n.id === parts[1] && n.recipientId === actor.id); if (n) n.readAt = after(); }
      else result = s.notices.filter(n => n.recipientId === actor.id);
    } else if (parts[0] === 'admin') {
      if (!actor.isAdmin) fail(403, 'Acceso de administración requerido.');
      if (parts[0] === 'admin' && method === 'GET') result = ({ profiles: s.profiles, documents: s.documents, payments: s.payments, audit: s.audit, bookings: s.bookings, requests: s.requests } as Record<string, unknown>)[parts[1]] ?? [];
      else if (parts[1] === 'profiles') { const profile = s.profiles.find(p => p.id === parts[2]); if (profile) { if (parts[3] === 'verify') { if (!s.documents.some(d => d.ownerId === profile.id && d.status === 'Approved')) fail(409, 'Se requiere al menos un documento aprobado.'); profile.isVerified = true; const worker = s.workers.find(w => w.id === profile.id); if (worker) worker.isVerified = true; } if (parts[3] === 'suspend') profile.isSuspended = url.searchParams.get('suspended') === 'true'; } }
      else if (parts[1] === 'documents') { const doc = s.documents.find(d => d.id === parts[2]); if (doc) { doc.status = body.status; doc.reviewNotes = body.notes; if (body.status === 'Rejected') { const profile = s.profiles.find(p => p.id === doc.ownerId); if (profile) profile.isVerified = false; const worker = s.workers.find(w => w.id === doc.ownerId); if (worker) worker.isVerified = false; } } }
      else if (parts[1] === 'payments') { const p = s.payments.find(p => p.id === parts[2]); if (p) p.status = 'Refunded'; }
    } else if (parts[0] === 'demo' && parts[1] === 'payments') { const p = s.payments.find(p => p.bookingId === parts[2]); if (p) p.status = 'Paid'; }
    else if (parts[0] !== 'auth') fail(404, 'Esta vista no está disponible en la demostración.');
    return structuredClone(result) as T;
  }
}
export const demoAccount = (role: Role | 'Admin') => structuredClone(demoAccounts[role]);


