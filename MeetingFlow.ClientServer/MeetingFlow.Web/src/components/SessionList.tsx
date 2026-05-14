import { Link } from 'react-router-dom';
import type { Session } from '../types/models';

// Educational baseline:
// This component receives full Session entities including InternalNotes.
// Students should later refactor to a smaller SessionListItem type.
export default function SessionList({ sessions }: { sessions: Session[] }) {
  const sorted = [...sessions].sort(
    (a, b) => new Date(a.startsAt).getTime() - new Date(b.startsAt).getTime()
  );

  return (
    <ul className="session-list">
      {sorted.map((s) => (
        <li key={s.id}>
          <strong>{s.title}</strong> &mdash; {s.roomName}
          <br />
          <small>
            {new Date(s.startsAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
            {' - '}
            {new Date(s.endsAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
          </small>
          <br />
          {s.speaker && (
            <small>
              Speaker:{' '}
              <Link to={`/speakers/${s.speaker.id}`}>{s.speaker.fullName}</Link>
            </small>
          )}
        </li>
      ))}
    </ul>
  );
}
