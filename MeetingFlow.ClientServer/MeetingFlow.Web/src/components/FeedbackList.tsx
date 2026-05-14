import type { Feedback } from '../types/models';

// Educational baseline:
// This component receives full Feedback entities including ModerationNotes.
// Students should refactor to display only public feedback fields.
export default function FeedbackList({ feedback }: { feedback: Feedback[] }) {
  if (!feedback || feedback.length === 0) {
    return <p>No feedback yet.</p>;
  }

  return (
    <>
      {feedback.map((fb) => (
        <div className="card" key={fb.id} style={{ marginBottom: '0.5rem' }}>
          <strong>Rating: {fb.rating}/5</strong> &mdash; {fb.comment}
          <p className="meta">
            By {fb.attendee?.fullName ?? 'Unknown'} on{' '}
            {new Date(fb.createdAt).toLocaleDateString()}
          </p>
        </div>
      ))}
    </>
  );
}
