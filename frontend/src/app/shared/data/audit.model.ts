/**
 * Global audit log — mirrors the backend AuditEntry / AuditLogPage contract.
 * Read-only record of who changed what, when, across the whole application.
 */

export interface AuditEntry {
  id: string;
  timestamp: string;
  user: string;
  category: string;
  action: string;
  details: string;
  entity: string;
  entityId: string;
}

export interface AuditLogPage {
  entries: AuditEntry[];
  total: number;
  page: number;
  pageSize: number;
  categories: string[];
}
