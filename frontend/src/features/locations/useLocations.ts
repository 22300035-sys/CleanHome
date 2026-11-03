import { useCallback, useState } from 'react';
import { createLocation, getMyLocations } from '../../services/locations';
import { getAccessToken } from '../../services/auth';
import type {
  CustomerLocationRequest,
  CustomerLocationResponse,
} from '../../types/locations';

export type LocationsState =
  | { kind: 'idle' }
  | { kind: 'creating' }
  | { kind: 'ready'; items: CustomerLocationResponse[] }
  | { kind: 'error'; status: number; message: string };

export function useLocations() {
  const [state, setState] = useState<LocationsState>({ kind: 'idle' });
  const token = getAccessToken();

  const create = useCallback(
    async (input: CustomerLocationRequest) => {
      setState({ kind: 'creating' });

      const location = await createLocation(input, token);

      const items = await getMyLocations(token);

      setState({ kind: 'ready', items });

      return location;
    },
    [token]
  );

  const load = useCallback(async (): Promise<CustomerLocationResponse[]> => {
    const items = await getMyLocations(token);

    setState({ kind: 'ready', items });

    return items;
  }, [token]);

  const refresh = useCallback(async (): Promise<void> => {
    const items = await getMyLocations(token);

    setState({ kind: 'ready', items });
  }, [token]);

  return { state, create, load, refresh };
}
