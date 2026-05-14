import { useState, useEffect } from "react";
import { useParams } from "react-router-dom";
import { fetchMeeting } from "../api/meetingsApi";
import SessionList from "../components/SessionList";
import FeedbackList from "../components/FeedbackList";
import type { Meeting } from "../types/models";

// Educational baseline:
// This page uses the full Meeting entity with all nested navigation properties.
// In production, prefer an MeetingDetailsResponse with only the fields needed for this page.
export default function MeetingDetailsPage() {
  const { id } = useParams<{ id: string }>();
  const [meeting, setMeeting] = useState<Meeting | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!id) return;
    fetchMeeting(id)
      .then(setMeeting)
      .catch((e) => setError(e.message))
      .finally(() => setLoading(false));
  }, [id]);

  if (loading) return <div className="loading">Loading meeting...</div>;
  if (error) return <div className="error">Error: {error}</div>;
  if (!meeting) return <h1>Meeting Not Found</h1>;

  const badgeClass =
    meeting.status === "Published" ? "badge-published" : meeting.status === "Draft" ? "badge-draft" : "badge-cancelled";

  const avgRating = meeting.feedback?.length
    ? (meeting.feedback.reduce((sum, f) => sum + f.rating, 0) / meeting.feedback.length).toFixed(1)
    : "N/A";

  return (
    <>
      <h1>{meeting.title}</h1>
      <p>
        <span className={`badge ${badgeClass}`}>{meeting.status}</span>
      </p>
      <p>{meeting.description}</p>

      <h2>Details</h2>
      <table>
        <tbody>
          <tr>
            <th>Venue</th>
            <td>
              {meeting.venue?.name}, {meeting.venue?.address}, {meeting.venue?.city}
            </td>
          </tr>
          <tr>
            <th>Starts</th>
            <td>{new Date(meeting.startsAt).toLocaleString()}</td>
          </tr>
          <tr>
            <th>Ends</th>
            <td>{new Date(meeting.endsAt).toLocaleString()}</td>
          </tr>
          <tr>
            <th>Registrations</th>
            <td>{meeting.registrations?.length ?? 0}</td>
          </tr>
          <tr>
            <th>Feedback</th>
            <td>
              {meeting.feedback?.length ?? 0} entries (avg: {avgRating})
            </td>
          </tr>
        </tbody>
      </table>

      <h2>Sessions</h2>
      {meeting.sessions?.length ? <SessionList sessions={meeting.sessions} /> : <p>No sessions scheduled yet.</p>}

      <h2>Feedback</h2>
      <FeedbackList feedback={meeting.feedback ?? []} />
    </>
  );
}
