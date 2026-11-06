import { Link, NavLink, Navigate, Outlet, useLocation, useNavigate } from 'react-router-dom';
import { useApp, useRemote } from '../app/hooks';
import type { Notice } from '../services/types';
import { Brand, Icon } from './ui';

export function Shell() {
  const { user, demo, startDemo } = useApp();
  const location = useLocation();
  const navigate = useNavigate();
  const notices = useRemote<Notice[]>(user && !user.isAdmin ? '/notifications?page=1' : null);
  if (!user) return <Navigate to="/acceso" state={{ from: location.pathname + location.search }} replace />;
  const admin = user.isAdmin;
  const flow = location.pathname.startsWith('/solicitar') || location.pathname.endsWith('/pago');
  const links = admin ? [{ to: '/admin', name: 'Panel', icon: 'imgContainer18' }, { to: '/admin/perfiles', name: 'Perfiles', icon: 'imgContainer21' }, { to: '/admin/documentos', name: 'Documentos', icon: 'imgContainer19' }, { to: '/perfil', name: 'Cuenta', icon: 'imgContainer21' }] : [{ to: '/inicio', name: 'Inicio', icon: 'imgContainer18' }, { to: '/reservas', name: 'Reservas', icon: 'imgContainer19' }, { to: '/mensajes', name: 'Mensajes', icon: 'imgContainer20' }, { to: '/perfil', name: 'Perfil', icon: 'imgContainer21' }];
  return <div className={`app-shell ${admin ? 'admin-shell' : ''}`}>
    <aside className="desktop-sidebar"><Brand /><span className="sidebar-caption">{admin ? 'CONSOLA ADMINISTRATIVA' : user.role === 'Worker' ? 'MI ESPACIO PROFESIONAL' : 'TU HOGAR, EN BUENAS MANOS'}</span><nav>{links.map(l => <NavLink end={l.to === '/admin'} key={l.to} to={l.to}><Icon name={l.icon} />{l.name}</NavLink>)}{!admin && <><NavLink to={user.role === 'Worker' ? '/ganancias' : '/solicitudes'}><Icon name="imgContainer12" />{user.role === 'Worker' ? 'Pagos y ganancias' : 'Mis solicitudes'}</NavLink><NavLink to="/documentos"><Icon name="imgContainer14" />Mis documentos</NavLink></>}<NavLink to="/ayuda"><Icon name="imgContainer14" />Ayuda y privacidad</NavLink></nav><div className="sidebar-user"><span className="avatar avatar-small">{user.displayName.slice(0, 1)}</span><div><strong>{user.displayName}</strong><small>{demo ? 'Cuenta de demostración' : user.email}</small></div></div></aside>
    <div className="app-body"><header className="app-header">{flow ? <div className="row"><button className="back-button" onClick={() => navigate(-1)} aria-label="Regresar">←</button><strong>{location.pathname.endsWith('/pago') ? 'Contratación y pago' : 'Detalle del servicio'}</strong></div> : <Brand />}<div className="header-actions">{!flow && <Link className="icon-button" to={admin ? '/admin/auditoria' : '/notificaciones'} aria-label="Notificaciones"><Icon name="imgContainer16" />{notices.data?.some(n => !n.readAt) && <span className="notification-dot" />}</Link>}<Link className="profile-dot" to="/perfil" aria-label="Mi perfil"><Icon name="imgContainer17" /></Link></div></header>
      {demo && <div className="demo-ribbon"><span><i />Demostración · sin cobros</span><label className="sr-only" htmlFor="demo-role">Cambiar perspectiva</label><select id="demo-role" value={admin ? 'Admin' : user.role} onChange={e => { const role = e.target.value as 'Customer' | 'Worker' | 'Admin'; startDemo(role); navigate(role === 'Admin' ? '/admin' : '/inicio'); }}><option value="Customer">Cliente</option><option value="Worker">Trabajador</option><option value="Admin">Administración</option></select></div>}
      <main id="contenido" className={`screen ${location.pathname.startsWith('/mensajes/') ? 'chat-screen' : ''}`}><Outlet /></main>
      {!flow && !location.pathname.startsWith('/mensajes/') && <Link to="/ayuda" className="help-fab" aria-label="Ayuda CleanHome"><Icon screen="chat" name="imgContainer15" /></Link>}
      {!flow && <nav className="bottom-nav" aria-label="Navegación principal">{links.map(l => <NavLink key={l.to} end={l.to === '/admin'} to={l.to}><Icon name={l.icon} /><span>{l.name}</span></NavLink>)}</nav>}
    </div>
  </div>;
}






