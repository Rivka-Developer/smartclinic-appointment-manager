export type SwapOfferStatus = 'Active' | 'Accepted' | 'Cancelled';

export interface SwapOfferResponse {
  id: string;
  appointmentId: string;
  appointmentStartTime: string;
  appointmentDurationMinutes: number;
  offeredByName: string;
  status: SwapOfferStatus;
  createdAt: string;
}

export interface CreateSwapOfferRequest {
  appointmentId: string;
}

export interface AdminSwapOfferResponse {
  id: string;
  appointmentId: string;
  appointmentStartTime: string;
  appointmentDurationMinutes: number;
  offeredByName: string;
  offeredByEmail: string;
  currentOwnerName: string;
  status: SwapOfferStatus;
  createdAt: string;
  acceptedAt: string | null;
  acceptedByName: string | null;
}

export interface AdminAcceptOfferRequest {
  targetClientId: string;
}
