import { useCallback, useState } from 'react';
import { getServiceCategories } from '../../services/catalog';
import type { ServiceCategoryDto } from '../../types/catalog';

export type CatalogState =
  | { kind: 'loading' }
  | { kind: 'ready'; items: ServiceCategoryDto[] }
  | { kind: 'error'; status: number; message: string };

export function useCatalog() {
  const [state, setState] = useState<CatalogState>({
    kind: 'loading',
  });

  const load = useCallback(async (): Promise<ServiceCategoryDto[]> => {
    const items = await getServiceCategories();

    setState({ kind: 'ready', items });

    return items;
  }, []);

  return { state, load };
}
