import { Link } from 'react-router-dom';
import { Brand } from '../components/ui';
import './Landing.css';

function HomeIllustration() {
  return <svg viewBox="0 0 600 480" role="img" aria-label="Ilustración de una sala luminosa y ordenada, con un sofá, plantas y una ventana">
    <rect x="50" y="30" width="500" height="385" rx="140" fill="#dcebe3" />
    <path d="M35 414H565" stroke="#668478" strokeWidth="2" />
    <rect x="285" y="73" width="175" height="207" rx="80" fill="#f6f5e9" />
    <circle cx="392" cy="133" r="32" fill="#e5b672" />
    <path d="M300 250Q350 172 445 247V275H300" fill="#b8d5c5" />
    <path d="M372 74V277M285 180H460" stroke="#fffdf5" strokeWidth="8" />
    <rect x="110" y="238" width="260" height="120" rx="25" fill="#527e69" />
    <rect x="99" y="286" width="284" height="97" rx="20" fill="#355e4c" />
    <path d="M128 384V414M353 384V414" stroke="#183b30" strokeWidth="9" />
    <rect x="143" y="261" width="82" height="66" rx="15" fill="#e8c7a1" transform="rotate(-8 143 261)" />
    <rect x="251" y="260" width="75" height="65" rx="15" fill="#d8e2bf" transform="rotate(8 251 260)" />
    <ellipse cx="307" cy="421" rx="180" ry="14" fill="#bdd0c0" />
    <ellipse cx="310" cy="367" rx="85" ry="20" fill="#dcb889" />
    <path d="M254 375L242 417M365 375L378 417" stroke="#a58056" strokeWidth="8" />
    <path d="M464 365V252M465 315Q400 307 424 267Q462 265 465 315M464 287Q518 273 503 239Q466 238 464 287M465 258Q428 229 447 201Q482 215 465 258" fill="#4a775d" stroke="#355c49" strokeWidth="3" />
    <path d="M434 356H495L485 413H445Z" fill="#c6916d" />
    <path d="M104 137V171M87 154H121M489 115V143M475 129H503" stroke="#688f76" strokeWidth="3" strokeLinecap="round" />
  </svg>;
}

export function Landing() {
  return <div className="landing">
    <header className="landing-nav"><Brand to="/" /><nav aria-label="Navegación principal"><a href="#como-funciona">Cómo funciona</a><a href="#para-ti">Para ti</a><Link className="landing-login" to="/acceso">Iniciar sesión <span aria-hidden="true">↗</span></Link></nav></header>
    <main id="contenido">
      <section className="landing-hero">
        <div className="landing-copy"><span className="landing-eyebrow"><span /> MENOS PENDIENTES. MÁS HOGAR.</span><h1>Un hogar limpio.<br /><em>Más tiempo para ti.</em></h1><p>Encuentra ayuda para la limpieza de tu espacio. Publica lo que necesitas, compara cotizaciones y coordina tu servicio desde un solo lugar.</p><div className="landing-actions"><Link className="landing-cta" to="/acceso">Quiero una limpieza <span aria-hidden="true">↗</span></Link><a className="landing-text-link" href="#como-funciona">Descubre cómo funciona <span aria-hidden="true">↓</span></a></div><div className="landing-note"><span aria-hidden="true">✓</span> Tú eliges la propuesta que mejor va contigo.</div></div>
        <div className="landing-art"><div className="landing-art-label">TU HOGAR, EN BUENAS MANOS <span aria-hidden="true">✦</span></div><HomeIllustration /><div className="landing-art-footer"><span>Un espacio para vivir mejor.</span><span aria-hidden="true">01 / HOGAR</span></div></div>
      </section>
      <div className="landing-strip"><span>Solicitudes a tu medida</span><i aria-hidden="true">✦</i><span>Cotizaciones en un lugar</span><i aria-hidden="true">✦</i><span>Chat dentro de tu reserva</span></div>
      <section className="landing-section" id="como-funciona"><div className="landing-section-heading"><div><span className="landing-eyebrow">ASÍ DE SENCILLO</span><h2>De pendiente a resuelto.</h2></div><p>Tres pasos para organizar tu próxima limpieza.</p></div><div className="landing-steps">{[
        ['01', 'Cuéntanos qué necesitas', 'Elige el tipo de limpieza, tu dirección y el horario que tienes en mente.'],
        ['02', 'Compara y elige', 'Revisa las propuestas de los profesionales y acepta la que se ajuste a tu espacio.'],
        ['03', 'Coordina tu servicio', 'Consulta tu reserva y conversa con la persona asignada desde CleanHome.'],
      ].map(([number, title, description]) => <article key={number}><span className="landing-step-number">{number}</span><h3>{title}</h3><p>{description}</p></article>)}</div></section>
      <section className="landing-audience" id="para-ti"><div><span className="landing-eyebrow">UN ESPACIO PARA CADA PERSONA</span><h2>¿Buscas ayuda<br />o nuevas oportunidades?</h2><p>CleanHome conecta a quienes necesitan una limpieza con quienes ofrecen su trabajo.</p></div><div className="landing-audience-links"><Link to="/acceso"><span><strong>Quiero contratar</strong><small>Publica una solicitud y recibe propuestas.</small></span><span aria-hidden="true">↗</span></Link><Link to="/acceso"><span><strong>Ofrezco servicios de limpieza</strong><small>Crea tu perfil y completa la verificación.</small></span><span aria-hidden="true">↗</span></Link></div></section>
      <section className="landing-faq"><div><span className="landing-eyebrow">ANTES DE EMPEZAR</span><h2>Todo más claro.</h2></div><div><details><summary>¿Cómo se define el precio?</summary><p>Los profesionales envían cotizaciones según tu solicitud. Puedes revisar el importe y los detalles antes de aceptar.</p></details><details><summary>¿Necesito una cuenta?</summary><p>Sí. Desde «Iniciar sesión» puedes crear una cuenta como cliente o trabajador y confirmar tu correo.</p></details><details><summary>¿Dónde coordino los detalles?</summary><p>Cuando tienes una reserva, puedes conversar con la otra persona desde el chat de CleanHome.</p></details></div></section>
      <section className="landing-last"><span className="landing-eyebrow">HAZ ESPACIO PARA LO QUE IMPORTA</span><h2>Tu próxima limpieza<br />empieza aquí.</h2><Link className="landing-cta" to="/acceso">Entrar a CleanHome <span aria-hidden="true">↗</span></Link></section>
    </main>
    <footer className="landing-footer"><Brand to="/" /><p>Tu hogar, en buenas manos.</p><Link to="/acceso">Iniciar sesión ↗</Link><small>© {new Date().getFullYear()} CleanHome</small></footer>
  </div>;
}
