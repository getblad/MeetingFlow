import { describe, it, expect } from 'vitest';
import { resolveMeetingRow } from '../after/resolveMeetingRow';
import type { MeetingStatus, UserRole } from '../types';

// ✅ Table-driven unit tests — no React, no DOM, no mocks.
// These test the same badge/status logic that lives inline in MeetingCard today.

describe('resolveMeetingRow', () => {
  it.each<{
    scenario: string;
    input: { status: MeetingStatus; registrationCount: number; role: UserRole; canManage: boolean };
    expected: { statusLabel: string; badgeVariant: string; canPublish: boolean; showRegistrations: boolean };
  }>([
    {
      scenario: 'Draft + Admin + canManage → can publish',
      input: { status: 'Draft', registrationCount: 0, role: 'Admin', canManage: true },
      expected: { statusLabel: 'Draft', badgeVariant: 'draft', canPublish: true, showRegistrations: false },
    },
    {
      scenario: 'Draft + Organizer + canManage → can publish',
      input: { status: 'Draft', registrationCount: 0, role: 'Organizer', canManage: true },
      expected: { statusLabel: 'Draft', badgeVariant: 'draft', canPublish: true, showRegistrations: false },
    },
    {
      scenario: 'Draft + Viewer + canManage → cannot publish',
      input: { status: 'Draft', registrationCount: 0, role: 'Viewer', canManage: true },
      expected: { statusLabel: 'Draft', badgeVariant: 'draft', canPublish: false, showRegistrations: false },
    },
    {
      scenario: 'Draft + Admin without canManage → cannot publish',
      input: { status: 'Draft', registrationCount: 0, role: 'Admin', canManage: false },
      expected: { statusLabel: 'Draft', badgeVariant: 'draft', canPublish: false, showRegistrations: false },
    },
    {
      scenario: 'Published → shows registrations, cannot publish',
      input: { status: 'Published', registrationCount: 42, role: 'Admin', canManage: true },
      expected: { statusLabel: 'Published', badgeVariant: 'published', canPublish: false, showRegistrations: true },
    },
    {
      scenario: 'Cancelled → no actions available',
      input: { status: 'Cancelled', registrationCount: 10, role: 'Admin', canManage: true },
      expected: { statusLabel: 'Cancelled', badgeVariant: 'cancelled', canPublish: false, showRegistrations: false },
    },
  ])('$scenario', ({ input, expected }) => {
    const result = resolveMeetingRow(input);

    expect(result).toEqual(expected);
  });
});
