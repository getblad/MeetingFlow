import { Link } from "react-router-dom";
import type { Meeting } from "../types/models";

// Educational baseline:
// This component receives the full Meeting entity even though it only needs a few fields.
// Students should later refactor this to use a page/component-specific type
// (e.g., { id: string; title: string; startsAt: string; venue?: { name: string; city: string } }).
export default function MeetingCard({ meeting }: { meeting: Meeting }) {
  const badgeClass =
    meeting.status === "Published" ? "badge-published" : meeting.status === "Draft" ? "badge-draft" : "badge-cancelled";

  return (
    <div className="card">
      <h3>
        <Link to={`/meetings/${meeting.id}`}>{meeting.title}</Link>
      </h3>
      <p>{meeting.description.slice(0, 120)}...</p>
      <p className="meta">
        <span className={`badge ${badgeClass}`}>{meeting.status}</span>
      </p>
      <p className="meta">
        {new Date(meeting.startsAt).toLocaleDateString()} &mdash; {meeting.venue?.name}, {meeting.venue?.city}
      </p>
    </div>
  );
}
