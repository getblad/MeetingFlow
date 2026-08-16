import { useState, useEffect } from 'react';

// ❌ BEFORE: Hard to test — mirrors the existing MeetingCard + MeetingDetailsPage patterns.
//
// Same problems that make the current components hard to unit-test:
//   1. Fetches meeting data internally (like MeetingDetailsPage) — needs network mocking
//   2. Badge/status logic is inline in JSX (like MeetingCard) — can't test without rendering
//   3. Permission check for Publish is buried in JSX — can't test without rendering
//   4. Publish handler does a fetch — hidden side effect

export function MeetingRow({
  meetingId,
  userRole,
  canManageMeetings,
}: {
  meetingId: string;
  userRole: 'Admin' | 'Organizer' | 'Viewer';
  canManageMeetings: boolean;
}) {
  const [meeting, setMeeting] = useState<{
    title: string;
    status: string;
    registrations: unknown[];
  } | null>(null);
  const [publishing, setPublishing] = useState(false);

  // ❌ Side effect inside the component — needs network mocking to test
  useEffect(() => {
    fetch(`/api/meetings/${meetingId}`)
      .then((res) => res.json())
      .then(setMeeting);
  }, [meetingId]);

  if (!meeting) return <div>Loading...</div>;

  const handlePublish = async () => {
    setPublishing(true);
    // ❌ Another fetch buried in event handler
    await fetch(`/api/meetings/${meetingId}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ ...meeting, status: 'Published' }),
    });
    setMeeting({ ...meeting, status: 'Published' });
    setPublishing(false);
  };

  // ❌ Business logic scattered through JSX — same pattern as MeetingCard badge logic
  const badgeClass =
    meeting.status === 'Published'
      ? 'badge-published'
      : meeting.status === 'Draft'
        ? 'badge-draft'
        : 'badge-cancelled';

  return (
    <div className="card">
      <span>{meeting.title}</span>
      <span className={`badge ${badgeClass}`}>{meeting.status}</span>
      <span>
        {meeting.status === 'Published'
          ? `${meeting.registrations?.length ?? 0} registrations`
          : '—'}
      </span>
      {meeting.status === 'Draft' &&
        userRole !== 'Viewer' &&
        canManageMeetings && (
          <button onClick={handlePublish} disabled={publishing}>
            {publishing ? 'Publishing…' : 'Publish'}
          </button>
        )}
    </div>
  );
}
