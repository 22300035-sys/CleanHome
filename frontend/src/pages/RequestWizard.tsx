import { useState } from 'react';
import { Link, Navigate, useNavigate, useParams, useSearchParams } from 'react-router-dom';
import { useAction, useApp, useRemote } from '../app/hooks';
import { Button, Card, ConfirmDialog, Empty, ErrorBox, Icon, Info, Loading, PageTitle } from '../components/ui';
import { jsonBody } from '../services/api';
import type { Category, Location, ServiceRequest } from '../services/types';
import { date } from '../services/types';

const serviceChoices = [
  { id: 'general', label: 'Limpieza general', description: 'Mantenimiento integral y orden regular de áreas comunes, salas y dormitorios.', icon: 'imgContainer2' },
  { id: 'profunda', label: 'Limpieza profunda', description: 'Desinfección a fondo y atención a sarro y grasa en áreas difíciles.', icon: 'imgContainer3' },
  { id: 'cocina', label: 'Limpieza de cocina', description: 'Hornos, campanas, gabinetes y desengrase profundo.', icon: 'imgContainer5' },
  { id: 'baños', label: 'Limpieza de baños', description: 'Azulejos, sanitarios, canceles y desinfección total.', icon: 'imgContainer6' },
  { id: 'ventanas', label: 'Limpieza de ventanas', description: 'Cristales accesibles, rieles y marcos en detalle.', icon: 'imgContainer7' },
  { id: 'pisos', label: 'Limpieza de pisos y alfombras', description: 'Aspirado y atención a las superficies de tu hogar.', icon: 'imgContainer8' },
];
function localDay(days = 0) { const d = new Date(); d.setDate(d.getDate() + days); return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`; }

export function LocationForm({ onCreated, onCancel }: { onCreated: (location: Location) => void; onCancel?: () => void }) {
  const { api, toast } = useApp();
  const action = useAction();
  return <form className="location-form" onSubmit={e => { e.preventDefault(); const f = new FormData(e.currentTarget); void action.run(async () => { const location = await api.request<Location>('/locations', jsonBody({ label: String(f.get('label')).trim(), address: String(f.get('address')).trim(), city: String(f.get('city')).trim() })); toast('Dirección guardada.'); onCreated(location); }); }}>
    <label>Nombre de la dirección<input name="label" required maxLength={80} placeholder="Casa, oficina…" autoComplete="off" /></label><label>Ciudad y zona<input name="city" required maxLength={120} placeholder="Polanco, Ciudad de México" autoComplete="address-level2" /></label><label>Dirección completa<textarea name="address" required maxLength={500} rows={3} placeholder="Calle, número, colonia, código postal e interior" autoComplete="street-address" /></label><ErrorBox message={action.error} /><div className="row">{onCancel && <Button variant="ghost" onClick={onCancel}>Cancelar</Button>}<Button type="submit" busy={action.pending} className="grow">Guardar dirección</Button></div>
  </form>;
}

export function RequestWizard() {
  const { step: stepParam } = useParams();
  const step = Number(stepParam || 1);
  const [search] = useSearchParams();
  const { user, api, toast, demo } = useApp();
  const navigate = useNavigate();
  const categories = useRemote<Category[]>('/service-categories');
  const locations = useRemote<Location[]>('/locations');
  const action = useAction();
  const [addLocation, setAddLocation] = useState(false);
  const [review, setReview] = useState(false);
  const [draft, setDraft] = useState({ services: [search.get('servicio') || 'general'], property: 'Departamento', bedrooms: 3, bathrooms: 2, area: 95, pets: false, supplies: true, notes: '', duration: 240, day: localDay(1), time: '10:00', locationId: '', interior: '', access: '' });
  const selectedLocation = locations.data?.find(l => l.id === draft.locationId) ?? (draft.locationId ? undefined : locations.data?.[0]);
  const patch = (changes: Partial<typeof draft>) => setDraft(d => ({ ...d, ...changes }));
  if (user?.role !== 'Customer' || user.isAdmin) return <Navigate to="/inicio" replace />;
  if (![1, 2, 3].includes(step)) return <Navigate to="/solicitar/1" replace />;
  const summary = draft.services.map(id => serviceChoices.find(s => s.id === id)?.label || id).join(' + ');
  const publish = () => void action.run(async () => {
    if (!selectedLocation) throw new Error('Agrega o selecciona una dirección.');
    if (!draft.services.length) throw new Error('Selecciona al menos un servicio.');
    const startsAt = new Date(`${draft.day}T${draft.time}:00`).toISOString();
    if (+new Date(startsAt) < Date.now() + 1800000) throw new Error('El servicio debe comenzar al menos dentro de 30 minutos.');
    const category = categories.data?.find(c => c.slug === (draft.services.includes('profunda') ? 'limpieza-profunda' : 'limpieza-general'));
    if (!category) throw new Error('No se encontró una categoría disponible. Actualiza el catálogo.');
    let locationId = selectedLocation.id;
    if (draft.interior.trim() || draft.access.trim()) {
      const address = `${selectedLocation.address}${draft.interior.trim() ? '\nInterior: ' + draft.interior.trim() : ''}${draft.access.trim() ? '\nAcceso: ' + draft.access.trim() : ''}`;
      if (address.length > 500) throw new Error('La dirección con sus indicaciones no puede superar 500 caracteres.');
      const privateLocation = await api.request<Location>('/locations', jsonBody({ label: `${selectedLocation.label.slice(0, 45)} · ${draft.day}`, address, city: selectedLocation.city }));
      locationId = privateLocation.id;
      patch({ locationId, interior: '', access: '' }); locations.reload();
    }
    const description = `${summary}\n${draft.property} · ${draft.area} m² · ${draft.bedrooms} recámaras · ${draft.bathrooms} baños.\n${draft.pets ? 'Hay mascotas en el inmueble.' : 'Sin mascotas.'} ${draft.supplies ? 'Solicito insumos del profesional.' : 'El cliente proporciona insumos.'}\n${draft.notes.trim()}`;
    const request = await api.request<ServiceRequest>('/requests', jsonBody({ locationId, categoryId: category.id, description, startsAt, durationMinutes: draft.duration }));
    toast('Solicitud publicada. Aquí aparecerán las cotizaciones que recibas.'); navigate(`/solicitudes/${request.id}/cotizaciones`, { replace: true });
  });
  return <div className="page-stack wizard-page"><div className="wizard-progress"><div className="row between"><span>PASO {step} DE 3 · {['TIPO DE SERVICIO', 'DATOS DEL INMUEBLE', 'FECHA Y UBICACIÓN'][step - 1]}</span><strong>{step === 3 ? '100%' : step === 2 ? '66%' : '33%'}</strong></div><progress max={3} value={step} aria-label={`Paso ${step} de 3`} /></div>
    {step === 1 && <><PageTitle title="¿Qué necesitas limpiar?" subtitle="Selecciona uno o varios servicios para tu solicitud personalizada." /><ErrorBox message={categories.error} retry={categories.reload} /><div className="service-options">{serviceChoices.map(s => <button className={`service-option ${draft.services.includes(s.id) ? 'selected' : ''}`} key={s.id} onClick={() => patch({ services: draft.services.includes(s.id) ? draft.services.filter(x => x !== s.id) : [...draft.services, s.id] })} aria-pressed={draft.services.includes(s.id)}><span className="checkbox-mark">{draft.services.includes(s.id) && <Icon screen="request1" name="imgContainer1" />}</span><span><strong>{s.label}</strong><small>{s.description}</small></span><Icon screen="request1" name={s.icon} /></button>)}</div><Info><p>El precio final se determina mediante las cotizaciones de los profesionales, según los detalles de tu espacio.</p></Info><div className="wizard-footer"><div className="row between small"><span>Servicios seleccionados:</span><strong>{draft.services.length} seleccionados</strong></div><Button className="full" disabled={!draft.services.length || !categories.data} onClick={() => navigate('/solicitar/2')}>Continuar al Paso 2 →</Button></div></>}
    {step === 2 && <><PageTitle title="Cuéntanos sobre el lugar" subtitle="Ayuda a los profesionales a calcular el tiempo y los recursos necesarios." /><form onSubmit={e => { e.preventDefault(); navigate('/solicitar/3'); }}><label>Tipo de propiedad</label><div className="property-grid">{['Departamento', 'Casa', 'Oficina', 'Local'].map(p => <button type="button" key={p} className={draft.property === p ? 'selected' : ''} onClick={() => patch({ property: p })}>{p}</button>)}</div><Card>{([{ key: 'bedrooms' as const, label: 'Número de recámaras', hint: 'Espacios para descansar', min: 0 }, { key: 'bathrooms' as const, label: 'Número de baños', hint: 'Completos y medios baños', min: 1 }]).map(f => <div className="counter-row" key={f.key}><div><strong>{f.label}</strong><p>{f.hint}</p></div><div className="counter"><button type="button" onClick={() => patch({ [f.key]: Math.max(f.min, draft[f.key] - 1) })} aria-label={`Reducir ${f.label.toLowerCase()}`}>−</button><output>{draft[f.key]}</output><button type="button" onClick={() => patch({ [f.key]: Math.min(20, draft[f.key] + 1) })} aria-label={`Aumentar ${f.label.toLowerCase()}`}>+</button></div></div>)}<label>Metraje aproximado (m²)<input type="number" required min={1} max={10000} value={draft.area} onChange={e => patch({ area: Number(e.target.value) })} /></label><label>Duración estimada<select value={draft.duration} onChange={e => patch({ duration: Number(e.target.value) })}>{[60, 120, 180, 240, 360, 480].map(m => <option key={m} value={m}>{m / 60} horas</option>)}</select></label></Card><Card>{([{ key: 'pets' as const, label: '¿Hay mascotas en el inmueble?', hint: 'Gatos, perros u otros animales en el hogar.' }, { key: 'supplies' as const, label: 'Insumos y productos incluidos', hint: 'Solicitar que el profesional lleve sus materiales.' }]).map(f => <div key={f.key} className="toggle-row"><div><strong>{f.label}</strong><p>{f.hint}</p></div><button type="button" className={`switch ${draft[f.key] ? 'on' : ''}`} role="switch" aria-label={f.label} aria-checked={draft[f.key]} onClick={() => patch({ [f.key]: !draft[f.key] })}><span /></button></div>)}</Card><label>Instrucciones especiales o áreas prioritarias<textarea maxLength={250} value={draft.notes} onChange={e => patch({ notes: e.target.value })} rows={4} placeholder="Por ejemplo, atender la campana de cocina y los canceles del baño." /><span className="field-hint">{draft.notes.length} / 250 · No incluyas dirección ni códigos de acceso.</span></label>{demo && <Info><p>Las fotografías opcionales del diseño no se adjuntan a solicitudes. Describe aquí las áreas que necesitan más atención.</p></Info>}<Button type="submit" className="full">Continuar al Paso 3 →</Button></form></>}
    {step === 3 && <><PageTitle title="¿Cuándo y dónde necesitas el servicio?" subtitle="Finaliza estos detalles para conectar con el profesional adecuado." /><form onSubmit={e => { e.preventDefault(); setReview(true); }}><label>Fecha de atención</label><div className="date-options">{[1, 2, 3].map((day, i) => <button type="button" key={day} className={draft.day === localDay(day) ? 'selected' : ''} onClick={() => patch({ day: localDay(day) })}><small>{i === 0 ? 'MÁS RÁPIDO' : 'PRÓXIMAMENTE'}</small><strong>{i === 0 ? 'Mañana' : new Date(`${localDay(day)}T12:00:00`).toLocaleDateString('es-MX', { weekday: 'long' })}</strong><span>{date(`${localDay(day)}T12:00:00`, { day: 'numeric', month: 'long' })}</span></button>)}</div><label>Elegir otra fecha<input type="date" required min={localDay()} max={localDay(180)} value={draft.day} onChange={e => patch({ day: e.target.value })} /></label><label>Hora sugerida de inicio</label><div className="property-grid">{['09:00', '10:00', '14:00', '16:00'].map(t => <button type="button" className={draft.time === t ? 'selected' : ''} key={t} onClick={() => patch({ time: t })}>{t} {draft.time === t && '✓'}</button>)}</div><div className="section-heading"><h2>Ubicación del inmueble</h2><button type="button" className="text-link" onClick={() => setAddLocation(!addLocation)}>+ Agregar</button></div><ErrorBox message={locations.error} retry={locations.reload} />{locations.loading ? <Loading /> : locations.data?.length ? <Card><label>Dirección guardada<select value={selectedLocation?.id || ''} onChange={e => patch({ locationId: e.target.value })}>{locations.data.map(l => <option key={l.id} value={l.id}>{l.label} · {l.city}</option>)}</select></label><p><strong>{selectedLocation?.label}</strong><br />{selectedLocation?.address}<br />{selectedLocation?.city}</p></Card> : !addLocation && <Empty title="Agrega tu primera dirección" text="Tu dirección exacta solo se compartirá con el trabajador contratado." />}<label>Número interior / Depto<input maxLength={60} value={draft.interior} onChange={e => patch({ interior: e.target.value })} placeholder="Depto 4B, Torre A" /></label><label>Indicaciones de acceso y referencias<textarea rows={3} maxLength={150} value={draft.access} onChange={e => patch({ access: e.target.value })} placeholder="Vigilancia en caseta, acceso por recepción…" /></label><Info><p><strong>Privacidad protegida:</strong> tu dirección exacta se mantiene privada y solo se comparte con el trabajador seleccionado al confirmar la reserva.</p></Info><Button type="submit" className="full" disabled={!selectedLocation || action.pending}>Revisar y publicar solicitud →</Button><Button variant="ghost" className="full" onClick={() => navigate('/solicitar/2')}>Regresar</Button></form>{addLocation && <Card><h2>Nueva dirección</h2><LocationForm onCreated={l => { patch({ locationId: l.id }); setAddLocation(false); locations.reload(); }} onCancel={() => setAddLocation(false)} /></Card>}</>}
    {review && <ConfirmDialog title="Revisa tu solicitud" confirm="Publicar solicitud" pending={action.pending} onClose={() => setReview(false)} onConfirm={publish} error={action.error}><p><strong>{summary}</strong></p><p>{draft.property} · {draft.area} m² · {draft.duration / 60} horas</p><p>{date(`${draft.day}T${draft.time}:00`)}<br />{selectedLocation?.label} · {selectedLocation?.city}</p><Info><p>Publicar no genera un cobro. Recibirás cotizaciones para comparar antes de contratar.</p></Info></ConfirmDialog>}
  </div>;
}

export function Catalog() {
  const categories = useRemote<Category[]>('/service-categories');
  return <div className="page-stack"><PageTitle title="Catálogo de servicios" subtitle="Encuentra la limpieza adecuada para tu espacio." back /><ErrorBox message={categories.error} retry={categories.reload} />{categories.loading ? <Loading /> : categories.data?.map(c => <Card key={c.id}><span className="service-glyph"><Icon name={c.slug.includes('profunda') ? 'imgContainer4' : 'imgContainer3'} /></span><h2>{c.name}</h2><p>{c.description}</p><Link className="button button-primary full" to={`/solicitar/1?servicio=${c.slug.includes('profunda') ? 'profunda' : 'general'}`}>Solicitar servicio →</Link></Card>)}</div>;
}


