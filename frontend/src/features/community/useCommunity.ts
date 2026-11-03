import { useCallback, useState } from 'react';
import { getAccessToken } from '../../services/auth';
import {
  addIncidentComment as addIncidentCommentService,
  createIncident as createIncidentService,
  createReview as createReviewService,
  getChatMessages,
  getIncidentComments,
  getMyIncidents,
  getNotifications,
  getWorkerReviews,
  markChatMessagesRead,
  markNotificationRead as markNotificationReadService,
  sendChatMessage,
} from '../../services/community';
import type {
  AddIncidentCommentRequest,
  ChatMessageResponse,
  CreateIncidentRequest,
  IncidentCommentResponse,
  IncidentResponse,
  NotificationResponse,
  ReviewResponse,
  WorkerRatingResponse,
} from '../../types/community';

export type CommunityState =
  | { kind: 'idle' }
  | { kind: 'messages'; items: ChatMessageResponse[]; bookingId: string }
  | { kind: 'notifications'; items: NotificationResponse[] }
  | { kind: 'workerRating'; workerProfileId: string; rating: WorkerRatingResponse }
  | { kind: 'incidents'; items: IncidentResponse[] }
  | { kind: 'incidentComments'; incidentId: string; items: IncidentCommentResponse[] }
  | { kind: 'error'; status: number; message: string };

export function useCommunity() {
  const [state, setState] = useState<CommunityState>({ kind: 'idle' });
  const token = getAccessToken();

  const loadMessages = useCallback(
    async (bookingId: string) => {
      const items = await getChatMessages(bookingId, token);

      setState({ kind: 'messages', items, bookingId });

      return items;
    },
    [token]
  );

  const sendMessage = useCallback(
    async (bookingId: string, content: string) => {
      const message = await sendChatMessage(
        bookingId,
        { content },
        token
      );

      const items = await getChatMessages(bookingId, token);

      setState({ kind: 'messages', items, bookingId });

      return message;
    },
    [token]
  );

  const markRead = useCallback(
    async (bookingId: string) => {
      await markChatMessagesRead(bookingId, token);
    },
    [token]
  );

  const loadNotifications = useCallback(async () => {
    const items = await getNotifications(token);

    setState({ kind: 'notifications', items });

    return items;
  }, [token]);

  const markNotificationRead = useCallback(
    async (notificationId: string) => {
      await markNotificationReadService(notificationId, token);
    },
    [token]
  );

  const loadWorkerRating = useCallback(
    async (workerProfileId: string) => {
      const rating = await getWorkerReviews(workerProfileId);

      if (rating) {
        setState({
          kind: 'workerRating',
          workerProfileId,
          rating,
        });
      }

      return rating;
    },
    []
  );

  const createIncident = useCallback(
    async (request: CreateIncidentRequest) => {
      const incident = await createIncidentService(request, token);

      const items = await getMyIncidents(token);

      setState({ kind: 'incidents', items });

      return incident;
    },
    [token]
  );

  const loadMyIncidents = useCallback(async () => {
    const items = await getMyIncidents(token);

    setState({ kind: 'incidents', items });

    return items;
  }, [token]);

  const loadIncidentComments = useCallback(
    async (incidentId: string) => {
      const items = await getIncidentComments(incidentId, token);

      setState({ kind: 'incidentComments', incidentId, items });

      return items;
    },
    [token]
  );

  const addIncidentComment = useCallback(
    async (incidentId: string, content: string) => {
      const request: AddIncidentCommentRequest = { content };

      await addIncidentCommentService(incidentId, request, token);
    },
    [token]
  );

  const createReview = useCallback(
    async (
      bookingId: string,
      rating: number,
      comment: string
    ): Promise<ReviewResponse> => {
      const review: ReviewResponse = await createReviewService(
        bookingId,
        { rating, comment },
        token
      );

      return review;
    },
    [token]
  );

  return {
    state,
    loadMessages,
    sendMessage,
    markRead,
    loadNotifications,
    markNotificationRead,
    loadWorkerRating,
    createIncident,
    loadMyIncidents,
    loadIncidentComments,
    addIncidentComment,
    createReview,
  };
}
