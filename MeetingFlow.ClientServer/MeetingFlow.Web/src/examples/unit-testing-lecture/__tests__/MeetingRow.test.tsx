import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MeetingRow } from '../after/MeetingRow';

// ✅ Component tests — verify user-visible behavior, not implementation details.
// Uses getByRole and accessible names — not CSS classes or internal state.

describe('MeetingRow', () => {
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

  it('does not show Publish button for a Viewer', () => {
    render(
      <MeetingRow
        title="Team Sync"
        status="Draft"
        registrationCount={0}
        role="Viewer"
        canManage={true}
        onPublish={vi.fn()}
      />,
    );

    expect(
      screen.queryByRole('button', { name: /publish/i }),
    ).not.toBeInTheDocument();
  });

  it('shows publishing state and calls onPublish after clicking Publish', async () => {
    // Arrange: onPublish returns a pending promise so we can observe the transition
    const onPublish = vi.fn(() => new Promise<void>(() => {}));
    const user = userEvent.setup();

    render(
      <MeetingRow
        title="Team Sync"
        status="Draft"
        registrationCount={0}
        role="Admin"
        canManage={true}
        onPublish={onPublish}
      />,
    );

    // Act
    await user.click(screen.getByRole('button', { name: /publish/i }));

    // Assert: visible state changed
    expect(onPublish).toHaveBeenCalledOnce();
    expect(screen.getByText('Publishing…')).toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: /publish/i }),
    ).not.toBeInTheDocument();
  });

  it('shows registration count for a published meeting', () => {
    render(
      <MeetingRow
        title="Annual Review"
        status="Published"
        registrationCount={42}
        role="Viewer"
        canManage={false}
        onPublish={vi.fn()}
      />,
    );

    expect(screen.getByText('42 registrations')).toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: /publish/i }),
    ).not.toBeInTheDocument();
  });
});
