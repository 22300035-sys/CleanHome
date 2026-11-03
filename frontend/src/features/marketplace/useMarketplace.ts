import { useCallback, useState } from 'react';
import { getAccessToken } from '../../services/auth';
import {
  browseWorkers as fetchBrowseWorkers,
  getUserProfile as fetchUserProfile,
  getWorkerDashboard as fetchWorkerDashboard,
  replaceAvailability as fetchReplaceAvailability,
  setOfferingActive as fetchSetOfferingActive,
  setPublication as fetchSetPublication,
  updateUserProfile as fetchUpdateUserProfile,
  updateWorkerProfile as fetchUpdateWorkerProfile,
  upsertOffering as fetchUpsertOffering,
} from '../../services/marketplace';
import type {
  AvailabilitySlotRequest,
  UpdateUserProfileRequest,
  UpsertOfferingRequest,
  UpsertWorkerProfileRequest,
  UserProfileResponse,
  WorkerDashboardResponse,
  WorkerListingResponse,
  WorkerOfferingResponse,
  WorkerProfileResponse,
} from '../../types/marketplace';

export type MarketplaceState =
  | { kind: 'idle' }
  | { kind: 'userProfile'; profile: UserProfileResponse }
  | { kind: 'workerProfile'; profile: WorkerDashboardResponse }
  | { kind: 'workers'; items: WorkerListingResponse[] }
  | { kind: 'error'; status: number; message: string };

export function useMarketplace() {
  const [state, setState] = useState<MarketplaceState>({
    kind: 'idle',
  });

  const token = getAccessToken();

  const getUserProfile = useCallback(
    async (): Promise<UserProfileResponse> => {
      const profile = await fetchUserProfile(token);

      setState({ kind: 'userProfile', profile });

      return profile;
    },
    [token]
  );

  const updateUserProfile = useCallback(
    async (
      input: UpdateUserProfileRequest
    ): Promise<UserProfileResponse> => {
      const profile = await fetchUpdateUserProfile(input, token);

      setState({ kind: 'userProfile', profile });

      return profile;
    },
    [token]
  );

  const getWorkerDashboard = useCallback(
    async (): Promise<WorkerDashboardResponse> => {
      const dashboard = await fetchWorkerDashboard(token);

      setState({ kind: 'workerProfile', profile: dashboard });

      return dashboard;
    },
    [token]
  );

  const updateWorkerProfile = useCallback(
    async (
      input: UpsertWorkerProfileRequest
    ): Promise<WorkerProfileResponse> => {
      const profile = await fetchUpdateWorkerProfile(input, token);

      const dashboard = await fetchWorkerDashboard(token);

      setState({ kind: 'workerProfile', profile: dashboard });

      return profile;
    },
    [token]
  );

  const upsertOffering = useCallback(
    async (
      input: UpsertOfferingRequest
    ): Promise<WorkerOfferingResponse> => {
      const offering = await fetchUpsertOffering(input, token);

      const dashboard = await fetchWorkerDashboard(token);

      setState({ kind: 'workerProfile', profile: dashboard });

      return offering;
    },
    [token]
  );

  const setOfferingActive = useCallback(
    async (
      offeringId: string,
      active: boolean
    ): Promise<void> => {
      await fetchSetOfferingActive(offeringId, active, token);
    },
    [token]
  );

  const replaceAvailability = useCallback(
    async (slots: AvailabilitySlotRequest[]): Promise<void> => {
      await fetchReplaceAvailability(slots, token);
    },
    [token]
  );

  const setPublication = useCallback(
    async (published: boolean): Promise<void> => {
      await fetchSetPublication(published, token);
    },
    [token]
  );

  const browseWorkers = useCallback(
    async (
      city?: string,
      categoryId?: string
    ): Promise<WorkerListingResponse[]> => {
      const workers = await fetchBrowseWorkers(city, categoryId);

      setState({ kind: 'workers', items: workers });

      return workers;
    },
    []
  );

  return {
    state,
    getUserProfile,
    updateUserProfile,
    getWorkerDashboard,
    updateWorkerProfile,
    upsertOffering,
    setOfferingActive,
    replaceAvailability,
    setPublication,
    browseWorkers,
  };
}
