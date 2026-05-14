import { get } from './http';
import type { AuditLogEntry } from '../types/models';

export function fetchAuditLog(): Promise<AuditLogEntry[]> {
  return get<AuditLogEntry[]>('/audit-log');
}
