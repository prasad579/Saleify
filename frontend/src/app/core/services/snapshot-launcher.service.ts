import { Injectable, signal } from '@angular/core';
import { SnapshotRequest } from '@shared/data/snapshot.model';

/**
 * Drives the single global Engagement Snapshot modal (mounted once in the shell).
 * Any surface (Home, Engagements list, Campaign tags, an individual engagement)
 * calls `launch(request)` to open the snapshot for a given scope.
 */
@Injectable({ providedIn: 'root' })
export class SnapshotLauncherService {
  readonly isOpen = signal(false);
  readonly request = signal<SnapshotRequest | null>(null);
  /** When true, the modal opens with the email compose panel expanded. */
  readonly startInEmail = signal(false);

  launch(request: SnapshotRequest, opts?: { email?: boolean }) {
    this.request.set(request);
    this.startInEmail.set(!!opts?.email);
    this.isOpen.set(true);
  }

  close() {
    this.isOpen.set(false);
  }
}
