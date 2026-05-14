import { useState, useEffect } from "react";
import { fetchMeetings } from "../api/meetingsApi";
import MeetingCard from "../components/MeetingCard";
import type { Meeting } from "../types/models";

// Educational baseline:
// This page uses the full Meeting entity type even though MeetingCard only needs a few fields.
// Students should later refactor to fetch/use a smaller meeting card type.
export default function MeetingsPage() {
  const [meetings, setMeetings] = useState<Meeting[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    fetchMeetings()
      .then(setMeetings)
      .catch((e) => setError(e.message))
      .finally(() => setLoading(false));
  }, []);

  if (loading) return <div className="loading">Loading meetings...</div>;
  if (error) return <div className="error">Error: {error}</div>;

  return (
    <>
      <h1>Meetings</h1>
      <div className="card-grid">
        {meetings.map((ev) => (
          <MeetingCard key={ev.id} meeting={ev} />
        ))}
      </div>
    </>
  );
}
