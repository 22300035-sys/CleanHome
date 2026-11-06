export type Role = 'Customer' | 'Worker';
export type BookingStatus = 'Confirmed' | 'InProgress' | 'Completed' | 'Cancelled';
export type PaymentStatus = 'Pending' | 'Paid' | 'Refunded' | 'Disputed';
export interface Profile { id: string; userId: string; displayName: string; role: Role; isVerified: boolean; isSuspended: boolean; createdAt: string }
export interface Account extends Profile { email: string; isAdmin: boolean }
export interface Category { id: string; name: string; slug: string; description: string }
export interface Location { id: string; customerId: string; label: string; address: string; city: string }
export interface ServiceRequest { id: string; customerId?: string; locationId?: string; categoryId: string; description: string; city: string; startsAt: string; durationMinutes: number; status: 'Open' | 'Booked' | 'Cancelled' | 'Completed'; createdAt: string }
export interface Worker { id: string; displayName: string; isVerified: boolean; completedServices: number; avatar?: string; rating?: number }
export interface Quote { id: string; requestId: string; workerId: string; amount: number; currency: string; notes: string; expiresAt: string; createdAt: string }
export interface Booking { id: string; requestId: string; quoteId: string; customerId: string; workerId: string; startsAt: string; endsAt: string; amount: number; currency: string; status: BookingStatus; createdAt: string }
export interface BookingDetail { booking: Booking; location: Location; participants?: { customerName: string; workerName: string } }
export interface Payment { id: string; bookingId: string; provider: 'stripe' | 'mercadopago'; status: PaymentStatus; amount: number; currency: string; checkoutUrl?: string; checkoutId?: string; createdAt: string }
export interface DocumentItem { id: string; ownerId?: string; originalName: string; contentType: string; length: number; status: 'Pending' | 'Approved' | 'Rejected'; reviewNotes: string; createdAt: string }
export interface Notice { id: string; recipientId: string; kind: string; resourceId: string; createdAt: string; readAt?: string }
export interface Message { id: string; bookingId: string; senderId: string; text: string; createdAt: string }
export interface AuditItem { id: string; actorUserId: string; action: string; resourceId: string; createdAt: string }
export interface Page<T> { items: T[]; pageNumber: number; pageSize: number }
export interface Tokens { accessToken: string; refreshToken: string; expiresIn: number; tokenType: string }
export interface Transport { request<T>(path: string, init?: RequestInit): Promise<T>; getBlob(path: string): Promise<Blob> }
export const money = (value: number) => new Intl.NumberFormat('es-MX', { style: 'currency', currency: 'MXN', minimumFractionDigits: 2 }).format(value);
export const date = (value: string, options?: Intl.DateTimeFormatOptions) => new Intl.DateTimeFormat('es-MX', options ?? { day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' }).format(new Date(value));
export const title = (description: string) => description?.split('\n')[0] || 'Servicio de limpieza';
export const shortId = (id: string) => id?.replaceAll('-', '').slice(0, 6).toUpperCase();
export const statusLabel: Record<string, string> = { Open: 'Recibiendo cotizaciones', Booked: 'Reservada', Confirmed: 'Confirmada', InProgress: 'En progreso', Completed: 'Completada', Cancelled: 'Cancelada', Pending: 'Pendiente', Paid: 'Pagado', Refunded: 'Reembolsado', Disputed: 'En disputa', Approved: 'Aprobado', Rejected: 'Rechazado' };
