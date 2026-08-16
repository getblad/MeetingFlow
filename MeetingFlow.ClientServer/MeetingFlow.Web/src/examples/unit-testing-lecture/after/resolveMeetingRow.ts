import type { MeetingStatus, UserRole } from '../types';

export interface MeetingRowInput {
  status: MeetingStatus;
  registrationCount: number;
  role: UserRole;
  canManage: boolean;
}

export interface ResolvedMeetingRow {
  statusLabel: string;
  badgeVariant: 'published' | 'draft' | 'cancelled';
  canPublish: boolean;
  showRegistrations: boolean;
}

// ✅ Pure function — no React, no side effects, no network.
// Extracts the badge/status logic from MeetingCard and the permission check
// into one testable place.
export function resolveMeetingRow(input: MeetingRowInput): ResolvedMeetingRow {
  const { status, role, canManage } = input;

  if (status === 'Published') {
    return {
      statusLabel: 'Published',
      badgeVariant: 'published',
      canPublish: false,
      showRegistrations: true,
    };
  }

  if (status === 'Draft') {
    const canPublish = role !== 'Viewer' && canManage;
    return {
      statusLabel: 'Draft',
      badgeVariant: 'draft',
      canPublish,
      showRegistrations: false,
    };
  }

  // Cancelled
  return {
    statusLabel: 'Cancelled',
    badgeVariant: 'cancelled',
    canPublish: false,
    showRegistrations: false,
  };
}
