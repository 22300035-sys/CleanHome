import { apiGet, apiPost } from './api';
import type {
  AddIncidentCommentRequest,
  ChatMessageResponse,
  CreateIncidentRequest,
  CreateReviewRequest,
  IncidentCommentResponse,
  IncidentResponse,
  NotificationResponse,
  ReviewResponse,
  SendChatMessageRequest,
  WorkerRatingResponse,
} from '../types/community';

export async function getChatMessages(
  bookingId: string,
  token?: string | null
): Promise<ChatMessageResponse[]> {
  return apiGet<ChatMessageResponse[]>(
    `/api/v1/bookings/${bookingId}/messages`,
    { token }
  );
}

export async function sendChatMessage(
  bookingId: string,
  request: SendChatMessageRequest,
  token?: string | null
): Promise<ChatMessageResponse> {
  return apiPost<ChatMessageResponse>(
    `/api/v1/bookings/${bookingId}/messages`,
    request,
    { token }
  );
}

export async function markChatMessagesRead(
  bookingId: string,
  token?: string | null
): Promise<void> {
  return apiPost<void>(
    `/api/v1/bookings/${bookingId}/messages/read`,
    {},
    { token }
  );
}

export async function getNotifications(
  token?: string | null
): Promise<NotificationResponse[]> {
  return apiGet<NotificationResponse[]>('/api/v1/notifications', { token });
}

export async function markNotificationRead(
  notificationId: string,
  token?: string | null
): Promise<void> {
  return apiPost<void>(
    `/api/v1/notifications/${notificationId}/read`,
    {},
    { token }
  );
}

export async function createReview(
  bookingId: string,
  request: CreateReviewRequest,
  token?: string | null
): Promise<ReviewResponse> {
  return apiPost<ReviewResponse>(
    `/api/v1/bookings/${bookingId}/reviews`,
    request,
    { token }
  );
}

export async function getWorkerReviews(
  workerProfileId: string
): Promise<WorkerRatingResponse | null> {
  return apiGet<WorkerRatingResponse>(
    `/api/v1/workers/${workerProfileId}/reviews`
  );
}

export async function createIncident(
  request: CreateIncidentRequest,
  token?: string | null
): Promise<IncidentResponse> {
  return apiPost<IncidentResponse>(
    '/api/v1/incidents',
    request,
    { token }
  );
}

export async function getMyIncidents(
  token?: string | null
): Promise<IncidentResponse[]> {
  return apiGet<IncidentResponse[]>('/api/v1/incidents/mine', { token });
}

export async function getIncidentComments(
  incidentId: string,
  token?: string | null
): Promise<IncidentCommentResponse[]> {
  return apiGet<IncidentCommentResponse[]>(
    `/api/v1/incidents/${incidentId}/comments`,
    { token }
  );
}

export async function addIncidentComment(
  incidentId: string,
  request: AddIncidentCommentRequest,
  token?: string | null
): Promise<void> {
  return apiPost<void>(
    `/api/v1/incidents/${incidentId}/comments`,
    request,
    { token }
  );
}
