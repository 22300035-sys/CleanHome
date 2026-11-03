import { ServiceQuotesPanel } from './ServiceQuotesPanel';
import type {
  ServiceRequestDetailDto,
} from '../../../types/bookings';

interface ServiceRequestDetailProps {
  detail: ServiceRequestDetailDto;
}

export function ServiceRequestDetail({
  detail,
}: ServiceRequestDetailProps) {
  return (
    <div className="worker-offerings">
      <p>
        {detail.request.title} — {detail.request.city} —{' '}
        {detail.request.status}
      </p>
      {detail.address ? (
        <p>
          {detail.address.street} {detail.address.exteriorNumber},{' '}
          {detail.address.neighborhood} {detail.address.postalCode}
        </p>
      ) : null}
      <ServiceQuotesPanel requestId={detail.request.id} />
    </div>
  );
}
