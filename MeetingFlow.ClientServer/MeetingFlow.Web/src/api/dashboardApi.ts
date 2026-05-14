import { get } from './http';
import type { Meeting } from '../types/models';

export type DashboardData = {
  totalMeetings: number;
  totalRegistrations: number;
  totalSpeakers: number;
  averageFeedbackRating: number;
  upcomingMeetings: Meeting[];
};

export function fetchDashboard(): Promise<DashboardData> {
  return get<DashboardData>('/dashboard');
}
