export interface AdminWorkerResponse {
  id: string;
  userId: string;
  displayName: string;
  serviceCity: string;
  serviceArea: string;
  verificationStatus: string;
  isPublished: boolean;
  createdAt: string;
}

export interface AdminWorkerDocumentResponse {
  id: string;
  workerProfileId: string;
  documentType: string;
  reviewStatus: string;
  uploadedAt: string;
}

export interface ReviewRequest {
  status: string;
  reason?: string | null;
}

export interface AdminIncidentResponse {
  id: string;
  reporterUserId: string;
  bookingId: string | null;
  subject: string;
  description: string;
  status: string;
  createdAt: string;
}

export interface ChangeIncidentStatusRequest {
  status: string;
  reason?: string | null;
}

export interface AdminIncidentCommentRequest {
  content: string;
  isInternal: boolean;
}

export interface AdminIncidentCommentResponse {
  id: string;
  authorUserId: string;
  content: string;
  isInternal: boolean;
  createdAt: string;
}

export interface AdminPermissionResponse {
  role: string;
  canManageIncidents: boolean;
  canReviewWorkers: boolean;
}
