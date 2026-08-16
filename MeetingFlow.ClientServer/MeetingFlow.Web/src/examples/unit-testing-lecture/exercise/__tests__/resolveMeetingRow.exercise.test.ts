import { describe, it, expect } from 'vitest';
import { resolveMeetingRow } from '../resolveMeetingRow.exercise';
import type { MeetingStatus, UserRole } from '../../types';

// ✏️ EXERCISE: Remove .skip once you implement resolveMeetingRow.

describe.skip('resolveMeetingRow (exercise)', () => {
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
      scenario: 'Published → shows registrations, cannot publish',
      input: { status: 'Published', registrationCount: 42, role: 'Admin', canManage: true },
      expected: { statusLabel: 'Published', badgeVariant: 'published', canPublish: false, showRegistrations: true },
    },
    {
      scenario: 'Cancelled → no actions',
      input: { status: 'Cancelled', registrationCount: 10, role: 'Admin', canManage: true },
      expected: { statusLabel: 'Cancelled', badgeVariant: 'cancelled', canPublish: false, showRegistrations: false },
    },
    // ✏️ EXERCISE (2): Add a test case for: Draft + Viewer + canManage → cannot publish
    // {
    //   scenario: '...',
    //   input: { ... },
    //   expected: { ... },
    // },
  ])('$scenario', ({ input, expected }) => {
    const result = resolveMeetingRow(input);

    expect(result).toEqual(expected);
  });
});
