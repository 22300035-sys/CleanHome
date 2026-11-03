import { useCallback, useState } from 'react';
import { getAccessToken } from '../../services/auth';
import {
  acceptServiceQuote,
  cancelBooking as cancelBookingService,
  completeBooking as completeBookingService,
  createServiceQuote,
  createServiceRequest,
  getAvailableServiceRequests,
  getMyBookings as getBookingsService,
  getBookingDetail as getBookingDetailService,
  getMyServiceRequests,
  getServiceQuotes,
  getServiceRequestDetail as getServiceRequestDetailService,
  repeatServiceRequest,
  startBooking as startBookingService,
  withdrawServiceQuote,
} from '../../services/bookings';
import type {
  BookingDetailDto,
  BookingDto,
  CreateServiceQuoteDto,
  CreateServiceRequestDto,
  ServiceQuoteDto,
  ServiceRequestDetailDto,
  ServiceRequestDto,
} from '../../types/bookings';

export type BookingsState =
  | { kind: 'idle' }
  | { kind: 'creatingRequest' }
  | { kind: 'requests'; items: ServiceRequestDto[] }
  | { kind: 'requestDetail'; detail: ServiceRequestDetailDto }
  | { kind: 'quotes'; requestId: string; items: ServiceQuoteDto[] }
  | { kind: 'bookingDetail'; detail: BookingDetailDto }
  | { kind: 'bookings'; items: BookingDto[] }
  | { kind: 'available'; items: ServiceRequestDto[] }
  | { kind: 'error'; status: number; message: string };

export function useBookings() {
  const [state, setState] = useState<BookingsState>({ kind: 'idle' });
  const token = getAccessToken();

  const createRequest = useCallback(
    async (input: CreateServiceRequestDto) => {
      setState({ kind: 'creatingRequest' });

      const request = await createServiceRequest(input, token);

      setState({ kind: 'requests', items: [request] });

      return request;
    },
    [token]
  );

  const getRequests = useCallback(
    async (): Promise<ServiceRequestDto[]> => {
      const requests = await getMyServiceRequests(token);

      setState({ kind: 'requests', items: requests });

      return requests;
    },
    [token]
  );

  const getRequestDetail = useCallback(
    async (id: string): Promise<ServiceRequestDetailDto> => {
      const detail = await getServiceRequestDetailService(id, token);

      setState({ kind: 'requestDetail', detail });

      return detail;
    },
    [token]
  );

  const createQuote = useCallback(
    async (
      serviceRequestId: string,
      input: CreateServiceQuoteDto
    ): Promise<ServiceQuoteDto> => {
      const quote = await createServiceQuote(
        serviceRequestId,
        input,
        token
      );

      setState({
        kind: 'quotes',
        requestId: serviceRequestId,
        items: [quote],
      });

      return quote;
    },
    [token]
  );

  const getQuotes = useCallback(
    async (
      serviceRequestId: string
    ): Promise<ServiceQuoteDto[]> => {
      const quotes = await getServiceQuotes(serviceRequestId, token);

      setState({
        kind: 'quotes',
        requestId: serviceRequestId,
        items: quotes,
      });

      return quotes;
    },
    [token]
  );

  const withdrawQuote = useCallback(
    async (quoteId: string): Promise<void> => {
      await withdrawServiceQuote(quoteId, token);
    },
    [token]
  );

  const acceptQuote = useCallback(
    async (
      serviceRequestId: string,
      quoteId: string
    ): Promise<BookingDto> => {
      const booking = await acceptServiceQuote(
        serviceRequestId,
        quoteId,
        token
      );

      setState({ kind: 'bookings', items: [booking] });

      return booking;
    },
    [token]
  );

  const getBookings = useCallback(async (): Promise<BookingDto[]> => {
    const bookings = await getBookingsService(token);

    setState({ kind: 'bookings', items: bookings });

    return bookings;
  }, [token]);

  const getBookingDetail = useCallback(
    async (id: string): Promise<BookingDetailDto> => {
      const detail = await getBookingDetailService(id, token);

      setState({ kind: 'bookingDetail', detail });

      return detail;
    },
    [token]
  );

  const cancelBooking = useCallback(
    async (id: string, reason?: string | null): Promise<void> => {
      await cancelBookingService(id, { reason }, token);
    },
    [token]
  );

  const repeatRequest = useCallback(
    async (
      id: string,
      scheduledStart: string
    ): Promise<ServiceRequestDto> => {
      const request = await repeatServiceRequest(
        id,
        { scheduledStart },
        token
      );

      setState({ kind: 'requests', items: [request] });

      return request;
    },
    [token]
  );

  const getAvailableRequests = useCallback(
    async (): Promise<ServiceRequestDto[]> => {
      const requests = await getAvailableServiceRequests(token);

      setState({ kind: 'available', items: requests });

      return requests;
    },
    [token]
  );

  const startBooking = useCallback(
    async (id: string): Promise<void> => {
      await startBookingService(id, token);
    },
    [token]
  );

  const completeBooking = useCallback(
    async (id: string): Promise<void> => {
      await completeBookingService(id, token);
    },
    [token]
  );

  return {
    state,
    createRequest,
    getRequests,
    getRequestDetail,
    createQuote,
    getQuotes,
    withdrawQuote,
    acceptQuote,
    getBookings,
    getBookingDetail,
    cancelBooking,
    repeatRequest,
    getAvailableRequests,
    startBooking,
    completeBooking,
  };
}
