import { useCallback, useEffect, useState } from 'react';
import type { ChangeEvent, FormEvent } from 'react';
import { useCatalog } from '../../catalog/useCatalog';
import { useMarketplace } from '../useMarketplace';

const UUID_PATTERN =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

export function OfferingSection() {
  const {
    state,
    getWorkerDashboard,
    upsertOffering,
    setOfferingActive,
  } = useMarketplace();

  const { state: catalogState, load: loadCategories } = useCatalog();

  const [categoryId, setCategoryId] = useState('');
  const [referencePrice, setReferencePrice] = useState('0');
  const [description, setDescription] = useState('');
  const [saved, setSaved] = useState(false);

  useEffect(() => {
    void loadCategories();
  }, [loadCategories]);

  const loadDashboard = useCallback(
    () => getWorkerDashboard(),
    [getWorkerDashboard]
  );

  useEffect(() => {
    void loadDashboard();
  }, [loadDashboard]);

  const createOffering = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    if (!UUID_PATTERN.test(categoryId)) {
      return;
    }

    const input = {
      serviceCategoryId: categoryId,
      referencePrice: Number(referencePrice),
      description,
    };

    await upsertOffering(input);

    setSaved(true);
  };

  const toggleActive = async (
    offeringId: string,
    isActive: boolean
  ) => {
    await setOfferingActive(offeringId, !isActive);
  };

  const onCategoryChange = (event: ChangeEvent<HTMLSelectElement>) => {
    setCategoryId(event.target.value);
  };

  const onPriceChange = (event: ChangeEvent<HTMLInputElement>) => {
    setReferencePrice(event.target.value);
  };

  if (state.kind !== 'workerProfile') {
    return (
      <section className="worker-panel">
        <h2>Mis servicios</h2>
        <p>Cargando servicios…</p>
      </section>
    );
  }

  const offerings = state.profile.offerings;

  return (
    <section className="worker-panel">
      <h2>Mis servicios</h2>
      {state.kind === 'workerProfile' && offerings.length > 0 ? (
        <ul className="worker-offerings">
          {offerings.map((offering) => (
            <li key={offering.id}>
              <span>
                {offering.categoryName}: ${offering.referencePrice.toFixed(2)}
              </span>
              <button
                type="button"
                onClick={() =>
                  void toggleActive(offering.id, offering.isActive)
                }
              >
                {offering.isActive ? 'Desactivar' : 'Activar'}
              </button>
            </li>
          ))}
        </ul>
      ) : null}
      <form onSubmit={createOffering}>
        <label>
          Categoría
          <select value={categoryId} onChange={onCategoryChange}>
            <option value="">Selecciona una categoría</option>
            {catalogState.kind === 'ready'
              ? catalogState.items.map((category) => (
                  <option key={category.id} value={category.id}>
                    {category.name}
                  </option>
                ))
              : null}
          </select>
        </label>
        <label>
          Precio de referencia
          <input
            type="number"
            step="0.01"
            min="0"
            value={referencePrice}
            onChange={onPriceChange}
          />
        </label>
        <label>
          Descripción
          <input
            value={description}
            onChange={(event) => setDescription(event.target.value)}
          />
        </label>
        <button type="submit">Crear/actualizar servicio</button>
      </form>
      {saved ? <p>Servicio guardado.</p> : null}
    </section>
  );
}
