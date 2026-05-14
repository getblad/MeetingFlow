import { useState, useEffect } from 'react';
import { fetchAuditLog } from '../api/auditLogApi';
import type { AuditLogEntry } from '../types/models';

// Educational baseline:
// This page uses AuditLogEntry entities directly, including TechnicalDetails.
// TechnicalDetails may contain SQL statements, IP addresses, and other sensitive data.
export default function AuditLogPage() {
  const [entries, setEntries] = useState<AuditLogEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    fetchAuditLog()
      .then(setEntries)
      .catch((e) => setError(e.message))
      .finally(() => setLoading(false));
  }, []);

  if (loading) return <div className="loading">Loading audit log...</div>;
  if (error) return <div className="error">Error: {error}</div>;

  return (
    <>
      <h1>Audit Log</h1>
      <table>
        <thead>
          <tr>
            <th>Timestamp</th>
            <th>Entity Type</th>
            <th>Action</th>
            <th>Actor</th>
            <th>Technical Details</th>
          </tr>
        </thead>
        <tbody>
          {entries.map((entry) => (
            <tr key={entry.id}>
              <td>{new Date(entry.createdAt).toLocaleString()}</td>
              <td>{entry.entityType}</td>
              <td>{entry.action}</td>
              <td>{entry.actorName}</td>
              <td className="internal-field" style={{ maxWidth: 400, wordBreak: 'break-all' }}>
                {entry.technicalDetails}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </>
  );
}
