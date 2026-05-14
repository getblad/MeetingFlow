import { useState, useEffect } from "react";
import { fetchAdminMeetings } from "../api/meetingsApi";
import MeetingTable from "../components/MeetingTable";
import type { Meeting } from "../types/models";

// Educational baseline:
// This page displays admin-only fields (InternalNotes, AdminOnlyCode) from full Meeting entities.
// In production, this should be behind authentication and use an AdminMeetingRow type.
export default function AdminMeetingsPage() {
  const [meetings, setMeetings] = useState<Meeting[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    fetchAdminMeetings()
      .then(setMeetings)
      .catch((e) => setError(e.message))
      .finally(() => setLoading(false));
  }, []);

  if (loading) return <div className="loading">Loading admin meetings...</div>;
  if (error) return <div className="error">Error: {error}</div>;

  return (
    <>
      <h1>Admin: Meetings</h1>
      <div className="warning-box">
        <strong>Educational Note:</strong> This page intentionally displays internal fields (InternalNotes,
        AdminOnlyCode) that should not be exposed in a public API.
      </div>
      <MeetingTable meetings={meetings} />
    </>
  );
}
