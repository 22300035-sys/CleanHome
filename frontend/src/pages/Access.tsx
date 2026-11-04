import { useState } from 'react';
import type { FormEvent } from 'react';
import { Link, Navigate, useLocation } from 'react-router-dom';
import { useAction, useApp } from '../app/hooks';
import { Brand, Button, Card, ErrorBox, Icon, Info, PageTitle } from '../components/ui';
import { jsonBody } from '../services/api';
import type { Role } from '../services/types';

export function Access() {
  const location = useLocation();
  const { user, login, register, startDemo } = useApp();
  const action = useAction();
  const [registering, setRegistering] = useState(false);
  const [role, setRole] = useState<Role>('Customer');
  const [visible, setVisible] = useState(false);
  const [success, setSuccess] = useState('');
  const [factor, setFactor] = useState(false);
  const destination = typeof location.state?.from === 'string' && location.state.from.startsWith('/') && !location.state.from.startsWith('//') && location.state.from !== '/acceso' ? location.state.from : '/inicio';
  if (user) return <Navigate to={user.isAdmin ? '/admin' : destination} replace />;
  const submit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const fields = new FormData(event.currentTarget);
    const email = String(fields.get('email')).trim();
    const password = String(fields.get('password'));
    void action.run(async () => {
      if (registering) {
        const loggedIn = await register(email, password, role, String(fields.get('name')).trim());
        if (!loggedIn) { setRegistering(false); setSuccess('Cuenta creada. Revisa tu correo para confirmar la cuenta y después inicia sesión.'); }
      } else await login(email, password, role, undefined, factor ? String(fields.get('factor')) : undefined);
    });
  };
  return <main id="contenido" className="access-page">
    <header className="access-brand"><Brand large /><p>Servicios profesionales de limpieza para<br />tu hogar y oficina</p></header>
    <div className="segmented" aria-label="Acceso"><button className={!registering ? 'selected' : ''} onClick={() => { setRegistering(false); action.clear(); }}>Iniciar Sesión</button><button className={registering ? 'selected' : ''} onClick={() => { setRegistering(true); action.clear(); }}>Crear Cuenta</button></div>
    <section className="role-section"><h2>¿CÓMO DESEAS UTILIZAR CLEANHOME?</h2>
      <button className={`role-card ${role === 'Customer' ? 'selected' : ''}`} onClick={() => setRole('Customer')} aria-pressed={role === 'Customer'}><span className="role-icon"><Icon screen="login" name="imgContainer1" /></span><span><strong>Soy cliente</strong><small>Necesito contratar un servicio de limpieza certificado para mi hogar o negocio.</small></span>{role === 'Customer' && <span className="selection-check">✓</span>}</button>
      <button className={`role-card ${role === 'Worker' ? 'selected' : ''}`} onClick={() => setRole('Worker')} aria-pressed={role === 'Worker'}><span className="role-icon"><Icon screen="login" name="imgContainer3" /></span><span><strong>Soy trabajador</strong><small>Ofrezco servicios de limpieza profesional y deseo generar ingresos flexibles.</small></span>{role === 'Worker' && <span className="selection-check">✓</span>}</button>
    </section>
    <Card className="access-form"><div className="row between"><h2>{registering ? 'Crea tu cuenta' : 'Acceso a tu cuenta'}</h2><span className="badge">Perfil: {role === 'Customer' ? 'Cliente' : 'Trabajador'}</span></div>
      {success && <p className="success-box" role="status">{success}</p>}
      <form onSubmit={submit}>
        {registering && <label>Nombre completo<input name="name" required minLength={2} maxLength={120} autoComplete="name" placeholder="¿Cómo te llamas?" /></label>}
        <label>Correo electrónico<div className="input-icon"><Icon screen="login" name="imgIcon" /><input name="email" type="email" autoComplete="email" required maxLength={254} placeholder="ejemplo@correo.com" /></div></label>
        <div className="row between field-caption"><span>Contraseña</span>{!registering && <Link className="text-link" to="/recuperar">¿Olvidaste tu contraseña?</Link>}</div>
        <div className="input-icon password-field"><Icon screen="login" name="imgIcon1" /><input name="password" aria-label="Contraseña" type={visible ? 'text' : 'password'} autoComplete={registering ? 'new-password' : 'current-password'} minLength={registering ? 12 : 1} required maxLength={128} placeholder="••••••••" /><button type="button" onClick={() => setVisible(!visible)} aria-label={visible ? 'Ocultar contraseña' : 'Mostrar contraseña'}><Icon screen="login" name="imgContainer5" /></button></div>
        {registering && <p className="field-hint">Al menos 12 caracteres, con mayúscula, minúscula, número y símbolo.</p>}
        {!registering && <><button type="button" className="text-link factor-toggle" onClick={() => setFactor(!factor)}>Tengo un código de autenticación</button>{factor && <label>Código de verificación<input name="factor" autoComplete="one-time-code" inputMode="numeric" required pattern="[0-9]{6}" placeholder="000000" /></label>}</>}
        <ErrorBox message={action.error} /><Button type="submit" className="full" busy={action.pending}>{registering ? 'Crear cuenta' : 'Iniciar sesión'} <span aria-hidden="true">→</span></Button>
      </form>
      <p className="access-toggle">{registering ? '¿Ya tienes una cuenta?' : '¿No tienes una cuenta?'} <button className="text-link strong" onClick={() => setRegistering(!registering)}>{registering ? 'Inicia sesión' : 'Regístrate aquí'}</button></p>
    </Card>
    <div className="demo-entry"><Button variant="secondary" onClick={() => startDemo(role)}>Ver demostración</Button><p>Explora las pantallas con datos de ejemplo, sin crear cuenta ni realizar cobros.</p></div>
    <div className="trust-strip"><span><Icon screen="login" name="imgIcon2" />Identidad<br />verificada</span><span><Icon screen="login" name="imgIcon3" />Pago con<br />proveedor seguro</span><span><Icon screen="login" name="imgIcon4" />Atención<br />profesional</span></div>
  </main>;
}

export function Recovery() {
  const { api } = useApp();
  const action = useAction();
  const [sent, setSent] = useState(false);
  return <main id="contenido" className="access-page recovery-page"><Brand large /><PageTitle title="Recupera tu contraseña" subtitle="Te ayudamos a volver a tu cuenta." />
    <Card><h2>{sent ? 'Revisa tu correo' : '¿Olvidaste tu contraseña?'}</h2><p>{sent ? 'Si hay una cuenta asociada, recibirás un código de recuperación. Revisa también tu carpeta de correo no deseado.' : 'Escribe el correo con el que te registraste para recibir un código.'}</p>
      <form onSubmit={e => { e.preventDefault(); const email = new FormData(e.currentTarget).get('email'); void action.run(async () => { await api.request('/auth/forgotPassword', jsonBody({ email })); setSent(true); }); }}><label>Correo electrónico<input name="email" type="email" required autoComplete="email" placeholder="ejemplo@correo.com" /></label><ErrorBox message={action.error} /><Button type="submit" className="full" busy={action.pending}>{sent ? 'Reenviar código' : 'Enviar código de recuperación'}</Button></form>
      {sent && <Link to="/restablecer" className="button button-secondary full">Ya tengo mi código →</Link>}
    </Card><Link className="text-link centered" to="/acceso">← Volver a iniciar sesión</Link>
  </main>;
}

export function ResetPassword() {
  const { api } = useApp();
  const action = useAction();
  const [done, setDone] = useState(false);
  return <main id="contenido" className="access-page recovery-page"><Brand large /><PageTitle title="Elige una nueva contraseña" subtitle="Usa el código que recibiste por correo." /><Card>{done ? <Info><strong>Contraseña actualizada</strong><p>Ya puedes iniciar sesión con tu nueva contraseña.</p><Link to="/acceso" className="text-link">Iniciar sesión →</Link></Info> : <form onSubmit={e => { e.preventDefault(); const f = new FormData(e.currentTarget); void action.run(async () => { await api.request('/auth/resetPassword', jsonBody({ email: f.get('email'), resetCode: f.get('code'), newPassword: f.get('password') })); setDone(true); }); }}><label>Correo electrónico<input name="email" type="email" required autoComplete="email" /></label><label>Código de recuperación<input name="code" required autoComplete="one-time-code" /></label><label>Nueva contraseña<input name="password" type="password" required minLength={12} maxLength={128} autoComplete="new-password" /></label><p className="field-hint">12 caracteres o más con mayúscula, minúscula, número y símbolo.</p><ErrorBox message={action.error} /><Button type="submit" busy={action.pending} className="full">Guardar nueva contraseña</Button></form>}</Card><Link className="text-link centered" to="/acceso">Volver al acceso</Link></main>;
}



