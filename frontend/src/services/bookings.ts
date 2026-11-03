import { apiGet, apiPost } from './api';
import type {
  BookingDetailDto,
  BookingDto,
  CancelBookingDto,
  CreateServiceQuoteDto,
  CreateServiceRequestDto,
  RepeatServiceRequestDto,
  ServiceQuoteDto,
  ServiceRequestDetailDto,
  ServiceRequestDto,
} from '../types/bookings';

export async function createServiceRequest(
  input: CreateServiceRequestDto,
  token?: string | null
): Promise<ServiceRequestDto> {
  return apiPost<ServiceRequestDto>(
    '/api/v1/service-requests',
    input,
    { token }
  );
}

export async function getMyServiceRequests(
  token?: string | null
): Promise<ServiceRequestDto[]> {
  return apiGet<ServiceRequestDto[]>(
    '/api/v1/service-requests/mine',
    { token }
  );
}

export async function getServiceRequestDetail(
  id: string,
  token?: string | null
): Promise<ServiceRequestDetailDto> {
  return apiGet<ServiceRequestDetailDto>(
    `/api/v1/service-requests/${id}`,
    { token }
  );
}

export async function createServiceQuote(
  serviceRequestId: string,
  input: CreateServiceQuoteDto,
  token?: string | null
): Promise<ServiceQuoteDto> {
  return apiPost<ServiceQuoteDto>(
    `/api/v1/service-requests/${serviceRequestId}/quotes`,
    input,
    { token }
  );
}

export async function getServiceQuotes(
  serviceRequestId: string,
  token?: string | null
): Promise<ServiceQuoteDto[]> {
  return apiGet<ServiceQuoteDto[]>(
    `/api/v1/service-requests/${serviceRequestId}/quotes`,
    { token }
  );
}

export async function withdrawServiceQuote(
  quoteId: string,
  token?: string | null
): Promise<void> {
  return apiPost<void>(
    `/api/v1/service-quotes/${quoteId}/withdraw`,
    {},
    { token }
  );
}

export async function acceptServiceQuote(
  serviceRequestId: string,
  quoteId: string,
  token?: string | null
): Promise<BookingDto> {
  return apiPost<BookingDto>(
    `/api/v1/service-requests/${serviceRequestId}/quotes/${quoteId}/accept`,
    {},
    { token }
  );
}

export async function getMyBookings(
  token?: string | null
): Promise<BookingDto[]> {
  return apiGet<BookingDto[]>('/api/v1/bookings/mine', { token });
}

export async function getBookingDetail(
  id: string,
  token?: string | null
): Promise<BookingDetailDto> {
  return apiGet<BookingDetailDto>(`/api/v1/bookings/${id}`, { token });
}

export async function cancelBooking(
  id: string,
  input?: CancelBookingDto,
  token?: string | null
): Promise<void> {
  return apiPost<void>(
    `/api/v1/bookings/${id}/cancel`,
    input ?? {},
    { token }
  );
}

export async function repeatServiceRequest(
  id: string,
  input: RepeatServiceRequestDto,
  token?: string | null
): Promise<ServiceRequestDto> {
  return apiPost<ServiceRequestDto>(
    `/api/v1/service-requests/${id}/repeat`,
    input,
    { token }
  );
}

export async function getAvailableServiceRequests(
  token?: string | null
): Promise<ServiceRequestDto[]> {
  return apiGet<ServiceRequestDto[]>(
    '/api/v1/service-requests/available',
    { token }
  );
}

export async function startBooking(
  id: string,
  token?: string | null
): Promise<void> {
  return apiPost<void>(`/api/v1/bookings/${id}/start`, {}, { token });
}

export async function completeBooking(
  id: string,
  token?: string | null
): Promise<void> {
  return apiPost<void>(`/api/v1/bookings/${id}/complete`, {}, { token });
}
