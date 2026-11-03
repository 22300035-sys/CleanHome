export interface SendChatMessageRequest {
  content: string;
}

export interface ChatMessageResponse {
  id: string;
  bookingId: string;
  senderUserId: string;
  content: string;
  createdAt: string;
  readAt: string | null;
}

export interface NotificationResponse {
  id: string;
  type: string;
  title: string;
  body: string;
  relatedEntityId: string | null;
  createdAt: string;
  readAt: string | null;
}

export interface CreateReviewRequest {
  rating: number;
  comment: string;
}

export interface ReviewResponse {
  id: string;
  bookingId: string;
  workerProfileId: string;
  rating: number;
  comment: string;
  createdAt: string;
}

export interface WorkerRatingResponse {
  workerProfileId: string;
  reviewCount: number;
  averageRating: number;
  reviews: ReviewResponse[];
}

export interface CreateIncidentRequest {
  bookingId: string | null;
  subject: string;
  description: string;
}

export interface IncidentResponse {
  id: string;
  bookingId: string | null;
  subject: string;
  description: string;
  status: string;
  createdAt: string;
}

export interface AddIncidentCommentRequest {
  content: string;
}

export interface IncidentCommentResponse {
  id: string;
  content: string;
  createdAt: string;
}
