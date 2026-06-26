/**
 * Offer Request — mirrors the backend OfferRequest contract. A record of an engagement pushed to
 * a destination (SaaSify / marketplace), the JSON payload sent, and the response captured back.
 */

export interface OfferRequest {
  id: string;
  dealId: string;
  engagementName: string;
  customer: string;
  engagementType: string;
  marketplace: string;
  products: string[];
  destination: string;
  status: string;
  value: number;
  submittedAt: string;
  submittedBy: string;
  requestJson: string;

  changedSinceSubmission: boolean;
  lastChangeSummary: string;

  responseReceived: boolean;
  responseStatus: string;
  responseReference: string;
  responseNotes: string;
  responseJson: string;
  responseAt: string;
  responseBy: string;
}

export interface CaptureResponseRequest {
  status: string;
  reference?: string;
  notes?: string;
  json?: string;
  user?: string;
}
