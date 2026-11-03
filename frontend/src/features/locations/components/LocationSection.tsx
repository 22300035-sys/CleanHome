import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { useLocations } from '../useLocations';
import type { CustomerLocationRequest } from '../../../types/locations';

export function LocationSection() {
  const { state, create, load } = useLocations();
  const [form, setForm] = useState<CustomerLocationRequest>({
    label: '',
    street: '',
    exteriorNumber: '',
    interiorNumber: null,
    neighborhood: '',
    postalCode: '',
    city: '',
    state: '',
    reference: null,
  });

  useEffect(() => {
    void load();
  }, [load]);

  const submit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    await create(form);
  };

  const updateField = (
    field: keyof CustomerLocationRequest,
    value: string
  ) => {
    setForm({
      ...form,
      [field]: value,
    });
  };

  if (state.kind === 'creating') {
    return <p>Guardando ubicación…</p>;
  }

  if (state.kind === 'error') {
    return <p role="alert">{state.message}</p>;
  }

  const textFields: Array<{
    field: keyof CustomerLocationRequest;
    label: string;
    type?: string;
  }> = [
    { field: 'label', label: 'Etiqueta' },
    { field: 'street', label: 'Calle' },
    { field: 'exteriorNumber', label: 'Número exterior' },
    { field: 'neighborhood', label: 'Colonia' },
    { field: 'postalCode', label: 'Código postal' },
    { field: 'city', label: 'Ciudad' },
    { field: 'state', label: 'Estado' },
  ];

  return (
    <section className="worker-panel">
      <h2>Mis ubicaciones</h2>
      {state.kind === 'ready' && state.items.length > 0 ? (
        <ul className="worker-offerings">
          {state.items.map((location) => (
            <li key={location.id}>
              {location.label} — {location.street} {location.exteriorNumber}{' '}
              — {location.city}
            </li>
          ))}
        </ul>
      ) : null}
      <form onSubmit={submit}>
        {textFields.map(({ field, label }) => (
          <label key={field}>
            {label}
            <input
              value={form[field] as string}
              onChange={(event) => updateField(field, event.target.value)}
            />
          </label>
        ))}
        <label>
          Número interior
          <input
            value={form.interiorNumber ?? ''}
            onChange={(event) => updateField('interiorNumber', event.target.value)}
          />
        </label>
        <label>
          Referencia
          <input
            value={form.reference ?? ''}
            onChange={(event) => updateField('reference', event.target.value)}
          />
        </label>
        <button type="submit">Guardar ubicación</button>
      </form>
    </section>
  );
}
