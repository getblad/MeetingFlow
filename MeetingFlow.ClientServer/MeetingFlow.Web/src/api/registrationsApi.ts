import { post } from './http';
import type { Registration } from '../types/models';

export interface CreateRegistrationRequest {
  meetingId: string;
  attendeeName: string;
  attendeeEmail: string;
  ticketType: string;
}

// Educational baseline:
// This posts the full Registration entity to the API.
// In production, prefer a dedicated CreateRegistrationRequest type.
export function createRegistration(request: CreateRegistrationRequest): Promise<Registration> {
  return post<Registration>('/registrations', request);
}
