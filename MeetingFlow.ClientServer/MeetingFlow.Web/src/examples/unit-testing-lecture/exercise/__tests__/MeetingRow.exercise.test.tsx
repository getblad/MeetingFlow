import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MeetingRow } from '../MeetingRow.exercise';

// ✏️ EXERCISE: Remove .skip once resolveMeetingRow is implemented.

describe.skip('MeetingRow (exercise)', () => {
  it('shows Publish button for an authorized user with a draft meeting', () => {
    render(
      <MeetingRow
        title="Team Sync"
        status="Draft"
        registrationCount={0}
        role="Admin"
        canManage={true}
        onPublish={vi.fn()}
      />,
    );

    expect(
      screen.getByRole('button', { name: /publish/i }),
    ).toBeEnabled();
  });

  // ✏️ EXERCISE (3): Add a component test that verifies a Viewer does NOT see
  //   the Publish button. Use queryByRole and .not.toBeInTheDocument().
  //
  // it('does not show Publish button for a Viewer', () => {
  //   ...
  // });
});
