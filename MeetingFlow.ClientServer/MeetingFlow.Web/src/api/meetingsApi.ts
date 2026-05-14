import { get } from './http';
import type { Meeting } from '../types/models';

export function fetchMeetings(): Promise<Meeting[]> {
  return get<Meeting[]>('/meetings');
}

export function fetchMeeting(id: string): Promise<Meeting> {
  return get<Meeting>(`/meetings/${id}`);
}

export function fetchAdminMeetings(): Promise<Meeting[]> {
  return get<Meeting[]>('/admin/meetings');
}
