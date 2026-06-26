/** Shared badge/label helpers for offer requests (used by the list and detail screens). */

import { OfferRequest } from '@shared/data/offer-request.model';

/** Badge class for the engagement submission status. */
export function offerStatusBadge(status: string): string {
  switch (status) {
    case 'Published':
    case 'Accepted': return 'badge-green';
    case 'In Review': return 'badge-blue';
    case 'Rejected': return 'badge-red';
    case 'Completed': return 'badge-purple';
    default: return 'badge-gray';
  }
}

/** Badge class for the captured response status. */
export function offerResponseBadge(o: OfferRequest): string {
  if (!o.responseReceived) return 'badge-orange';
  switch (o.responseStatus) {
    case 'Accepted': return 'badge-green';
    case 'Rejected': return 'badge-red';
    case 'Changes Requested': return 'badge-orange';
    default: return 'badge-gray';
  }
}

/** Human label for the response state (used in lists + filters). */
export function offerResponseLabel(o: OfferRequest): string {
  return o.responseReceived ? o.responseStatus : 'Awaiting response';
}
