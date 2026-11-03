import { apiGet, apiPost } from './api';
import type {
  CustomerLocationRequest,
  CustomerLocationResponse,
} from '../types/locations';

export async function createLocation(
  input: CustomerLocationRequest,
  token?: string | null
): Promise<CustomerLocationResponse> {
  return apiPost<CustomerLocationResponse>(
    '/api/v1/locations',
    input,
    { token }
  );
}

export async function getMyLocations(
  token?: string | null
): Promise<CustomerLocationResponse[]> {
  return apiGet<CustomerLocationResponse[]>('/api/v1/locations', { token });
}
