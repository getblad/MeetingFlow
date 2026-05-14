import { useState, useEffect } from "react";
import { useParams, Link } from "react-router-dom";
import { fetchSpeaker } from "../api/speakersApi";
import type { Speaker } from "../types/models";

// Educational baseline:
// This page uses the full Speaker entity including sensitive fields (Email, Phone, InternalNotes).
// In production, a public speaker profile should exclude internal/PII fields.
export default function SpeakerProfilePage() {
  const { id } = useParams<{ id: string }>();
  const [speaker, setSpeaker] = useState<Speaker | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!id) return;
    fetchSpeaker(id)
      .then(setSpeaker)
      .catch((e) => setError(e.message))
      .finally(() => setLoading(false));
  }, [id]);

  if (loading) return <div className="loading">Loading speaker...</div>;
  if (error) return <div className="error">Error: {error}</div>;
  if (!speaker) return <h1>Speaker Not Found</h1>;

  return (
    <>
      <h1>{speaker.fullName}</h1>
      <p>{speaker.bio}</p>

      <h2>Contact</h2>
      <table>
        <tbody>
          <tr>
            <th>Email</th>
            <td>{speaker.email}</td>
          </tr>
          <tr>
            <th>Phone</th>
            <td>{speaker.phone ?? "N/A"}</td>
          </tr>
          <tr>
            <th>Company</th>
            <td>{speaker.company ?? "N/A"}</td>
          </tr>
        </tbody>
      </table>

      {/* Educational baseline:
          InternalNotes should never appear on a public page.
          This is intentionally displayed to show the risk of using entities directly. */}
      {speaker.internalNotes && (
        <>
          <h2>Internal Notes</h2>
          <p className="internal-field">{speaker.internalNotes}</p>
        </>
      )}

      <h2>Sessions</h2>
      {speaker.sessions?.length ? (
        <ul className="session-list">
          {speaker.sessions.map((s) => (
            <li key={s.id}>
              <strong>{s.title}</strong>
              <br />
              <small>
                {s.roomName} &mdash; <Link to={`/meetings/${s.meetingId}`}>View Meeting</Link>
              </small>
            </li>
          ))}
        </ul>
      ) : (
        <p>No sessions assigned.</p>
      )}
    </>
  );
}
