export interface UpdateUserProfileRequest {
  city: string;
  phone?: string | null;
}

export interface UserProfileResponse {
  userId: string;
  city: string;
  phone?: string | null;
}

export interface UpsertWorkerProfileRequest {
  description: string;
  serviceCity: string;
  serviceArea: string;
}

export interface WorkerProfileResponse {
  id: string;
  userId: string;
  description: string;
  serviceCity: string;
  serviceArea: string;
  verificationStatus: string;
  isPublished: boolean;
}

export interface UpsertOfferingRequest {
  serviceCategoryId: string;
  referencePrice: number;
  description: string;
}

export interface WorkerOfferingResponse {
  id: string;
  serviceCategoryId: string;
  categoryName: string;
  referencePrice: number;
  description: string;
  isActive: boolean;
}

export interface AvailabilitySlotRequest {
  dayOfWeek: number;
  startTime: string;
  endTime: string;
}

export interface AvailabilitySlotResponse {
  id: string;
  dayOfWeek: number;
  startTime: string;
  endTime: string;
}

export interface WorkerListingResponse {
  id: string;
  displayName: string;
  description: string;
  serviceCity: string;
  serviceArea: string;
  offerings: WorkerOfferingResponse[];
}

export interface WorkerDashboardResponse {
  profile: WorkerProfileResponse;
  offerings: WorkerOfferingResponse[];
  availability: AvailabilitySlotResponse[];
}
