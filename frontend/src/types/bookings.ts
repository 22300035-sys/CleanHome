export interface ServiceAddressRequest {
  street: string;
  exteriorNumber: string;
  interiorNumber?: string | null;
  neighborhood: string;
  postalCode: string;
  state: string;
  reference?: string | null;
}

export interface ServiceAddressResponse {
  street: string;
  exteriorNumber: string;
  interiorNumber?: string | null;
  neighborhood: string;
  postalCode: string;
  state: string;
  reference?: string | null;
}

export interface CreateServiceRequestDto {
  serviceCategoryId: string;
  title: string;
  description: string;
  city: string;
  scheduledStart: string;
  estimatedDurationMinutes: number;
  address: ServiceAddressRequest;
}

export interface ServiceRequestDto {
  id: string;
  serviceCategoryId: string;
  title: string;
  description: string;
  city: string;
  scheduledStart: string;
  estimatedDurationMinutes: number;
  status: string;
  createdAt: string;
}

export interface ServiceRequestDetailDto {
  request: ServiceRequestDto;
  address?: ServiceAddressResponse | null;
}

export interface CreateServiceQuoteDto {
  amount: number;
  description: string;
  expiresAt: string;
}

export interface ServiceQuoteDto {
  id: string;
  serviceRequestId: string;
  workerProfileId: string;
  amount: number;
  description: string;
  status: string;
  expiresAt: string;
}

export interface BookingDto {
  id: string;
  serviceRequestId: string;
  serviceQuoteId: string;
  customerUserId: string;
  workerProfileId: string;
  agreedAmount: number;
  scheduledStart: string;
  estimatedDurationMinutes: number;
  status: string;
  address?: ServiceAddressResponse | null;
}

export interface BookingHistoryDto {
  previousStatus: string;
  newStatus: string;
  reason?: string | null;
  createdAt: string;
}

export interface BookingDetailDto {
  booking: BookingDto;
  history: BookingHistoryDto[];
}

export interface CancelBookingDto {
  reason?: string | null;
}

export interface RepeatServiceRequestDto {
  scheduledStart: string;
}
