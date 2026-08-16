import { useState } from "react";
import { resolveMeetingRow } from "./resolveMeetingRow";
import type { MeetingStatus, UserRole } from "../types";

interface MeetingRowProps {
  title: string;
  status: MeetingStatus;
  registrationCount: number;
  role: UserRole;
  canManage: boolean;
  onPublish: () => Promise<void>;
}

// ✅ AFTER: Thin component — delegates decisions to the pure resolver.
// Compare with the existing MeetingCard: same domain, but the logic is testable
// without rendering React.
export function MeetingRow({ title, status, registrationCount, role, canManage, onPublish }: MeetingRowProps) {
  const [publishing, setPublishing] = useState(false);
  const vm = resolveMeetingRow({ status, registrationCount, role, canManage });

  const handlePublish = async () => {
    setPublishing(true);
    await onPublish();
  };

  return (
    <div className="card">
      <span>{title}</span>
      <span className={`badge badge-${vm.badgeVariant}`}>{publishing ? "Publishing…" : vm.statusLabel}</span>
      {vm.showRegistrations && <span>{registrationCount} registrations</span>}
      {vm.canPublish && !publishing && (
        <button onClick={handlePublish} aria-label="Publish meeting">
          Publish
        </button>
      )}
    </div>
  );
}
