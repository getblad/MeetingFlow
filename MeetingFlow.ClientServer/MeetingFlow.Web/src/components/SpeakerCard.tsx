import type { Speaker } from '../types/models';

// Educational baseline:
// This component receives the full Speaker entity including sensitive fields.
// Students should refactor to a SpeakerCardProps type with only public info.
export default function SpeakerCard({ speaker }: { speaker: Speaker }) {
  return (
    <div className="card">
      <h3>{speaker.fullName}</h3>
      <p>{speaker.bio}</p>
      <p className="meta">{speaker.company}</p>
    </div>
  );
}
