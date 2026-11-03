import { apiGet, apiPost } from './api';
import type {
  AdminIncidentCommentRequest,
  AdminIncidentCommentResponse,
  AdminIncidentResponse,
  AdminPermissionResponse,
  AdminWorkerDocumentResponse,
  AdminWorkerResponse,
  ChangeIncidentStatusRequest,
  ReviewRequest,
} from '../types/administration';

export async function getAdminPermissions(
  token?: string | null
): Promise<AdminPermissionResponse> {
  return apiGet<AdminPermissionResponse>(
    '/api/v1/admin/me',
    { token }
  );
}

export async function getAdminWorkers(
  token?: string | null,
  status?: string
): Promise<AdminWorkerResponse[]> {
  const params = new URLSearchParams();

  if (status) {
    params.set('status', status);
  }

  const query = params.toString();
  const suffix = query ? `?${query}` : '';

  return apiGet<AdminWorkerResponse[]>(
    `/api/v1/admin/workers${suffix}`,
    { token }
  );
}

export async function getAdminWorkerDocuments(
  workerId: string,
  token?: string | null
): Promise<AdminWorkerDocumentResponse[]> {
  return apiGet<AdminWorkerDocumentResponse[]>(
    `/api/v1/admin/workers/${workerId}/documents`,
    { token }
  );
}

export async function reviewWorker(
  workerId: string,
  request: ReviewRequest,
  token?: string | null
): Promise<void> {
  return apiPost<void>(
    `/api/v1/admin/workers/${workerId}/review`,
    request,
    { token }
  );
}

export async function reviewDocument(
  documentId: string,
  request: ReviewRequest,
  token?: string | null
): Promise<void> {
  return apiPost<void>(
    `/api/v1/admin/documents/${documentId}/review`,
    request,
    { token }
  );
}

export async function reviewLocation(
  locationId: string,
  request: ReviewRequest,
  token?: string | null
): Promise<void> {
  return apiPost<void>(
    `/api/v1/admin/locations/${locationId}/review`,
    request,
    { token }
  );
}

export async function getAdminIncidents(
  token?: string | null,
  status?: string
): Promise<AdminIncidentResponse[]> {
  const params = new URLSearchParams();

  if (status) {
    params.set('status', status);
  }

  const query = params.toString();
  const suffix = query ? `?${query}` : '';

  return apiGet<AdminIncidentResponse[]>(
    `/api/v1/admin/incidents${suffix}`,
    { token }
  );
}

export async function changeIncidentStatus(
  incidentId: string,
  request: ChangeIncidentStatusRequest,
  token?: string | null
): Promise<void> {
  return apiPost<void>(
    `/api/v1/admin/incidents/${incidentId}/status`,
    request,
    { token }
  );
}

export async function getIncidentComments(
  incidentId: string,
  token?: string | null
): Promise<AdminIncidentCommentResponse[]> {
  return apiGet<AdminIncidentCommentResponse[]>(
    `/api/v1/admin/incidents/${incidentId}/comments`,
    { token }
  );
}

export async function addIncidentComment(
  incidentId: string,
  request: AdminIncidentCommentRequest,
  token?: string | null
): Promise<void> {
  return apiPost<void>(
    `/api/v1/admin/incidents/${incidentId}/comments`,
    request,
    { token }
  );
}
