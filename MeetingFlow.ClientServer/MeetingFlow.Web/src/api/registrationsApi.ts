import { post } from './http';
import type { Registration } from '../types/models';

// Educational baseline:
// This posts the full Registration entity to the API.
// In production, prefer a dedicated CreateRegistrationRequest type.
export function createRegistration(registration: Partial<Registration>): Promise<Registration> {
  return post<Registration>('/registrations', registration);
}
