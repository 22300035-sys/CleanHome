import { useEffect, useRef } from 'react';
import type { ReactNode, ButtonHTMLAttributes } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { designAssets } from '../design/assets';
import { statusLabel } from '../services/types';

type Asset = { src: string; width?: number; height?: number };
const allAssets = designAssets as Record<string, Record<string, Asset>>;
export function Icon({ screen = 'home', name = 'imgContainer18', className = '' }: { screen?: string; name?: string; className?: string }) {
  const asset = allAssets[screen]?.[name];
  return asset ? <span className={`icon ${className}`} aria-hidden="true"><img src={asset.src} width={asset.width} height={asset.height} alt="" draggable={false} /></span> : null;
}
export function Brand({ large = false, to = '/inicio' }: { large?: boolean; to?: string }) {
  return <Link to={to} className={`brand ${large ? 'brand-large' : ''}`} aria-label="CleanHome, inicio"><span className="brand-mark"><Icon screen={large ? 'login' : 'home'} name={large ? 'imgContainer' : 'imgContainer15'} /></span><span className="brand-name">Clean<span>Home</span><i /></span></Link>;
}
export function Button({ children, variant = 'primary', className = '', busy, ...props }: ButtonHTMLAttributes<HTMLButtonElement> & { variant?: 'primary' | 'secondary' | 'ghost' | 'danger'; busy?: boolean }) {
  return <button type="button" {...props} disabled={props.disabled || busy} aria-busy={busy || undefined} className={`button button-${variant} ${className}`}>{busy ? <span className="spinner" aria-hidden="true" /> : null}{children}</button>;
}
export function Card({ children, className = '' }: { children: ReactNode; className?: string }) { return <section className={`card ${className}`}>{children}</section>; }
export function Badge({ children, status, tone }: { children?: ReactNode; status?: string; tone?: string }) { return <span className={`badge tone-${tone || (status === 'Paid' || status === 'Completed' || status === 'Approved' ? 'green' : status === 'Cancelled' || status === 'Rejected' || status === 'Disputed' ? 'red' : 'blue')}`}>{children || statusLabel[status || ''] || status}</span>; }
export function PageTitle({ title, subtitle, back = false, children }: { title: string; subtitle?: string; back?: boolean; children?: ReactNode }) {
  const navigate = useNavigate();
  return <header className="page-heading">{back && <button type="button" onClick={() => navigate(-1)} className="back-button" aria-label="Regresar">←</button>}<div><h1>{title}</h1>{subtitle && <p>{subtitle}</p>}</div>{children}</header>;
}
export function SectionTitle({ children, action, to }: { children: ReactNode; action?: string; to?: string }) { return <div className="section-heading"><h2>{children}</h2>{action && to && <Link to={to} className="text-link">{action}</Link>}</div>; }
export function Info({ children, screen = 'home', icon = 'imgContainer14' }: { children: ReactNode; screen?: string; icon?: string }) { return <aside className="info"><span className="info-icon"><Icon screen={screen} name={icon} /></span><div>{children}</div></aside>; }
export function ErrorBox({ message, retry }: { message?: string; retry?: () => void }) { return message ? <div className="error-box" role="alert"><strong>No pudimos completar la acción</strong><p>{message}</p>{retry && <Button variant="secondary" onClick={retry}>Reintentar</Button>}</div> : null; }
export function Loading() { return <div className="loading" role="status"><span className="spinner" /> Cargando…</div>; }
export function Empty({ title, text, action, to }: { title: string; text: string; action?: string; to?: string }) { return <Card className="empty"><span className="empty-symbol"><Icon name="imgContainer19" /></span><h2>{title}</h2><p>{text}</p>{action && to && <Link className="button button-primary" to={to}>{action}</Link>}</Card>; }
export function Avatar({ name = '', src, size = 'medium' }: { name?: string; src?: string; size?: 'small' | 'medium' | 'large' }) { return <span className={`avatar avatar-${size}`}>{src ? <img src={src} alt={name} /> : <span aria-label={name}>{name.split(' ').map(n => n[0]).slice(0, 2).join('').toUpperCase() || 'CH'}</span>}</span>; }
export function Pagination({ page, hasNext, onPage }: { page: number; hasNext: boolean; onPage: (page: number) => void }) { return <nav className="pagination" aria-label="Paginación"><Button variant="ghost" disabled={page <= 1} onClick={() => onPage(page - 1)}>← Anterior</Button><span>Página {page}</span><Button variant="ghost" disabled={!hasNext} onClick={() => onPage(page + 1)}>Siguiente →</Button></nav>; }
export function ConfirmDialog({ title, children, confirm, pending, onConfirm, onClose, error }: { title: string; children: ReactNode; confirm: string; pending?: boolean; onConfirm: () => void; onClose: () => void; error?: string }) {
  const ref = useRef<HTMLDialogElement>(null);
  useEffect(() => { const dialog = ref.current; dialog?.showModal(); return () => dialog?.close(); }, []);
  return <dialog ref={ref} className="confirm-dialog" aria-labelledby="confirm-title" onCancel={e => { e.preventDefault(); if (!pending) onClose(); }}><h2 id="confirm-title">{title}</h2><div>{children}</div><ErrorBox message={error} /><div className="dialog-actions"><Button variant="secondary" onClick={onClose} disabled={pending} autoFocus>Volver</Button><Button onClick={onConfirm} busy={pending}>{confirm}</Button></div></dialog>;
}
export function Modal({ title, children, onClose }: { title: string; children: ReactNode; onClose: () => void }) {
  const ref = useRef<HTMLDialogElement>(null);
  useEffect(() => { const dialog = ref.current; dialog?.showModal(); return () => dialog?.close(); }, []);
  return <dialog ref={ref} className="confirm-dialog" aria-labelledby="modal-title" onCancel={e => { e.preventDefault(); onClose(); }}><h2 id="modal-title">{title}</h2>{children}</dialog>;
}

