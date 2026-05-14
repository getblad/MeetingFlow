import { Link } from "react-router-dom";
import type { Meeting } from "../types/models";

// Educational baseline:
// This component receives full Meeting entities including InternalNotes and AdminOnlyCode.
// Students should later refactor to use an AdminMeetingRow-specific type.
export default function MeetingTable({ meetings }: { meetings: Meeting[] }) {
  return (
    <table>
      <thead>
        <tr>
          <th>Title</th>
          <th>Status</th>
          <th>Venue</th>
          <th>Registrations</th>
          <th>Created</th>
          <th>Internal Notes</th>
          <th>Admin Code</th>
        </tr>
      </thead>
      <tbody>
        {meetings.map((ev) => {
          const badgeClass =
            ev.status === "Published" ? "badge-published" : ev.status === "Draft" ? "badge-draft" : "badge-cancelled";
          return (
            <tr key={ev.id}>
              <td>
                <Link to={`/meetings/${ev.id}`}>{ev.title}</Link>
              </td>
              <td>
                <span className={`badge ${badgeClass}`}>{ev.status}</span>
              </td>
              <td>{ev.venue?.name}</td>
              <td>{ev.registrations?.length ?? 0}</td>
              <td>{new Date(ev.createdAt).toLocaleDateString()}</td>
              <td className="internal-field">{ev.internalNotes}</td>
              <td className="internal-field">{ev.adminOnlyCode}</td>
            </tr>
          );
        })}
      </tbody>
    </table>
  );
}
