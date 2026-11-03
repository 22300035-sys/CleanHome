import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { useCatalog } from '../../catalog/useCatalog';
import { useBookings } from '../useBookings';
import type { ServiceAddressRequest } from '../../../types/bookings';

const EMPTY_ADDRESS: ServiceAddressRequest = {
  street: '',
  exteriorNumber: '',
  interiorNumber: null,
  neighborhood: '',
  postalCode: '',
  state: '',
  reference: null,
};

export function ServiceRequestForm() {
  const { state, createRequest } = useBookings();
  const { state: catalogState, load: loadCategories } = useCatalog();

  const [serviceCategoryId, setServiceCategoryId] = useState('');
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [city, setCity] = useState('');
  const [scheduledStart, setScheduledStart] = useState('');
  const [estimatedDurationMinutes, setEstimatedDurationMinutes] =
    useState('60');
  const [address, setAddress] =
    useState<ServiceAddressRequest>(EMPTY_ADDRESS);

  useEffect(() => {
    void loadCategories();
  }, [loadCategories]);

  const submit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const input = {
      serviceCategoryId,
      title,
      description,
      city,
      scheduledStart,
      estimatedDurationMinutes: Number(
        estimatedDurationMinutes
      ),
      address,
    };

    await createRequest(input);
  };

  if (state.kind === 'creatingRequest') {
    return <p>Publicando solicitud…</p>;
  }

  return (
    <section className="worker-panel">
      <h2>Nueva solicitud de servicio</h2>
      <form onSubmit={submit}>
        <label>
          Categoría
          <select
            value={serviceCategoryId}
            onChange={(event) =>
              setServiceCategoryId(event.target.value)
            }
          >
            <option value="">Selecciona una categoría</option>
            {catalogState.kind === 'ready'
              ? catalogState.items.map((category) => (
                  <option
                    key={category.id}
                    value={category.id}
                  >
                    {category.name}
                  </option>
                ))
              : null}
          </select>
        </label>
        <label>
          Título
          <input
            value={title}
            onChange={(event) => setTitle(event.target.value)}
          />
        </label>
        <label>
          Descripción
          <textarea
            value={description}
            onChange={(event) =>
              setDescription(event.target.value)
            }
          />
        </label>
        <label>
          Ciudad
          <input
            value={city}
            onChange={(event) => setCity(event.target.value)}
          />
        </label>
        <label>
          Inicio programado
          <input
            type="datetime-local"
            value={scheduledStart}
            onChange={(event) =>
              setScheduledStart(event.target.value)
            }
          />
        </label>
        <label>
          Duración estimada (minutos)
          <input
            type="number"
            min={15}
            step={15}
            value={estimatedDurationMinutes}
            onChange={(event) =>
              setEstimatedDurationMinutes(event.target.value)
            }
          />
        </label>
        <fieldset>
          <legend>Dirección</legend>
          <label>
            Calle
            <input
              value={address.street}
              onChange={(event) =>
                setAddress({
                  ...address,
                  street: event.target.value,
                })
              }
            />
          </label>
          <label>
            Número exterior
            <input
              value={address.exteriorNumber}
              onChange={(event) =>
                setAddress({
                  ...address,
                  exteriorNumber: event.target.value,
                })
              }
            />
          </label>
          <label>
            Número interior
            <input
              value={address.interiorNumber ?? ''}
              onChange={(event) =>
                setAddress({
                  ...address,
                  interiorNumber: event.target.value || null,
                })
              }
            />
          </label>
          <label>
            Colonia
            <input
              value={address.neighborhood}
              onChange={(event) =>
                setAddress({
                  ...address,
                  neighborhood: event.target.value,
                })
              }
            />
          </label>
          <label>
            Código postal
            <input
              value={address.postalCode}
              onChange={(event) =>
                setAddress({
                  ...address,
                  postalCode: event.target.value,
                })
              }
            />
          </label>
          <label>
            Estado
            <input
              value={address.state}
              onChange={(event) =>
                setAddress({
                  ...address,
                  state: event.target.value,
                })
              }
            />
          </label>
          <label>
            Referencia
            <input
              value={address.reference ?? ''}
              onChange={(event) =>
                setAddress({
                  ...address,
                  reference: event.target.value || null,
                })
              }
            />
          </label>
        </fieldset>
        <button type="submit">Publicar solicitud</button>
      </form>
    </section>
  );
}
