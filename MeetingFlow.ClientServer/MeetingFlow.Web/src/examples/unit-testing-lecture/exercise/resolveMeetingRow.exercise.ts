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

// ✏️ EXERCISE (1): Implement this pure function.
// Extract the badge/status logic from MeetingCard.tsx and the permission check
// into this function.
//
// The badge rules match the ternary duplicated in MeetingCard.tsx, MeetingTable.tsx
// and MeetingDetailsPage.tsx. The canPublish rule is new — no shipped component has it.
//
// Rules:
//   - Published → statusLabel: "Published", badgeVariant: "published",
//                 showRegistrations: true, canPublish: false
//   - Draft     → statusLabel: "Draft", badgeVariant: "draft",
//                 showRegistrations: false,
//                 canPublish: true ONLY if role !== "Viewer" AND canManage === true
//   - Cancelled → statusLabel: "Cancelled", badgeVariant: "cancelled",
//                 showRegistrations: false, canPublish: false
export function resolveMeetingRow(input: MeetingRowInput): ResolvedMeetingRow {
  // TODO: implement
  throw new Error('Not implemented — complete this exercise');
}
