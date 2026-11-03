import { useCallback, useState } from 'react';
import { getAccessToken } from '../../services/auth';
import {
  addIncidentComment as addIncidentCommentService,
  changeIncidentStatus as changeIncidentStatusService,
  getAdminIncidents as getAdminIncidentsService,
  getAdminPermissions as getAdminPermissionsService,
  getAdminWorkerDocuments as getAdminWorkerDocumentsService,
  getAdminWorkers as getAdminWorkersService,
  getIncidentComments as getIncidentCommentsService,
  reviewDocument as reviewDocumentService,
  reviewLocation as reviewLocationService,
  reviewWorker as reviewWorkerService,
} from '../../services/administration';
import type {
  AdminIncidentCommentRequest,
  AdminIncidentCommentResponse,
  AdminIncidentResponse,
  AdminPermissionResponse,
  AdminWorkerDocumentResponse,
  AdminWorkerResponse,
  ChangeIncidentStatusRequest,
  ReviewRequest,
} from '../../types/administration';

export type AdministrationState =
  | { kind: 'idle' }
  | { kind: 'permissions'; permissions: AdminPermissionResponse }
  | { kind: 'workers'; items: AdminWorkerResponse[] }
  | {
      kind: 'workerDocuments';
      workerId: string;
      items: AdminWorkerDocumentResponse[];
    }
  | { kind: 'incidents'; items: AdminIncidentResponse[] }
  | {
      kind: 'incidentComments';
      incidentId: string;
      items: AdminIncidentCommentResponse[];
    }
  | { kind: 'error'; status: number; message: string };

export function useAdministration() {
  const [state, setState] = useState<AdministrationState>({
    kind: 'idle',
  });
  const token = getAccessToken();

  const loadPermissions = useCallback(async (): Promise<void> => {
    const permissions =
      await getAdminPermissionsService(token);

    setState({ kind: 'permissions', permissions });
  }, [token]);

  const loadWorkers = useCallback(async (): Promise<void> => {
    const workers = await getAdminWorkersService(token);

    setState({ kind: 'workers', items: workers });
  }, [token]);

  const loadWorkerDocuments = useCallback(
    async (workerId: string): Promise<void> => {
      const items =
        await getAdminWorkerDocumentsService(workerId, token);

      setState({ kind: 'workerDocuments', workerId, items });
    },
    [token]
  );

  const reviewWorker = useCallback(
    async (workerId: string, request: ReviewRequest): Promise<void> => {
      await reviewWorkerService(workerId, request, token);

      await loadWorkers();
    },
    [loadWorkers, token]
  );

  const reviewDocument = useCallback(
    async (
      documentId: string,
      request: ReviewRequest
    ): Promise<void> => {
      await reviewDocumentService(documentId, request, token);
    },
    [token]
  );

  const reviewLocation = useCallback(
    async (
      locationId: string,
      request: ReviewRequest
    ): Promise<void> => {
      await reviewLocationService(locationId, request, token);
    },
    [token]
  );

  const loadIncidents = useCallback(async (): Promise<void> => {
    const incidents = await getAdminIncidentsService(token);

    setState({ kind: 'incidents', items: incidents });
  }, [token]);

  const changeIncidentStatus = useCallback(
    async (
      incidentId: string,
      request: ChangeIncidentStatusRequest
    ): Promise<void> => {
      await changeIncidentStatusService(incidentId, request, token);
    },
    [token]
  );

  const loadIncidentComments = useCallback(
    async (incidentId: string): Promise<void> => {
      const items =
        await getIncidentCommentsService(incidentId, token);

      setState({
        kind: 'incidentComments',
        incidentId,
        items,
      });
    },
    [token]
  );

  const addIncidentComment = useCallback(
    async (
      incidentId: string,
      request: AdminIncidentCommentRequest
    ): Promise<void> => {
      await addIncidentCommentService(incidentId, request, token);
    },
    [token]
  );

  return {
    state,
    loadPermissions,
    loadWorkers,
    loadWorkerDocuments,
    reviewWorker,
    reviewDocument,
    reviewLocation,
    loadIncidents,
    changeIncidentStatus,
    loadIncidentComments,
    addIncidentComment,
  };
}
