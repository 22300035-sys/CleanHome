import { apiGet, apiPatch, apiPut } from './api';
import type {
  AvailabilitySlotRequest,
  UpsertOfferingRequest,
  UpsertWorkerProfileRequest,
  UpdateUserProfileRequest,
  UserProfileResponse,
  WorkerDashboardResponse,
  WorkerListingResponse,
  WorkerOfferingResponse,
  WorkerProfileResponse,
} from '../types/marketplace';

export async function getUserProfile(
  token?: string | null
): Promise<UserProfileResponse> {
  return apiGet<UserProfileResponse>(
    '/api/v1/marketplace/profile',
    { token }
  );
}

export async function updateUserProfile(
  input: UpdateUserProfileRequest,
  token?: string | null
): Promise<UserProfileResponse> {
  return apiPut<UserProfileResponse>(
    '/api/v1/marketplace/profile',
    input,
    { token }
  );
}

export async function getWorkerDashboard(
  token?: string | null
): Promise<WorkerDashboardResponse> {
  return apiGet<WorkerDashboardResponse>(
    '/api/v1/marketplace/worker/profile',
    { token }
  );
}

export async function updateWorkerProfile(
  input: UpsertWorkerProfileRequest,
  token?: string | null
): Promise<WorkerProfileResponse> {
  return apiPut<WorkerProfileResponse>(
    '/api/v1/marketplace/worker/profile',
    input,
    { token }
  );
}

export async function upsertOffering(
  input: UpsertOfferingRequest,
  token?: string | null
): Promise<WorkerOfferingResponse> {
  return apiPut<WorkerOfferingResponse>(
    '/api/v1/marketplace/worker/offerings',
    input,
    { token }
  );
}

export async function setOfferingActive(
  offeringId: string,
  active: boolean,
  token?: string | null
): Promise<void> {
  return apiPatch<void>(
    `/api/v1/marketplace/worker/offerings/${offeringId}/active`,
    { active },
    { token }
  );
}

export async function replaceAvailability(
  slots: AvailabilitySlotRequest[],
  token?: string | null
): Promise<void> {
  return apiPut<void>(
    '/api/v1/marketplace/worker/availability',
    slots,
    { token }
  );
}

export async function setPublication(
  published: boolean,
  token?: string | null
): Promise<void> {
  return apiPatch<void>(
    '/api/v1/marketplace/worker/publication',
    { published },
    { token }
  );
}

export async function browseWorkers(
  city?: string,
  categoryId?: string
): Promise<WorkerListingResponse[]> {
  const params = new URLSearchParams();

  if (city) {
    params.set('city', city);
  }

  if (categoryId) {
    params.set('categoryId', categoryId);
  }

  const query = params.toString();
  const suffix = query ? `?${query}` : '';

  return apiGet<WorkerListingResponse[]>(
    `/api/v1/marketplace/workers${suffix}`
  );
}
